using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class SceneExpansionWorkshop
{
    [MenuItem("Tools/RPG/Scene Expansion/Open Trading Windows")]
    public static void OpenTradingWindows()
    {
        var scene=SceneManager.GetActiveScene();if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=Expanded)return;
        var backup=File.ReadAllText("Backups/LatestWindowShops.txt").Trim();
        if(!File.Exists(backup+"/Willowreach-live.unity"))EditorSceneManager.SaveScene(scene,backup+"/Willowreach-live.unity",true);
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();int index=0;
        foreach(var node in world.nodes.Where(n=>n.kind==WillowreachNode.Kind.Shop))
        {
            var house=node.transform.parent.GetComponent<SpriteRenderer>();var shop=node.shop;var body=shop.GetComponent<SpriteRenderer>();
            string color=house.name.Contains("Blue")?"Blue":"Yellow";
            house.sprite=OpenWindowArt(color);var box=house.GetComponent<BoxCollider2D>();
            var sill=ChildSprite(house.transform,"Trading window sill");
            sill.sprite=Crop("WindowSill"+color,"Assets/Sprites/Building/House/House_"+color+".png",new Rect(43,32,42,8),new Vector2(.5f,0));
            sill.transform.localPosition=new Vector3(0,(32-96)/64f,0);sill.transform.localScale=Vector3.one;
            var display=ChildSprite(house.transform,"Merchant at open window");
            var visible=AlphaBounds(body.sprite);float height=Mathf.Ceil(visible.height*.67f);
            var cropped=new Rect(visible.x,visible.yMax-height,visible.width,height);
            display.sprite=Crop("WindowMerchant"+index,AssetDatabase.GetAssetPath(body.sprite.texture),new Rect(body.sprite.rect.x+cropped.x,body.sprite.rect.y+cropped.y,cropped.width,cropped.height),new Vector2(.5f,0));
            float scale=Mathf.Min(36f/cropped.width,24f/cropped.height);
            display.transform.localScale=Vector3.one*scale;
            display.transform.localPosition=new Vector3(0,(38-96)/64f,0);
            var oldFoot=shop.transform.Find("Merchant ground contact");if(oldFoot!=null)foreach(var collider in oldFoot.GetComponents<Collider2D>())collider.enabled=false;
            shop.transform.position=display.transform.position;
            body.enabled=false;var depth=shop.GetComponent<ObliqueDepth2D>();if(depth!=null)depth.enabled=false;
            node.transform.position=new Vector3(house.transform.position.x,box.bounds.min.y-.7f,0);node.range=1.65f;
            node.hint="E 窗口交易 · K 关闭 · F5 帮厨挑战";
            var portrait=ChildSprite(world.transform,"Window portrait source "+index);
            portrait.gameObject.layer=8;portrait.sprite=body.sprite;portrait.transform.position=new Vector3(-120-index*10,-120,0);portrait.transform.localScale=shop.transform.lossyScale;shop.portraitTarget=portrait.transform;
            var framing=house.GetComponent<WillowreachWindowShop>();if(framing==null)framing=house.gameObject.AddComponent<WillowreachWindowShop>();
            framing.source=body;framing.merchant=shop;framing.display=display;framing.portrait=portrait;framing.house=house;framing.crop=cropped;framing.counterFeet=sill.transform;
            var icon=shop.transform.Find("Compact trade hint");if(icon!=null)
            {
                icon.position=display.transform.position+Vector3.up*(cropped.height/64*scale*house.transform.lossyScale.y+.22f);
                icon.GetComponent<WillowreachMerchantPrompt>().headOffset=shop.transform.InverseTransformPoint(icon.position);
            }
            index++;
        }
        var kitchen=world.GetComponent<WillowreachKitchen>();if(kitchen==null)kitchen=world.gameObject.AddComponent<WillowreachKitchen>();
        kitchen.world=world;kitchen.counters=world.nodes.Where(n=>n.kind==WillowreachNode.Kind.Shop).ToArray();
        kitchen.pumpkin=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/pumpkin.asset");
        kitchen.mushroom=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Mushroom.asset");
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        CheckTradingWindows();
    }
    private static SpriteRenderer ChildSprite(Transform parent,string name)
    {
        var child=parent.Find(name);if(child==null){child=new GameObject(name,typeof(SpriteRenderer)).transform;child.SetParent(parent,false);}return child.GetComponent<SpriteRenderer>();
    }
    private static Rect AlphaBounds(Sprite sprite)
    {
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));var colors=texture.GetPixels32();var r=sprite.rect;
        int x0=(int)r.width,y0=(int)r.height,x1=0,y1=0;
        for(int y=0;y<r.height;y++)for(int x=0;x<r.width;x++)if(colors[((int)r.y+y)*texture.width+(int)r.x+x].a>32){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
        UnityEngine.Object.DestroyImmediate(texture);return new Rect(x0,y0,x1-x0+1,y1-y0+1);
    }
    private static Sprite OpenWindowArt(string color)
    {
        string path=Folder+"/OpenWindowHouse"+color+".png";
        var texture=new Texture2D(2,2);texture.LoadImage(File.ReadAllBytes("Assets/Sprites/Building/House/House_"+color+".png"));
        var dark=new Color32(30,36,41,255);var wood=new Color32(125,83,51,255);var edge=new Color32(54,43,38,255);var light=new Color32(170,122,70,255);
        for(int y=36;y<=61;y++)for(int x=44;x<=83;x++)texture.SetPixel(x,y,dark);
        // Open shutters sit outside the aperture; pixels are drawn at the original texture resolution.
        for(int side=0;side<2;side++)for(int y=35;y<=62;y++)for(int dx=0;dx<8;dx++)
        {
            int x=side==0?35+dx:85+dx;
            texture.SetPixel(x,y,dx==0||dx==7||y==35||y==62?edge:y%5==0?light:wood);
        }
        texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.spritePixelsPerUnit=64;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
    [MenuItem("Tools/RPG/Scene Expansion/Check Trading Windows")]
    public static void CheckTradingWindows()
    {
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();var lines=new List<string>();Action<bool,string> check=(ok,s)=>lines.Add((ok?"PASS ":"FAIL ")+s);
        foreach(var frame in world.nodes.Where(n=>n.kind==WillowreachNode.Kind.Shop).Select(n=>n.transform.parent.GetComponent<WillowreachWindowShop>()))
        {
            check(frame!=null&&frame.display.sprite!=null&&frame.house.sprite.name.Contains("OpenWindow"),"Window has open shutters and merchant upper-body art");
            check(frame.portrait!=null&&frame.merchant.portraitTarget==frame.portrait.transform,"Full-body shop portrait is independently preserved");
            check(frame.house.GetComponent<BoxCollider2D>()!=null&&!frame.source.enabled,"Building retains collision and outdoor merchant body is hidden");
        }
        check(world.GetComponent<WillowreachKitchen>().counters.Length==2,"Both windows offer the timed kitchen challenge");
        File.WriteAllLines("Logs/WillowreachWindows.txt",lines);Debug.Log(string.Join("\n",lines));
    }
}
