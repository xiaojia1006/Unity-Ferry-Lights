using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Cinemachine;

public static class RPG1FrontierBuilder
{
    const string ScenePath="Assets/Scenes/RPG1.unity";
    static Transform root;
    static RPG1Frontier world;
    static readonly Rect bounds=new Rect(-32,-22,64,46);
    static readonly List<Vector2> occupied=new List<Vector2>();
    static readonly Vector2[] route={new Vector2(-23,-14),new Vector2(-13,-13),new Vector2(-15,-3),new Vector2(-21,4),new Vector2(-14,8),new Vector2(0,7),new Vector2(14,6),new Vector2(20,-3),new Vector2(16,-12),new Vector2(2,-14),new Vector2(-13,-13)};
    [MenuItem("Tools/RPG/RPG1 Frontier/Build RPG1")]
    public static void Build()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before building.");
        string backup="Backups/RPG1Build-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        var active=SceneManager.GetActiveScene();
        if(active.IsValid()&&active.isLoaded){EditorSceneManager.SaveScene(active,backup+"/"+active.name+"-live.unity",true);if(active.isDirty)EditorSceneManager.SaveScene(active);}
        File.Copy(ScenePath,backup+"/RPG1.unity");File.Copy(ScenePath+".meta",backup+"/RPG1.unity.meta");
        foreach(string path in new[]{"Assets/SceneExpansion/RPG1FieldMap.png","Assets/SceneExpansion/RPG1FieldMap.png.meta"})if(File.Exists(path))File.Copy(path,backup+"/"+Path.GetFileName(path));
        File.WriteAllText("Backups/LatestRPG1SceneBuild.txt",backup);
        var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
        if(UnityEngine.Object.FindObjectOfType<RPG1Frontier>()!=null)throw new InvalidOperationException("RPG1 already built. Edit the native scene; restore backup before rebuilding.");
        var enemy=UnityEngine.Object.FindObjectOfType<Enemy_Movement>();if(enemy==null)throw new InvalidOperationException("Original enemy source missing.");
        var prototype=UnityEngine.Object.Instantiate(enemy.gameObject);prototype.name="Temporary original enemy";prototype.SetActive(false);
        var archive=new GameObject("Original RPG1 layout - disabled rollback reference");
        foreach(var grid in UnityEngine.Object.FindObjectsOfType<Grid>())grid.transform.SetParent(archive.transform,true);
        foreach(var loot in UnityEngine.Object.FindObjectsOfType<Loot>())loot.transform.SetParent(archive.transform,true);
        foreach(var old in UnityEngine.Object.FindObjectsOfType<Enemy_Movement>())old.transform.SetParent(archive.transform,true);
        foreach(var old in UnityEngine.Object.FindObjectsOfType<TreasureChest>())old.transform.SetParent(archive.transform,true);
        foreach(var old in UnityEngine.Object.FindObjectsOfType<RPG1Activities>())UnityEngine.Object.DestroyImmediate(old.gameObject);
        archive.SetActive(false);
        root=new GameObject("RPG1 - Windwood logging frontier").transform;world=root.gameObject.AddComponent<RPG1Frontier>();
        var hud=root.gameObject.AddComponent<RPG1Activities>();world.hud=hud;hud.frontier=world;hud.worldRect=bounds;
        var theme=Resources.Load<BackpackTheme>("BackpackTheme");hud.parchment=Art("Assets/SceneExpansion/JournalParchment.png");hud.ribbon=theme.ribbon;hud.button=theme.button;hud.gold=Item("Gold");hud.experience=Item("EXP");hud.posts=new Transform[7];occupied.Clear();
        Terrain();
        var player=UnityEngine.Object.FindObjectOfType<PlayerMovement>();player.transform.position=world.spawn;player.GetComponent<Rigidbody2D>().position=world.spawn;
        Camp();PowerStations();Fort();
        // Open patrol lanes; the small solid foot collider is independent of the animation bounds.
        var sentinels=new List<RPG1Sentinel>();
        sentinels.Add(Enemy(prototype,"Western lookout",new Vector2(-22,1),new Vector2(-18,3),new Rect(-27,-5,14,14),3));
        sentinels.Add(Enemy(prototype,"Western machine guard",new Vector2(-19,6),new Vector2(-15,5),new Rect(-27,-5,14,14),4));
        sentinels.Add(Enemy(prototype,"Timber road patrol",new Vector2(-8,-5),new Vector2(-4,-6),new Rect(-12,-10,12,13),3));
        sentinels.Add(Enemy(prototype,"East orchard scout",new Vector2(22,3),new Vector2(26,2),new Rect(20,-8,10,16),3));
        sentinels.Add(Enemy(prototype,"Northern road guard",new Vector2(10,7),new Vector2(15,8),new Rect(6,3,15,7),4));
        sentinels.Add(Enemy(prototype,"Southern cache guard",new Vector2(6,-16),new Vector2(11,-17),new Rect(2,-20,13,9),4));
        world.arena=new[]{Enemy(prototype,"Fort captain",new Vector2(-4,15),new Vector2(-5,18),new Rect(-9,11,18,10),6,true),Enemy(prototype,"Fort defender",new Vector2(4,16),new Vector2(5,19),new Rect(-9,11,18,10),5,true)};
        UnityEngine.Object.DestroyImmediate(prototype);
        var treasure=Chest("Fort completion chest",new Vector2(0,19));world.finalChest=treasure;treasure.gameObject.SetActive(false);
        Chest("Western hidden supply",new Vector2(-27,12));Chest("Southern risk reward",new Vector2(11,-18));Chest("Eastern woodland cache",new Vector2(27,13));
        Harvest("Mushroom patch",new Vector2(-27,-5),"Mushroom",3,80);Harvest("Mushroom patch",new Vector2(-25,11),"Mushroom",2,80);
        Harvest("Pumpkin garden",new Vector2(22,-12),"pumpkin",2,100);Harvest("Pumpkin garden",new Vector2(24,-14),"pumpkin",2,100);
        Harvest("Lost supplies",new Vector2(27,10),"bone",3,0);Harvest("Trail supplies",new Vector2(-6,-16),"bone",2,0);
        BuildSheep();Decorate();
        foreach(var camera in UnityEngine.Object.FindObjectsOfType<Camera>())if(camera.targetTexture==null){camera.orthographic=true;camera.orthographicSize=6;camera.transform.rotation=Quaternion.identity;camera.transparencySortMode=TransparencySortMode.CustomAxis;camera.transparencySortAxis=Vector3.up;camera.backgroundColor=new Color(.18f,.29f,.21f);}
        foreach(var cam in UnityEngine.Object.FindObjectsOfType<CinemachineVirtualCamera>()){cam.m_Lens.OrthographicSize=6;cam.Follow=player.transform;cam.transform.position=player.transform.position+Vector3.back*10;}
        var conf=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Confiner");
        if(conf!=null){conf.transform.position=Vector3.zero;conf.transform.localScale=Vector3.one;foreach(var c in conf.GetComponents<Collider2D>()){c.isTrigger=true;if(c is PolygonCollider2D poly){poly.offset=Vector2.zero;poly.points=new[]{new Vector2(-32,-22),new Vector2(-32,24),new Vector2(32,24),new Vector2(32,-22)};}if(c is BoxCollider2D box){box.offset=new Vector2(0,1);box.size=new Vector2(64,46);}}}
        foreach(var c in UnityEngine.Object.FindObjectsOfType<CinemachineConfiner>())c.InvalidatePathCache();
        var portal=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Teleport");if(portal!=null){portal.transform.position=new Vector3(-29,-16,0);foreach(var c in portal.GetComponents<Collider2D>())c.offset=Vector2.zero;Sign("通往旧道路",new Vector2(-29,-14.5f));}
        var weather=UnityEngine.Object.FindObjectOfType<WeatherCycle2D>();if(weather!=null){weather.minimumDuration=100;weather.maximumDuration=170;weather.SetWeather(WeatherCycle2D.Weather.Sunny);}
        ObliqueView2D.Configure(scene);Physics2D.SyncTransforms();hud.chart=Map(); // baked from the authored scene, not a flat colour chart
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(new Vector3(-18,-10,0),Quaternion.identity,12,true);
        Selection.activeGameObject=root.gameObject;
        File.WriteAllText("Logs/RPG1FrontierBuild.txt","Built "+DateTime.Now+"\nBackup: "+backup+"\n8 enemies, 4 chests, 6 harvest patches, 1 rescue sheep, 2 power stations, 2 hazards.\n");
        RPG1FrontierChecks.VerifyScene();
    }
    static ItemSO Item(string name)=>AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/"+name+".asset");
    static Sprite Art(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    static Sprite Asset(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SceneExpansion/"+name+".asset");
    static GameObject Obj(string name,Vector2 p){var g=new GameObject(name);g.transform.SetParent(root,false);g.transform.position=p;return g;}
    static SpriteRenderer Place(string name,Sprite art,Vector2 p,float scale=1,Vector2? solid=null)
    {
        if(art==null)throw new InvalidOperationException("Missing art: "+name);
        var g=Obj(name,p);g.transform.localScale=Vector3.one*scale;var s=g.AddComponent<SpriteRenderer>();s.sprite=art;
        if(solid.HasValue){g.layer=7;var c=g.AddComponent<BoxCollider2D>();c.size=solid.Value/scale;c.offset=new Vector2(0,.12f/scale);occupied.Add(p);}
        g.AddComponent<ObliqueDepth2D>().Refresh();return s;
    }
    static void Sign(string text,Vector2 p)
    {
        var g=Obj("Sign - "+text,p);var t=g.AddComponent<TextMesh>();t.text=text;t.font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Arial"},30);t.fontSize=30;t.characterSize=.065f;t.anchor=TextAnchor.MiddleCenter;t.color=new Color(.2f,.16f,.10f);var r=g.GetComponent<MeshRenderer>();r.sharedMaterial=t.font.material;r.sortingOrder=20000;
    }
    static Transform Post(int index,string name,Vector2 p){var g=Obj(name,p);world.hud.posts[index]=g.transform;occupied.Add(p);return g.transform;}
    static Tilemap Tiles(string name,Transform parent,int order)
    {var g=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));g.transform.SetParent(parent,false);g.GetComponent<TilemapRenderer>().sortingOrder=order;var d=g.AddComponent<ObliqueTilemap2D>();d.terrainOrder=order;return g.GetComponent<Tilemap>();}
    static float Road(Vector2 p)
    {float d=1000;for(int i=0;i<route.Length-1;i++){var a=route[i];var v=route[i+1]-a;d=Mathf.Min(d,Vector2.Distance(p,a+v*Mathf.Clamp01(Vector2.Dot(p-a,v)/v.sqrMagnitude)));}d=Mathf.Min(d,Vector2.Distance(p,new Vector2(0,Mathf.Clamp(p.y,7,19))));return d;}
    static void Terrain()
    {
        var grid=Obj("Editable terrain - grass paths and clearings",Vector2.zero);grid.AddComponent<Grid>();var ground=Tiles("Ground",grid.transform,-2000);var paths=Tiles("Paths",grid.transform,-1800);var fringe=Tiles("Path edges",grid.transform,-1750);
        var grass=AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/Grass Tiles/Tilemap_Flat_11.asset");var dirt=AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/Grass Tiles/Tilemap_Flat_15.asset");
        var painted=new HashSet<Vector3Int>();
        for(int x=-32;x<32;x++)for(int y=-22;y<24;y++)
        {
            var c=new Vector3Int(x,y,0);ground.SetTile(c,grass);ground.SetTileFlags(c,TileFlags.None);float tint=.93f+Mathf.PerlinNoise((x+60)*.12f,(y+60)*.12f)*.07f;ground.SetColor(c,new Color(tint,tint,tint));
            var p=new Vector2(x+.5f,y+.5f);bool clearing=new Vector2((p.x+23)/7,(p.y+14)/5).sqrMagnitude<1||new Vector2((p.x+20)/5,(p.y-4)/4).sqrMagnitude<1||new Rect(10,-8,9,8).Contains(p)||new Rect(-9,11,18,10).Contains(p);
            if(Road(p)<1.3f+Mathf.PerlinNoise(x*.17f+30,y*.17f+30)*.4f||clearing){paths.SetTile(c,dirt);painted.Add(c);}
        }
        foreach(var c in painted){bool l=!painted.Contains(c+Vector3Int.left),r=!painted.Contains(c+Vector3Int.right),u=!painted.Contains(c+Vector3Int.up),d=!painted.Contains(c+Vector3Int.down);if(!l&&!r&&!u&&!d)continue;var tile=AssetDatabase.LoadAssetAtPath<TileBase>("Assets/SceneExpansion/PathEdge"+(l?0:r?2:1)+(u?2:d?0:1)+"Tile.asset");if(tile!=null){fringe.SetTile(c,tile);paths.SetTile(c,null);}}
        // Continuous physical boundary behind the dense forest, with no invisible interior walls.
        Wall("West forest boundary",new Vector2(-32,1),new Vector2(1,46));Wall("East forest boundary",new Vector2(32,1),new Vector2(1,46));Wall("South forest boundary",new Vector2(0,-22),new Vector2(64,1));Wall("North forest boundary",new Vector2(0,24),new Vector2(64,1));
    }
    static void Wall(string name,Vector2 p,Vector2 size){var g=Obj(name,p);g.layer=7;g.AddComponent<BoxCollider2D>().size=size;}
    static void Camp()
    {
        Post(0,"营地 · 休息与结算",new Vector2(-23,-13));var fire=Place("Campfire",Asset("CampFire0"),new Vector2(-23,-12),1);var flip=fire.gameObject.AddComponent<WillowreachFlipbook>();flip.frames=Enumerable.Range(0,7).Select(i=>Asset("CampFire"+(i*128))).ToArray();flip.framesPerSecond=8;
        Sign("风鸣伐木场  ·  安全营地",new Vector2(-23,-10));
        Building("Quartermaster shelter","Assets/Sprites/Building/House/House_Blue.png",new Vector2(-27,-9),1.4f,new Vector2(2.2f,1.6f));
        Building("Field kitchen","Assets/Sprites/Building/House/House_Yellow.png",new Vector2(-18,-9),1.4f,new Vector2(2.2f,1.6f));
        var shops=UnityEngine.Object.FindObjectsOfType<ShopKeeper>().OrderBy(s=>s.name).ToArray();for(int i=0;i<shops.Length;i++){shops[i].transform.position=new Vector3(i==0?-27:-18,-11,0);var n=shops[i].GetComponent<NPC>();if(n!=null)n.currentState=NPC.NPCState.Idle;ObliqueView2D.ConfigureActor(shops[i].GetComponent<SpriteRenderer>());occupied.Add(shops[i].transform.position);}
        ObliqueView2D.RepairWorldPrompts(SceneManager.GetActiveScene());
        Sign("休息 E   商店 K   背包 B   地图 Tab",new Vector2(-23,-16.5f));
        var board=Place("Bone supply commission",Art("Assets/Sprites/Deco/17.png"),new Vector2(-19,-17),1);var order=board.gameObject.AddComponent<RPG1SupplyOrder>();order.world=world;order.bone=Item("bone");Sign("骨料委托 F · 3骨料换10金币",new Vector2(-19,-18.2f));
    }
    static void Building(string name,string path,Vector2 p,float scale,Vector2 footprint)
    {var s=Place(name,Art(path),p,scale,footprint);var b=s.sprite.bounds;s.transform.position=new Vector3(p.x,p.y-b.min.y*scale,0);s.GetComponent<BoxCollider2D>().offset=new Vector2(0,b.min.y+footprint.y/(2*scale));}
    static void PowerStations()
    {
        Building("Western mine engine","Assets/Sprites/GoldMine/GoldMine_Inactive.png",new Vector2(-21,5),1.25f,new Vector2(3.0f,1.4f));Post(1,"西供能 · 清敌后按住 E",new Vector2(-21,3.7f));Sign("西供能 · 清理守卫",new Vector2(-21,8.2f));
        Post(2,"东供能 · 配重机关",new Vector2(17,-3));Place("East machine",Art("Assets/Sprites/Deco/17.png"),new Vector2(17,-2),1);Sign("推箱到金色压板，再按住 E",new Vector2(14.5f,1));
        var plate=Place("Golden pressure plate",Asset("BridgeBoardRepeat"),new Vector2(14,-3),1.1f);UnityEngine.Object.DestroyImmediate(plate.GetComponent<ObliqueDepth2D>());plate.sortingOrder=-100;plate.color=new Color(1,.78f,.3f);world.plate=plate.transform;
        var crate=Place("Movable timber crate",Asset("BridgeBoardRepeat"),new Vector2(12,-6),1,new Vector2(.85f,.7f));var cb=crate.gameObject.AddComponent<Rigidbody2D>();cb.gravityScale=0;cb.freezeRotation=true;cb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;world.crate=crate.gameObject.AddComponent<RPG1PushCrate>();world.crate.origin=new Vector2(12,-6);world.crate.allowed=new Rect(10,-8,9,8);
        for(int i=0;i<3;i++){var slat=new GameObject("Crate board "+i);slat.transform.SetParent(crate.transform,false);slat.transform.localPosition=new Vector3(0,i*.18f,0);var sr=slat.AddComponent<SpriteRenderer>();sr.sprite=Asset("BridgeBoardRepeat");slat.AddComponent<ObliqueDepth2D>();}
        Post(4,"木箱复位杆",new Vector2(10,-6));Place("Reset lever",Art("Assets/Sprites/Deco/16.png"),new Vector2(10,-5),.8f);
        Hazard(new Vector2(-6,-10),0);Hazard(new Vector2(9,-13),2.6f);
    }
    static void Fort()
    {
        // Dense palisade has a matching continuous footprint, and a single 3-unit gate.
        Wall("Fort west footprint",new Vector2(-10,16),new Vector2(1,12));Wall("Fort east footprint",new Vector2(10,16),new Vector2(1,12));Wall("Fort north footprint",new Vector2(0,22),new Vector2(21,1));Wall("Fort south west",new Vector2(-5.75f,10),new Vector2(8.5f,1));Wall("Fort south east",new Vector2(5.75f,10),new Vector2(8.5f,1));
        for(int x=-10;x<=10;x++){if(Mathf.Abs(x)>1)Palisade(new Vector2(x,10));Palisade(new Vector2(x,22));}for(int y=11;y<22;y++){Palisade(new Vector2(-10,y));Palisade(new Vector2(10,y));}
        world.gate=Obj("Dual-power gate",new Vector2(0,10));world.gate.layer=7;world.gate.AddComponent<BoxCollider2D>().size=new Vector2(3,1);
        for(int i=-1;i<=1;i++){var s=Place("Gate timber",Asset("Driftwood"),new Vector2(i,10),1);s.transform.SetParent(world.gate.transform,true);}
        Post(3,"工事大门 · 双供能",new Vector2(0,8.7f));Post(5,"工事战旗 · 唤醒守卫",new Vector2(0,13));Post(6,"工事奖励 · 宝箱",new Vector2(0,18));
        Place("Encounter flag",Art("Assets/Sprites/Deco/18.png"),new Vector2(0,14),1);Sign("北部工事 · 双供能解锁",new Vector2(0,11));
        Building("Western watchtower","Assets/Sprites/Building/Tower/Tower_Red.png",new Vector2(-8,11),1.2f,new Vector2(1.8f,1.6f));Building("Eastern watchtower","Assets/Sprites/Building/Tower/Tower_Purple.png",new Vector2(8,11),1.2f,new Vector2(1.8f,1.6f));
        Place("Old fort ruins",Art("Assets/Sprites/Building/Castle/Castle_Destroyed.png"),new Vector2(0,21),1);
    }
    static void Palisade(Vector2 p){Place("Timber palisade",Asset("Driftwood"),p,1.4f);}
    static void Hazard(Vector2 p,float phase)
    {
        var s=Place("Timber slide - amber warning red danger",Asset("BridgeBoardRepeat"),p,1);UnityEngine.Object.DestroyImmediate(s.GetComponent<ObliqueDepth2D>());s.sortingOrder=-80;s.drawMode=SpriteDrawMode.Tiled;s.size=new Vector2(4,1.2f);var h=s.gameObject.AddComponent<RPG1SawHazard>();h.warning=s;h.size=new Vector2(4,1.2f);h.phase=phase;Sign("落木道 · 黄灯预警 / 两侧绕行",p+Vector2.up*1.4f);
        var log=Place("Sliding timber",Asset("Driftwood"),p,1);var motion=log.gameObject.AddComponent<RPG1TimberMotion>();motion.hazard=h;
    }
    static RPG1Sentinel Enemy(GameObject prototype,string name,Vector2 p,Vector2 to,Rect area,int hp,bool arena=false)
    {
        var g=UnityEngine.Object.Instantiate(prototype,root);g.name=name;g.transform.position=p;g.transform.localScale=Vector3.one;g.layer=6;
        foreach(var c in g.GetComponents<Collider2D>())if(c.isTrigger)UnityEngine.Object.DestroyImmediate(c);else if(c is CapsuleCollider2D cap){cap.offset=new Vector2(0,.8f);cap.size=new Vector2(.46f,.3f);}
        var h=g.GetComponent<Enemy_Health>();h.maxHealth=hp;h.currenetHealth=hp;h.expReward=arena?0:2;h.preserveForRetry=arena;h.invulnerable=arena;
        var a=g.AddComponent<RPG1Sentinel>();a.world=world;a.home=p;a.patrolEnd=to;a.territory=area;a.arenaOnly=arena;
        g.GetComponent<Enemy_Movement>().enabled=false;g.GetComponent<Enemy_Combat>().enabled=false;
        if(g.GetComponent<ObliqueDepth2D>()==null)g.AddComponent<ObliqueDepth2D>();g.SetActive(true);occupied.Add(p);occupied.Add(to);return a;
    }
    static TreasureChest Chest(string name,Vector2 p)
    {var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Treasure Chest.prefab");var g=(GameObject)PrefabUtility.InstantiatePrefab(prefab);g.transform.SetParent(root,true);g.transform.position=p;g.name=name;g.layer=7;var c=g.GetComponent<TreasureChest>();c.ConfigureEditorDefaults();g.AddComponent<RPG1ChestSession>().chestId=name;ObliqueView2D.ConfigureActor(g.GetComponent<SpriteRenderer>());occupied.Add(p);return c;}
    static void Harvest(string name,Vector2 p,string item,int quantity,float regrow)
    {var data=Item(item);var s=Place(name,data.icon,p,1);var h=s.gameObject.AddComponent<RPG1Harvest>();h.item=data;h.quantity=quantity;h.regrowSeconds=regrow;h.label=item=="bone"?"搜集遗失骨料":item=="pumpkin"?"采摘南瓜":"采摘蘑菇";h.prefab=AssetDatabase.LoadAssetAtPath<Loot>("Assets/prefabs/Loot Prefab.prefab");occupied.Add(p);}
    static void BuildSheep()
    {var s=Place("Lost sheep - rescue to camp",Asset("SheepIdle0"),new Vector2(-27,6),1);s.gameObject.AddComponent<Rigidbody2D>().gravityScale=0;var c=s.gameObject.AddComponent<CapsuleCollider2D>();c.size=new Vector2(.6f,.35f);c.offset=new Vector2(0,.15f);var a=s.gameObject.AddComponent<RPG1LostSheep>();a.world=world;a.home=s.transform.position;a.meadow=new Rect(-29,3,3,4);a.idle=Enumerable.Range(0,8).Select(i=>Asset("SheepIdle"+i)).ToArray();a.walk=Enumerable.Range(0,6).Select(i=>Asset("SheepWalk"+i)).ToArray();occupied.Add(s.transform.position);}
    static void Tree(Vector2 p,float scale)
    {var s=Place("Animated original pine",Asset("WillowPineFrame0"),p,scale,new Vector2(.55f,.42f));var f=s.gameObject.AddComponent<WillowreachFlipbook>();f.frames=Enumerable.Range(0,4).Select(i=>Asset("WillowPineFrame"+i)).ToArray();f.framesPerSecond=5;f.phase=0;}
    static void Decorate()
    {
        var random=new System.Random(181126);
        for(int x=-31;x<=31;x+=2){Tree(new Vector2(x,-21),1.1f);Tree(new Vector2(x,23),1.1f);}for(int y=-19;y<23;y+=2){Tree(new Vector2(-31,y),1.1f);Tree(new Vector2(31,y),1.1f);}
        for(int i=0;i<400;i++)
        {
            var p=new Vector2(-29+(float)random.NextDouble()*58,-20+(float)random.NextDouble()*41);
            if(Road(p)<2.5f||world.safeCamp.Contains(p)||new Rect(-12,8,24,16).Contains(p)||new Rect(8,-10,13,12).Contains(p)||occupied.Any(o=>Vector2.Distance(o,p)<2.2f))continue;
            if(i%3==0)Tree(p,.85f+(float)random.NextDouble()*.3f);
            else if(i%5==0)Place("Grounded forest rock",Asset(i%2==0?"GroundRock05":"GroundRock06"),p,1,new Vector2(.65f,.45f));
            else Place("Forest undergrowth",Art("Assets/Sprites/Deco/"+(7+i%5).ToString("00")+".png"),p,.8f+(float)random.NextDouble()*.3f);
        }
        for(int i=0;i<5;i++)Place("Timber stockpile",Asset("Driftwood"),new Vector2(-15+i*.7f,-1),1,new Vector2(.6f,.4f));
        Sign("采集支路 ←    北部工事 ↑    配重站 →",new Vector2(-2,5.4f));Sign("林间救援 ←",new Vector2(-24,0));
    }
    static Sprite Map()=>RPG1MapTools.BakeAtlasSprite(); // camera bake, same as RPG2 atlas
}
