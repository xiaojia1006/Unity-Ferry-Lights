using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Bakes the RPG1 field atlas the same way RPG2 does: a temporary orthographic camera
// renders the authored scene, then the PNG replaces the flat colour chart in place.
// Requests only run on demand (Temp/RPG1Map.request), never automatically.
[InitializeOnLoad]
public static class RPG1MapTools
{
    const string Request="Temp/RPG1Map.request";
    const string ScenePath="Assets/Scenes/RPG1.unity";
    const string AtlasPath="Assets/SceneExpansion/RPG1FieldMap.png";
    const int AtlasWidth=1280;

    static RPG1MapTools(){EditorApplication.update+=Tick;}

    static void Tick()
    {
        ShopTick();HudTick();
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
        string command;
        try{command=File.ReadAllText(Request).Trim();if(command.Length==0)return;File.Delete(Request);}catch(IOException){return;}
        try
        {
            if(command=="bake")Bake();
            else if(command=="play")EditorApplication.isPlaying=true;
            else if(command=="stop")EditorApplication.isPlaying=false;
            else if(command=="capture")Capture();
            else if(command=="audio")AudioCheck();
            else if(command=="shop"){shopPhase=0;shopNext=0;}
            else if(command=="hud"){hudPhase=0;hudNext=0;}
            else if(command=="open-rpg2")EditorSceneManager.OpenScene("Assets/Scenes/RPG2.unity",OpenSceneMode.Single);
            else if(command=="open-rpg1")
            {
                var current=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if(current.path!=ScenePath){if(current.isDirty)EditorSceneManager.SaveScene(current);EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);}
            }
            else throw new InvalidOperationException("Unknown scoped request");
            File.WriteAllText("Logs/RPG1MapRequest.txt",command+" PASS "+DateTime.Now);
        }
        catch(Exception e){File.WriteAllText("Logs/RPG1MapRequest.txt",command+" FAIL "+e);Debug.LogException(e);}
    }

    // Walks the player next to a merchant and screenshots the plain HUD (no atlas) so the new prompt is visible.
    static int hudPhase=-1;
    static double hudNext;

    static void HudTick()
    {
        if(hudPhase<0||EditorApplication.timeSinceStartup<hudNext)return;
        if(!EditorApplication.isPlaying){hudPhase=-1;return;}
        if(hudPhase==0)
        {
            Application.runInBackground=true;
            var shops=UnityEngine.Object.FindObjectsOfType<ShopKeeper>();
            var player=UnityEngine.Object.FindObjectOfType<PlayerMovement>();
            if(shops.Length>0&&player!=null&&player.rb!=null)
            {
                var spot=new Vector3(shops[0].transform.position.x+.3f,shops[0].transform.position.y-1.15f,player.transform.position.z);
                player.transform.position=spot;player.rb.position=spot;player.rb.velocity=Vector2.zero;player.rb.WakeUp();
                Physics2D.SyncTransforms();
            }
            hudNext=EditorApplication.timeSinceStartup+1.0;
            hudPhase=1;
            return;
        }
        CaptureHud();
        hudPhase=-1;
    }

    static void CaptureHud()
    {
        var hud=UnityEngine.Object.FindObjectOfType<RPG1Activities>();
        if(hud==null)throw new InvalidOperationException("RPG1 world not loaded");
        var canvas=(Canvas)typeof(RPG1Activities).GetField("canvas",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(hud);
        Canvas.ForceUpdateCanvases();
        var hidden=new List<Renderer>();
        foreach(var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>())
            if(renderer.enabled&&!(renderer is ParticleSystemRenderer)){renderer.enabled=false;hidden.Add(renderer);}
        var go=new GameObject("RPG1 hud capture camera");
        var camera=go.AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.2f,.16f);
        camera.transform.position=new Vector3(0,0,-100);camera.orthographic=true;
        var target=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            File.WriteAllBytes("Logs/RPG1HudPreview.png",image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            foreach(var renderer in hidden)if(renderer!=null)renderer.enabled=true;
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);
        }
    }

    // Steps through "walk up to the merchant" over several frames so trigger state is real, then reports what blocks it.
    static int shopPhase=-1;
    static double shopNext;
    static string shopLog="";

    static void ShopTick()
    {
        if(shopPhase<0||EditorApplication.timeSinceStartup<shopNext)return;
        if(!EditorApplication.isPlaying){shopPhase=-1;return;}
        var shops=UnityEngine.Object.FindObjectsOfType<ShopKeeper>();
        var player=UnityEngine.Object.FindObjectOfType<PlayerMovement>();
        if(shopPhase==0)
        {
            Application.runInBackground=true; // the player loop stalls while the editor is unfocused
            shopLog="paused="+EditorApplication.isPaused+" frame="+Time.frameCount+" sim="+Physics2D.simulationMode+" timeScale="+Time.timeScale+" settingsOpen="+(GlobalSettings.Instance!=null&&GlobalSettings.Instance.IsOpen)+" blocksOtherInput="+BackpackUI.BlocksOtherInput+"\n";
            shopLog+="shops="+shops.Length+"\n";
            foreach(var s in shops)
            {
                shopLog+=" "+s.name+" pos="+s.transform.position+" inRange="+s.PlayerInRange+" open="+s.isShopOpen+" layer="+s.gameObject.layer+" tag="+s.gameObject.tag+"\n";
                shopLog+="   self id="+s.GetInstanceID()+"\n";
                foreach(var c in s.GetComponentsInChildren<Collider2D>(true))
                    shopLog+="   col "+c.gameObject.name+" id="+c.GetInstanceID()+" sameGo="+(c.gameObject==s.gameObject)+" hasKeeper="+(c.GetComponent<ShopKeeper>()!=null)+" "+c.GetType().Name+" trigger="+c.isTrigger+" enabled="+c.enabled+" layer="+c.gameObject.layer+"\n";
            }
            if(player!=null)
            {
                shopLog+="player tag="+player.gameObject.tag+" layer="+player.gameObject.layer+" pos="+player.transform.position+"\n";
                foreach(var c in player.GetComponents<Collider2D>())
                    shopLog+="   pcol "+c.GetType().Name+" trigger="+c.isTrigger+" enabled="+c.enabled+" bounds="+c.bounds+"\n";
            }
            if(player!=null&&shops.Length>0)
            {
                // Teleports can land on a sleeping body, which never reports trigger contacts; wake it and nudge.
                var start=new Vector3(shops[0].transform.position.x,shops[0].transform.position.y-1.0f,player.transform.position.z);
                player.transform.position=start;if(player.rb!=null){player.rb.position=start;player.rb.velocity=Vector2.zero;player.rb.WakeUp();}
                Physics2D.SyncTransforms();shopLog+="moved to "+start+"\n";
            }
        }
        else if(shopPhase==1)
        {
            if(player!=null&&player.rb!=null){player.rb.position=player.rb.position+Vector2.up*0.1f;player.rb.WakeUp();}
            Physics2D.SyncTransforms();
        }
        else if(shopPhase==2)
        {
            if(player!=null&&player.rb!=null)player.rb.velocity=Vector2.zero;
            for(int i=0;i<shops.Length;i++)
            {
                float distance=player!=null?Vector2.Distance(player.transform.position,shops[i].transform.position):-1f;
                shopLog+=" frame="+Time.frameCount+" walked inRange="+shops[i].PlayerInRange+" distance="+distance.ToString("0.00")+" enabled="+shops[i].enabled+"\n";
            }
            if(player!=null&&player.rb!=null)shopLog+="player rb="+player.rb.bodyType+" kinematic="+player.rb.isKinematic+"\n";
            shopLog+="physics sim="+Physics2D.simulationMode+" ignoreNpcPlayer="+Physics2D.GetIgnoreLayerCollision(8,3)+"\n";
        }
        else if(shopPhase==3)
        {
            if(shops.Length>0)
            {
                bool opened=shops[0].TryOpenFromBuilding();
                var group=shops[0].shopCanvasGroup;
                shopLog+="TryOpen="+opened+" isShopOpen="+shops[0].isShopOpen+" alpha="+(group!=null?group.alpha:-1f)+" manager="+(shops[0].shopManager!=null)+" timeScale="+Time.timeScale+"\n";
            }
        }
        else
        {
            File.WriteAllText("Logs/RPG1ShopCheck.txt",shopLog);
            shopPhase=-1;
            return;
        }
        File.WriteAllText("Logs/RPG1ShopCheck.txt",shopLog);
        shopPhase++;shopNext=EditorApplication.timeSinceStartup+.6;
    }

    [MenuItem("Tools/RPG/Developer/Bake RPG1 Atlas")]
    static void Bake()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Leave play mode before baking");
        var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if(active.path!=ScenePath)
        {
            if(active.isDirty)EditorSceneManager.SaveScene(active);
            EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        }
        Debug.Log("RPG1 atlas baked: "+BakeAtlasSprite().name);
    }

    // Renders every authored sprite inside the playable bounds; callers own the returned sprite.
    public static Sprite BakeAtlasSprite()
    {
        var hud=UnityEngine.Object.FindObjectOfType<RPG1Activities>();
        if(hud==null)throw new InvalidOperationException("RPG1 world (RPG1Activities) is not in the open scene");
        Rect bounds=hud.worldRect;
        int width=AtlasWidth,height=Mathf.Max(64,Mathf.RoundToInt(AtlasWidth*bounds.height/bounds.width));
        var baker=new GameObject("Temporary atlas baker");
        var camera=baker.AddComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=bounds.height*.5f;
        camera.transform.position=new Vector3(bounds.center.x,bounds.center.y,-20);
        camera.transform.rotation=Quaternion.identity;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.20f,.31f,.22f);
        camera.transparencySortMode=TransparencySortMode.CustomAxis;camera.transparencySortAxis=Vector3.up;
        var target=new RenderTexture(width,height,24);
        var previous=RenderTexture.active;
        // In-world sign lettering turns into smudges at atlas scale; the planks stay.
        var lettering=new List<MeshRenderer>();
        foreach(var text in UnityEngine.Object.FindObjectsOfType<TextMesh>())
        {
            var renderer=text.GetComponent<MeshRenderer>();
            if(renderer!=null&&renderer.enabled){renderer.enabled=false;lettering.Add(renderer);}
        }
        try
        {
            camera.targetTexture=target;
            camera.Render();
            RenderTexture.active=target;
            var png=new Texture2D(width,height,TextureFormat.RGB24,false);
            png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();
            File.WriteAllBytes(AtlasPath,png.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(png);
        }
        finally
        {
            foreach(var renderer in lettering)if(renderer!=null)renderer.enabled=true;
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(baker);
        }
        AssetDatabase.ImportAsset(AtlasPath);
        var importer=(TextureImporter)AssetImporter.GetAtPath(AtlasPath);
        importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit=100;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(AtlasPath);
    }

    // Confirms the combat banks resolve from Resources and that the shared source really starts playing.
    static void AudioCheck()
    {
        var report=new System.Text.StringBuilder();
        foreach(var name in new[]{"Audio/blade_01","Audio/blade_02","Audio/blade_03","Audio/metal_01","Audio/metal_02","Audio/creature_die_01"})
            report.Append(name+"="+(Resources.Load<AudioClip>(name)!=null)+"\n");
        Sfx.Blade();Sfx.Impact();Sfx.EnemySwing();Sfx.Bow();
        var field=typeof(Sfx).GetField("source",BindingFlags.NonPublic|BindingFlags.Static);
        var audio=(AudioSource)field.GetValue(null);
        report.Append("source="+(audio!=null?audio.name:"null"));
        report.Append(" isPlaying="+(audio!=null&&audio.isPlaying));
        report.Append(" effectSlider="+(GlobalSettings.Instance!=null?GlobalSettings.Instance.Data.effects:-1f));
        report.Append(" listenerVolume="+AudioListener.volume+"\n");
        File.WriteAllText("Logs/RPG1AudioCheck.txt",report.ToString());
    }

    static void Capture()    {
        var hud=UnityEngine.Object.FindObjectOfType<RPG1Activities>();
        if(hud==null)throw new InvalidOperationException("RPG1 world not loaded; enter play mode first");
        var atlas=(GameObject)typeof(RPG1Activities).GetField("atlas",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(hud);
        var canvas=(Canvas)typeof(RPG1Activities).GetField("canvas",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(hud);
        if(atlas==null||canvas==null)throw new InvalidOperationException("RPG1 HUD not built yet");
        atlas.SetActive(true);Canvas.ForceUpdateCanvases();
        // Hide the world for one render so the HUD reads cleanly; the game view is untouched otherwise.
        var hidden=new List<Renderer>();
        foreach(var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>())
        {
            if(renderer.enabled&&!(renderer is ParticleSystemRenderer)){renderer.enabled=false;hidden.Add(renderer);}
        }
        var go=new GameObject("RPG1 map capture camera");
        var camera=go.AddComponent<Camera>();
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.2f,.16f);
        camera.transform.position=new Vector3(0,0,-100);camera.orthographic=true;
        var target=new RenderTexture(1920,1080,24);var previous=RenderTexture.active;
        try
        {
            camera.targetTexture=target;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            File.WriteAllBytes("Logs/RPG1MapPreview.png",image.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(image);
        }
        finally
        {
            foreach(var renderer in hidden)if(renderer!=null)renderer.enabled=true;
            canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();
            UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
