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
    [MenuItem("Tools/RPG/Scene Expansion/Improve Living World")]
    public static void ImproveLivingWorld()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=Expanded)return;
        string backup=File.ReadAllText("Backups/LatestLivingWorld.txt").Trim();
        if(!File.Exists(backup+"/Willowreach-live.unity"))EditorSceneManager.SaveScene(scene,backup+"/Willowreach-live.unity",true);
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();scenery=world.transform;
        // Use RPG1's exact first-row four-frame cycle, excluding the alternative poses below it.
        var frames=Enumerable.Range(0,4).Select(i=>Crop("WillowPineFrame"+i,"Assets/Sprites/Tree/Tree.png",new Rect(i*192,384,192,192),new Vector2(.5f,.08f))).ToArray();
        foreach(var r in scenery.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Pine grove"||r.name=="Boundary pine"))
        {var f=r.GetComponent<WillowreachFlipbook>();if(f==null)f=r.gameObject.AddComponent<WillowreachFlipbook>();f.frames=frames;f.framesPerSecond=5;f.phase=0;r.sprite=frames[0];}
        var bone=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/bone.asset");
        foreach(var r in scenery.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Ruin remnants"))
        {
            var collider=r.GetComponent<CircleCollider2D>();if(collider==null)collider=r.gameObject.AddComponent<CircleCollider2D>();collider.radius=.65f;collider.isTrigger=true;
            var loot=r.GetComponent<Loot>();if(loot==null)loot=r.gameObject.AddComponent<Loot>();loot.sr=r;loot.itemSO=bone;loot.quantity=1;
            // OnValidate names loot after its item; retain a stable editor identity for future edits.
            r.gameObject.name="Ruin bone pickup";
        }
        foreach(var r in scenery.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Ground detail").ToArray())
        {
            string path=AssetDatabase.GetAssetPath(r.sprite);
            if(!new[]{"Assets/Sprites/Deco/04.png","Assets/Sprites/Deco/05.png","Assets/Sprites/Deco/06.png"}.Contains(path))continue;
            var source=r.sprite;var sprite=TightRock(path);float scale=r.transform.lossyScale.y;
            float dy=(sprite.rect.y-source.rect.y-source.pivot.y)/source.pixelsPerUnit*scale;
            r.transform.position+=new Vector3(0,dy,0);r.sprite=sprite;r.name="Grounded rock";
            var box=r.GetComponent<BoxCollider2D>();
            if(box!=null){box.offset=new Vector2(0,.10f);box.size=new Vector2(sprite.bounds.size.x*.75f,.20f);}
            var depth=r.GetComponent<ObliqueDepth2D>();if(depth!=null){depth.footOffset=0;depth.Refresh();}
            var shadow=new GameObject("Rock contact shadow");shadow.transform.SetParent(r.transform,false);
            shadow.transform.localPosition=new Vector3(.03f,.02f,0);
            var shade=shadow.AddComponent<SpriteRenderer>();shade.sprite=Crop("RockGroundShadow","Assets/Shadows.png",new Rect(56,56,80,80),new Vector2(.5f,.5f));
            shade.color=new Color(1,1,1,.45f);shade.sortingOrder=-20;
            shadow.transform.localScale=new Vector3((sprite.bounds.size.x+.08f)/1.25f,.09f,1);
        }
        var nav=world.GetComponent<WillowreachNavigation>();if(nav==null)nav=world.gameObject.AddComponent<WillowreachNavigation>();nav.world=world;
        var pastoral=world.GetComponent<WillowreachPastoral>();if(pastoral==null)pastoral=world.gameObject.AddComponent<WillowreachPastoral>();
        pastoral.world=world;pastoral.bone=bone;pastoral.gold=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Gold.asset");
        var idle=Enumerable.Range(0,8).Select(i=>Crop("SheepIdle"+i,"Assets/Sprites/Sheep/HappySheep_Idle.png",new Rect(i*128,0,128,128),new Vector2(.5f,.375f))).ToArray();
        var walk=Enumerable.Range(0,6).Select(i=>Crop("SheepWalk"+i,"Assets/Sprites/Sheep/HappySheep_Bouncing.png",new Rect(i*128,0,128,128),new Vector2(.5f,.375f))).ToArray();
        var flock=new List<WillowreachSheep>();int number=0;
        foreach(var r in scenery.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name.StartsWith("Village sheep")))
        {
            var animal=r.GetComponent<WillowreachSheep>();if(animal==null)animal=r.gameObject.AddComponent<WillowreachSheep>();
            animal.navigation=nav;animal.idleFrames=idle;animal.walkFrames=walk;animal.flockIndex=number++;
            r.sprite=idle[0];r.transform.localScale=Vector3.one*1.45f;
            var depth=r.GetComponent<ObliqueDepth2D>();depth.footOffset=0;depth.Refresh();flock.Add(animal);
        }
        pastoral.sheep=flock.ToArray();
        var list=world.nodes.ToList();
        var craft=SideNode(list,"Bone flute workshop",new Vector2(-19.7f,-15.8f),"制作骨笛","消耗3根骨头 · R 呼唤/放牧羊群",WillowreachNode.Kind.Craft);
        craft.item=null;
        Vector2[] places={new Vector2(-18,-19),new Vector2(18,-13),new Vector2(13,18)};
        string[] names={"雪绒草场","雨露草场","晴光草场"};
        var weather=new[]{WeatherCycle2D.Weather.Snow,WeatherCycle2D.Weather.Rain,WeatherCycle2D.Weather.Sunny};
        var pastures=new List<WillowreachNode>();
        for(int i=0;i<3;i++)
        {
            var n=SideNode(list,"Pasture treasure "+i,places[i],names[i],"带至少一只羊靠近，在"+WillowreachWorld.WeatherName(weather[i])+"寻找埋藏的补给",WillowreachNode.Kind.Pasture);
            n.requiredWeather=weather[i];n.item=pastoral.gold;n.quantity=12;pastures.Add(n);
        }
        pastoral.pastures=pastures.ToArray();world.nodes=list.ToArray();
        Physics2D.SyncTransforms();nav.Rebuild();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        CheckLivingWorld();
    }
    private static WillowreachNode SideNode(List<WillowreachNode> list,string name,Vector2 position,string title,string hint,WillowreachNode.Kind kind)
    {
        var old=scenery.Find(name);SpriteRenderer r;
        if(old!=null)r=old.GetComponent<SpriteRenderer>();else r=Place(name,Art("Assets/Sprites/Deco/16.png"),position,1.15f);
        var n=r.GetComponent<WillowreachNode>();if(n==null)n=r.gameObject.AddComponent<WillowreachNode>();
        n.kind=kind;n.title=title;n.hint=hint;n.indicator=r;n.range=2.1f;if(!list.Contains(n))list.Add(n);return n;
    }
    private static Sprite TightRock(string path)
    {
        string name="GroundRock"+Path.GetFileNameWithoutExtension(path),destination=Folder+"/"+name+".asset";
        var existing=AssetDatabase.LoadAssetAtPath<Sprite>(destination);if(existing!=null)return existing;
        var pixels=new Texture2D(2,2);pixels.LoadImage(File.ReadAllBytes(path));
        var colors=pixels.GetPixels32();int minX=pixels.width,minY=pixels.height,maxX=0,maxY=0;
        for(int y=0;y<pixels.height;y++)for(int x=0;x<pixels.width;x++)if(colors[y*pixels.width+x].a>16)
        {minX=Mathf.Min(minX,x);minY=Mathf.Min(minY,y);maxX=Mathf.Max(maxX,x);maxY=Mathf.Max(maxY,y);}
        UnityEngine.Object.DestroyImmediate(pixels);
        return Crop(name,path,new Rect(minX,minY,maxX-minX+1,maxY-minY+1),new Vector2(.5f,0));
    }
    [MenuItem("Tools/RPG/Scene Expansion/Check Living World")]
    public static void CheckLivingWorld()
    {
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();if(world==null)return;
        var nav=world.GetComponent<WillowreachNavigation>();var game=world.GetComponent<WillowreachPastoral>();
        var lines=new List<string>();Action<bool,string> check=(ok,name)=>lines.Add((ok?"PASS ":"FAIL ")+name);
        check(world.GetComponentsInChildren<Loot>(true).Count(l=>l.itemSO==game.bone&&l.quantity==1)>=5,"Decorative bones are now real trigger pickups");
        check(game.sheep.Length==3&&game.sheep.All(s=>s.idleFrames.Length==8&&s.walkFrames.Length==6),"Three sheep have idle and moving animation clips");
        check(game.pastures.Length==3&&game.pastures.Select(p=>p.requiredWeather).Distinct().Count()==3,"Optional sheep treasure integrates all three weather types");
        foreach(var animal in game.sheep)check(nav.Safe(animal.transform.position),"Sheep starts on dry unobstructed ground: "+animal.name);
        foreach(var destination in game.pastures)
        {
            var path=nav.Path(game.sheep[0].transform.position,destination.transform.position);
            check(path.Count>0,"Sheep can reach pasture: "+destination.title);
            check(path.Count>0&&path.Zip(path.Skip(1),(a,b)=>nav.SegmentSafe(a,b)).All(s=>s),"Whole route avoids river and solid obstacles: "+destination.title);
            if(path.Count>0&&destination.transform.position.y>10)check(path.Any(p=>world.bridges.Any(b=>b.Contains(p))),"Northern pasture route crosses a real bridge");
        }
        check(!nav.Safe(new Vector2(0,6)),"Open river is forbidden to sheep even in snow");
        var tree=world.GetComponentsInChildren<WillowreachFlipbook>(true).Where(f=>f.name=="Pine grove"||f.name=="Boundary pine").ToArray();
        var original=new SerializedObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>("Assets/Sprites/Tree/Tree01.asset"));
        int count=original.FindProperty("m_AnimatedSprites").arraySize;
        check(count==4&&tree.All(f=>f.frames.Length==count&&f.framesPerSecond==5&&f.phase==0),"Tree loop matches RPG1 frame count, speed and phase");
        check(world.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="Grounded rock").All(r=>r.sprite.pivot.y==0),"Stone ground contacts exclude transparent padding");
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/WillowreachLivingWorld.txt",lines);Debug.Log(string.Join("\n",lines));
    }
}
