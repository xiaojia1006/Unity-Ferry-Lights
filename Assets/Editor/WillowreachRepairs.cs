using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static partial class SceneExpansionWorkshop
{
    [MenuItem("Tools/RPG/Scene Expansion/Fix Reported Scene Issues")]
    public static void FixReportedIssues()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path!=Expanded) return;
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();
        scenery=world.transform;
        var frames=new List<Sprite>();
        for(int i=0;i<4;i++) frames.Add(Crop("WillowPineFrame"+i,"Assets/Sprites/Tree/Tree.png",
            new Rect((i%4)*192,i<4?384:192,192,192),new Vector2(.5f,.08f)));
        foreach(var tree in scenery.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Pine grove"||r.name=="Boundary pine"))
        {
            var flip=tree.GetComponent<WillowreachFlipbook>();if(flip==null)flip=tree.gameObject.AddComponent<WillowreachFlipbook>();
            flip.frames=frames.ToArray();flip.framesPerSecond=5;
            flip.phase=0; // RPG1 AnimatedTiles use the same four frames at five FPS.
            tree.sprite=frames[0];
        }
        foreach(var bridge in scenery.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Crossing - plank bridge").ToArray()) RepairBridge(bridge);
        world.bridges=new[]{new Rect(-8.7f,.1f,1.4f,12.6f),new Rect(22.3f,-8,1.4f,12.6f)};
        world.shortcutBridgeArea=new Rect(-23.7f,-4.3f,1.4f,12.6f);

        var list=world.nodes.ToList();
        AddBuilding(list,"House_Blue - village shop",WillowreachNode.Kind.Shop,"柳湾杂货铺","购买物品；K 关闭商店");
        AddBuilding(list,"House_Yellow - farm",WillowreachNode.Kind.Shop,"农场交易所","向农场商人购买补给；K 关闭商店");
        AddBuilding(list,"House_Purple - rest",WillowreachNode.Kind.Camp,"旅舍休息","恢复最多10生命，与营火共享30秒冷却");
        AddBuilding(list,"Village watchtower",WillowreachNode.Kind.Guide,"村口路标 · 查看地图","向北过桥前往晴光矿场，向东前往雨露林地。");
        AddBuilding(list,"Mine watchtower",WillowreachNode.Kind.Guide,"矿场瞭望台 · 查看地图","矿场有守卫巡逻，先完成晴天观测，再领取矿洞补给。");
        AddBuilding(list,"Ruin tower",WillowreachNode.Kind.Guide,"遗迹瞭望台 · 查看地图","雪迹观测站在遗迹南侧，三处观测完成后可修复西侧渡桥。");
        var mine=AddBuilding(list,"Old mine entrance",WillowreachNode.Kind.Supply,"领取矿洞储备","完成一处观测后领取金币12，一次性补给");
        mine.item=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Gold.asset");mine.quantity=12;mine.requiredObservations=1;
        var ruin=AddBuilding(list,"Ruin castle",WillowreachNode.Kind.Supply,"领取遗迹储备","完成三处观测后领取南瓜2，一次性补给");
        ruin.item=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/pumpkin.asset");ruin.quantity=2;ruin.requiredObservations=3;
        world.nodes=list.ToArray();
        RepairPortrait();
        Physics2D.SyncTransforms();
        world.mapSprite=CreateMap();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();CheckWillowreach();CheckReportedRepairs();
    }

    private static void RepairBridge(SpriteRenderer root)
    {
        foreach(Transform child in root.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
        root.transform.localScale=Vector3.one;
        var middle=Crop("BridgeBoardRepeat","Assets/Sprites/Bridge/Bridge_All.png",new Rect(0,80,64,32),new Vector2(.5f,.5f));
        var bottom=Crop("BridgeFootCap","Assets/Sprites/Bridge/Bridge_All.png",new Rect(0,16,64,32),new Vector2(.5f,.5f));
        var top=Crop("BridgeHeadCap","Assets/Sprites/Bridge/Bridge_All.png",new Rect(0,144,64,32),new Vector2(.5f,.5f));
        root.sprite=middle;root.enabled=false;
        // Fourteen 0.9-unit segments span the original river without stretching the planks.
        for(int i=0;i<14;i++)
        {
            var part=new GameObject("Bridge segment "+i);part.transform.SetParent(root.transform,false);
            part.transform.localPosition=new Vector3(0,-5.85f+i*.9f,0);part.transform.localScale=Vector3.one*1.8f;
            var sr=part.AddComponent<SpriteRenderer>();sr.sprite=i==0?bottom:i==13?top:middle;sr.sortingOrder=10;
        }
        for(int sign=-1;sign<=1;sign+=2)
        {
            var rail=new GameObject("Bridge railing");rail.transform.SetParent(root.transform,false);rail.transform.localPosition=new Vector3(sign*.78f,0,0);
            rail.AddComponent<BoxCollider2D>().size=new Vector2(.12f,11.8f);
        }
    }

    private static WillowreachNode AddBuilding(List<WillowreachNode> list,string name,WillowreachNode.Kind kind,string title,string hint)
    {
        var building=scenery.Find(name);var anchor=building.Find("Door interaction");
        if(anchor==null) { anchor=new GameObject("Door interaction").transform;anchor.SetParent(building,false); }
        // Ground-contact collider gives an accessible door position independent of image padding.
        var body=building.GetComponent<BoxCollider2D>();
        anchor.position=new Vector3(building.position.x,body.bounds.min.y-.65f,0);
        var node=anchor.GetComponent<WillowreachNode>();if(node==null)node=anchor.gameObject.AddComponent<WillowreachNode>();
        node.kind=kind;node.title=title;node.hint=hint;node.range=1.8f;node.indicator=null;
        if(kind==WillowreachNode.Kind.Shop) node.shop=UnityEngine.Object.FindObjectsOfType<ShopKeeper>().OrderBy(s=>Vector2.Distance(s.transform.position,anchor.position)).First();
        if(!list.Contains(node))list.Add(node);return node;
    }

    private static void RepairPortrait()
    {
        string path=Folder+"/MerchantPortrait.renderTexture";
        var texture=AssetDatabase.LoadAssetAtPath<RenderTexture>(path);
        if(texture==null) {texture=new RenderTexture(256,256,16,RenderTextureFormat.ARGB32);texture.name="MerchantPortrait";AssetDatabase.CreateAsset(texture,path);}
        foreach(var camera in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).Where(c=>c.targetTexture!=null))
        { camera.targetTexture=texture;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;camera.orthographicSize=1.09f;camera.cullingMask=256; }
        foreach(var raw in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<RawImage>(true)).Where(r=>r.name=="ShopKeeperPortait"))
        { raw.texture=texture;raw.raycastTarget=false; }
    }

    [MenuItem("Tools/RPG/Scene Expansion/Check Reported Repairs")]
    public static void CheckReportedRepairs()
    {
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();if(world==null)return;
        var report=new List<string>();Action<bool,string> check=(ok,name)=>report.Add((ok?"PASS ":"FAIL ")+name);
        var trees=world.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Pine grove"||r.name=="Boundary pine").ToArray();
        check(trees.Length>0&&trees.All(t=>t.GetComponent<WillowreachFlipbook>()!=null&&t.GetComponent<WillowreachFlipbook>().frames.Distinct().Count()==4),"Every tree uses the same four-frame cycle as RPG1");
        check(trees.All(t=>t.GetComponent<BoxCollider2D>()!=null),"Animated tree trunks retain fixed solid colliders");
        foreach(var bridge in world.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Crossing - plank bridge"))
        {
            var parts=bridge.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r!=bridge).ToArray();
            check(parts.Length==14&&parts.All(p=>Mathf.Approximately(p.transform.localScale.x,p.transform.localScale.y)),"Proportionate segmented crossing: "+bridge.transform.position);
        }
        var doors=world.nodes.Where(n=>n.transform.name=="Door interaction").ToArray();
        check(doors.Length==8,"All eight buildings have reachable door services");
        check(doors.Where(n=>n.kind==WillowreachNode.Kind.Shop).All(n=>n.shop!=null),"Both shops connect to existing merchant inventories");
        var camera=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Camera>(true)).First(c=>c.targetTexture!=null);
        check(camera.clearFlags==CameraClearFlags.SolidColor&&camera.backgroundColor.a==0&&Mathf.Approximately(camera.orthographicSize,1.09f)&&camera.targetTexture.format==RenderTextureFormat.ARGB32,"Portrait has transparent background and original framing");
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/WillowreachRepairs.txt",report);Debug.Log(string.Join("\n",report));
    }
}
