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
    [MenuItem("Tools/RPG/Scene Expansion/Polish Village And Adventures")]
    public static void PolishVillageAndAdventures()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=Expanded)return;
        var backup=File.ReadAllText("Backups/LatestVillageAdventure.txt").Trim();
        if(!File.Exists(backup+"/Willowreach-live.unity"))EditorSceneManager.SaveScene(scene,backup+"/Willowreach-live.unity",true);
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();scenery=world.transform;
        var shops=UnityEngine.Object.FindObjectsOfType<ShopKeeper>();
        foreach(var door in world.nodes.Where(n=>n.kind==WillowreachNode.Kind.Shop))
        {
            var house=door.transform.parent.GetComponent<SpriteRenderer>();var shop=door.shop;
            if(shop==null)continue;
            if(house.GetComponent<WillowreachWindowShop>()!=null)continue;
            var collider=house.GetComponent<BoxCollider2D>();
            // Stand outside the foundation; do not force the actor above every world sprite.
            shop.transform.position=new Vector3(house.transform.position.x,collider.bounds.min.y-.75f,0);
            door.transform.position=shop.transform.position+Vector3.down*.85f;door.range=1.8f;
            var body=shop.GetComponent<SpriteRenderer>();ObliqueView2D.ConfigureActor(body);
            var old=shop.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(s=>s.name=="Button_Blue_3Slides");
            if(old!=null)old.gameObject.SetActive(false);
            var hint=shop.transform.Find("Compact trade hint");
            if(hint==null){hint=new GameObject("Compact trade hint").transform;hint.SetParent(shop.transform,false);hint.gameObject.layer=0;hint.gameObject.AddComponent<SpriteRenderer>();}
            var icon=hint.GetComponent<SpriteRenderer>();icon.sprite=shops.SelectMany(s=>s.GetComponentsInChildren<SpriteRenderer>(true)).First(s=>s.name=="G_Idle").sprite;
            icon.sortingOrder=30002;hint.localScale=Vector3.one*.48f;
            hint.position=new Vector3(body.bounds.center.x,body.bounds.max.y+.28f,0);
            var prompt=hint.GetComponent<WillowreachMerchantPrompt>();if(prompt==null)prompt=hint.gameObject.AddComponent<WillowreachMerchantPrompt>();prompt.merchant=shop;prompt.icon=icon;
            prompt.headOffset=VisibleHead(body.sprite);hint.position=shop.transform.TransformPoint(prompt.headOffset);
            var foot=shop.transform.Find("Merchant ground contact");
            if(foot==null){foot=new GameObject("Merchant ground contact").transform;foot.SetParent(shop.transform,false);}
            foot.localPosition=new Vector3(0,VisibleBottom(body.sprite),0);
            var solid=foot.GetComponent<CircleCollider2D>();if(solid==null)solid=foot.gameObject.AddComponent<CircleCollider2D>();solid.radius=.25f;solid.offset=Vector2.up*.12f;
            shop.GetComponent<ObliqueDepth2D>().feet=foot;
        }
        // Use the actual RPG1 prefab and controller, including its animated child named Sprite.
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Loot Prefab.prefab");
        foreach(var old in world.GetComponentsInChildren<Loot>(true).Where(l=>l.itemSO!=null&&l.itemSO.name=="bone").ToArray())
        {
            if(PrefabUtility.GetCorrespondingObjectFromSource(old.gameObject)==prefab)continue;
            var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);go.transform.SetParent(scenery,true);
            go.transform.position=old.transform.position;go.transform.localScale=Vector3.one;
            var loot=go.GetComponent<Loot>();loot.itemSO=old.itemSO;loot.quantity=old.quantity;
            loot.sr.sprite=loot.itemSO.icon;go.name="Ruin bone pickup";
            var depth=loot.sr.GetComponent<ObliqueDepth2D>();if(depth==null)depth=loot.sr.gameObject.AddComponent<ObliqueDepth2D>();depth.feet=go.transform;depth.Refresh();
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
        var adventure=world.GetComponent<WillowreachAdventure>();if(adventure==null)adventure=world.gameObject.AddComponent<WillowreachAdventure>();
        adventure.world=world;adventure.gold=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Gold.asset");
        adventure.mushroom=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Mushroom.asset");
        var list=world.nodes.ToList();
        adventure.dispatch=SideNode(list,"Village dispatch board",new Vector2(-21,-14),"商队委托牌","E 接单/领奖 · 无押金 · 桥上运货安全",WillowreachNode.Kind.Adventure);
        adventure.destinations=new[]{
            SideNode(list,"Delivery mine",new Vector2(14,16),"矿场收货点","E 交付指定货物 · 避免涉水和受伤",WillowreachNode.Kind.Adventure),
            SideNode(list,"Delivery forest",new Vector2(20,-16),"林地收货点","E 交付指定货物 · 避免涉水和受伤",WillowreachNode.Kind.Adventure),
            SideNode(list,"Delivery ruins",new Vector2(-20,16),"遗迹收货点","E 交付指定货物 · 避免涉水和受伤",WillowreachNode.Kind.Adventure)};
        Vector2[] runePlaces={new Vector2(-16,15),new Vector2(-13,18),new Vector2(-10,15)};
        adventure.runes=new WillowreachNode[3];
        for(int i=0;i<3;i++)
        {
            var n=SideNode(list,"Resonance rune "+i,runePlaces[i],"共鸣符石"+(i+1),"E 触碰 · F4 重播四次闪光，按顺序共鸣",WillowreachNode.Kind.Adventure);
            n.indicator.sprite=TightRock("Assets/Sprites/Deco/06.png");n.transform.localScale=Vector3.one*2.4f;
            var label=n.transform.Find("Rune number");
            if(label==null){label=new GameObject("Rune number",typeof(TextMesh)).transform;label.SetParent(n.transform,false);}
            label.localPosition=new Vector3(0,.23f,0);var text=label.GetComponent<TextMesh>();text.text=(i+1).ToString();text.fontSize=64;text.characterSize=.07f;text.anchor=TextAnchor.MiddleCenter;text.color=new Color(1,.88f,.45f);text.GetComponent<MeshRenderer>().sortingOrder=30001;
            var depth=n.GetComponent<ObliqueDepth2D>();depth.footOffset=0;depth.Refresh();adventure.runes[i]=n;
        }
        world.nodes=list.ToArray();
        Physics2D.SyncTransforms();
        foreach(var depth in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<ObliqueDepth2D>(true)))depth.Refresh();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        CheckVillageAdventure();
    }
    private static Vector2 VisibleHead(Sprite sprite)
    {
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
        var pixels=image.GetPixels32();var rect=sprite.rect;int top=(int)rect.y;
        for(int y=(int)rect.y;y<rect.yMax;y++)for(int x=(int)rect.x;x<rect.xMax;x++)if(pixels[y*image.width+x].a>32)top=Mathf.Max(top,y);
        UnityEngine.Object.DestroyImmediate(image);
        return new Vector2(0,(top-rect.y-sprite.pivot.y)/sprite.pixelsPerUnit+.3f);
    }
    private static float VisibleBottom(Sprite sprite)
    {
        var image=new Texture2D(2,2);image.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
        var pixels=image.GetPixels32();var rect=sprite.rect;int bottom=(int)rect.yMax;
        for(int y=(int)rect.y;y<rect.yMax;y++)for(int x=(int)rect.x;x<rect.xMax;x++)if(pixels[y*image.width+x].a>32)bottom=Mathf.Min(bottom,y);
        UnityEngine.Object.DestroyImmediate(image);return (bottom-rect.y-sprite.pivot.y)/sprite.pixelsPerUnit;
    }
    [MenuItem("Tools/RPG/Scene Expansion/Check Village Adventure")]
    public static void CheckVillageAdventure()
    {
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();var a=world.GetComponent<WillowreachAdventure>();
        var lines=new List<string>();Action<bool,string> check=(ok,name)=>lines.Add((ok?"PASS ":"FAIL ")+name);
        foreach(var n in world.nodes.Where(n=>n.kind==WillowreachNode.Kind.Shop))
        {
            var body=n.shop.GetComponent<SpriteRenderer>();var house=n.transform.parent.GetComponent<BoxCollider2D>();
            var window=n.transform.parent.GetComponent<WillowreachWindowShop>();
            check(window!=null?window.display.sprite!=null:body.bounds.min.y<house.bounds.min.y-.5f,"Merchant is visible at its trading position: "+n.title);
            check(window!=null?window.display.sortingOrder>window.house.sortingOrder:body.sortingOrder>n.transform.parent.GetComponent<SpriteRenderer>().sortingOrder,"Merchant renders in front of shop opening: "+n.title);
            check(!n.shop.GetComponentsInChildren<Transform>(true).First(t=>t.name=="Button_Blue_3Slides").gameObject.activeSelf,"Large shop plaque disabled: "+n.title);
        }
        var bones=world.GetComponentsInChildren<Loot>(true).Where(l=>l.itemSO!=null&&l.itemSO.name=="bone").ToArray();
        check(bones.Length==5&&bones.All(l=>l.anim!=null&&l.sr.transform.parent==l.transform&&l.sr.name=="Sprite"),"All bones use RPG1 pickup prefab animation bindings");
        check(a.destinations.Length==3&&a.runes.Length==3&&a.dispatch!=null,"Delivery network and three-rune puzzle are authored scene objects");
        check(a.runes.All(n=>n.indicator.sprite!=null&&n.GetComponentInChildren<TextMesh>()!=null),"All runes have visible stone art and number labels");
        check(a.destinations.Concat(a.runes).All(n=>n.kind==WillowreachNode.Kind.Adventure&&n.Reusable),"Activities remain interactable for retries and pending rewards");
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/WillowreachVillage.txt",lines);Debug.Log(string.Join("\n",lines));
    }
    [MenuItem("Tools/RPG/Scene Expansion/Preview Village In Play")]
    public static void PreviewVillageInPlay()
    {var w=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();if(EditorApplication.isPlaying&&w!=null){w.player.position=new Vector3(-20,-13,0);w.weather.SetWeather(WeatherCycle2D.Weather.Sunny);}}
    [MenuItem("Tools/RPG/Scene Expansion/Preview Runes In Play")]
    public static void PreviewRunesInPlay()
    {var a=UnityEngine.Object.FindObjectOfType<WillowreachAdventure>();if(EditorApplication.isPlaying&&a!=null){a.world.player.position=new Vector3(-13,16,0);a.world.weather.SetWeather(WeatherCycle2D.Weather.Sunny);}}
}
