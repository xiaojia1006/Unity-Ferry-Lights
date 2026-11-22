using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static partial class RPG2Workshop
{
    const string ScenePath="Assets/Scenes/RPG2.unity";
    const string Request="Temp/RPG2Workshop.request";
    static RPG2Workshop()
    {
        EditorApplication.update+=Tick;
        Application.logMessageReceived+=(message,stack,type)=> {
            if(type==LogType.Error||type==LogType.Exception) File.AppendAllText("Logs/RPG2Errors.txt",message+"\n"+stack+"\n");
        };
    }
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(Request))return;
        string command;
        try{command=File.ReadAllText(Request).Trim();if(command.Length==0)return;File.Delete(Request);}catch(IOException){return;}
        try
        {
            if(command=="inspect")Inspect();
            else if(command=="overview")Overview();
            else if(command=="play")EditorApplication.isPlaying=true;
            else if(command=="stop")EditorApplication.isPlaying=false;
            else if(command=="save"&&!EditorApplication.isPlaying)EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            else RunCommand(command);
            File.WriteAllText("Logs/RPG2Request.txt",command+" complete");
        }
        catch(Exception error){File.WriteAllText("Logs/RPG2Request.txt",command+" FAILED "+error);Debug.LogException(error);}
    }
    static partial void RunCommand(string command);
    [MenuItem("Tools/RPG/RPG2 Workshop/Inspect Existing Scene")]
    public static void Inspect()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        var previous=SceneManager.GetActiveScene();
        var scene=SceneManager.GetSceneByPath(ScenePath);
        bool opened=!scene.IsValid()||!scene.isLoaded;
        if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        var report=new StringBuilder();
        foreach(var root in scene.GetRootGameObjects())
        {
            report.AppendLine("ROOT "+root.name+" at "+root.transform.position+" active="+root.activeSelf);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                var map=t.GetComponent<Tilemap>();
                if(map!=null)report.AppendLine("TILEMAP "+t.name+" position="+t.position+" scale="+t.lossyScale+" cellBounds="+map.cellBounds+" occupied="+map.GetUsedTilesCount()+" components="+string.Join(",",t.GetComponents<Component>().Select(c=>c==null?"MISSING":c.GetType().Name)));
                var sr=t.GetComponent<SpriteRenderer>();
                if(sr!=null && sr.sprite!=null)report.AppendLine("SPRITE "+t.name+" pos="+t.position+" size="+sr.bounds.size+" sprite="+AssetDatabase.GetAssetPath(sr.sprite)+" active="+t.gameObject.activeInHierarchy+" components="+string.Join(",",t.GetComponents<Component>().Select(c=>c==null?"MISSING":c.GetType().Name)));
                if(t.GetComponent<QuestLogUI>()!=null || t.GetComponent<QuestManager>()!=null || t.GetComponent<QuestBoard>()!=null)report.AppendLine("QUEST "+t.name+" "+EditorJsonUtility.ToJson(t.GetComponent<QuestLogUI>() as UnityEngine.Object ?? t.GetComponent<QuestBoard>() as UnityEngine.Object ?? t.GetComponent<QuestManager>()));
            }
        }
        foreach(string guid in AssetDatabase.FindAssets("t:QuestSo"))
        {string path=AssetDatabase.GUIDToAssetPath(guid);report.AppendLine("QUEST ASSET "+path+" "+EditorJsonUtility.ToJson(AssetDatabase.LoadAssetAtPath<QuestSo>(path)));}
        foreach(var root in scene.GetRootGameObjects())
        {
            foreach(var c in root.GetComponentsInChildren<Collider2D>(true))
                if(c.enabled)report.AppendLine("COLLIDER "+c.name+" "+c.GetType().Name+" center="+c.bounds.center+" size="+c.bounds.size+" trigger="+c.isTrigger+" layer="+c.gameObject.layer);
            foreach(var c in root.GetComponentsInChildren<SceneChanger>(true))report.AppendLine("EXIT "+c.name+" pos="+c.transform.position+" target="+c.sceneToLoad);
        }
        var water=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<WaterSurface2D>()).FirstOrDefault();
        if(water!=null)
        {
            var shape=water.GetComponent<PolygonCollider2D>();
            report.AppendLine("RIVER GRID: # water / . land, x=-34..30 step2, y=20..-18 step2");
            for(int y=20;y>=-18;y-=2){report.Append(y.ToString("00")+" ");for(int x=-34;x<=30;x+=2)report.Append(shape.OverlapPoint(new Vector2(x,y))?'#':'.');report.AppendLine();}
        }
        File.WriteAllText("Logs/RPG2Before.txt",report.ToString());
        if(previous.IsValid())SceneManager.SetActiveScene(previous);
        if(opened)EditorSceneManager.CloseScene(scene,true);
    }
    public static void Overview()
    {
        var go=new GameObject("Temporary RPG2 overview camera");
        var camera=go.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=23;
        camera.transform.position=new Vector3(-3,1,-20);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.15f,.24f,.25f);
        var texture=new RenderTexture(1536,1024,24);camera.targetTexture=texture;camera.Render();
        var prior=RenderTexture.active;RenderTexture.active=texture;var png=new Texture2D(1536,1024,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1536,1024),0,0);png.Apply();
        File.WriteAllBytes("Logs/RPG2Overview.png",png.EncodeToPNG());RenderTexture.active=prior;camera.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(go);
    }
}
