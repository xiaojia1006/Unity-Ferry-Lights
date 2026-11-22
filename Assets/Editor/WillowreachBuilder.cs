using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using Cinemachine;

public static partial class SceneExpansionWorkshop
{
    [MenuItem("Tools/RPG/Scene Expansion/Polish Expanded Terrain")]
    public static void PolishTerrain()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || SceneManager.GetActiveScene().path!=Expanded) return;
        scenery=UnityEngine.Object.FindObjectOfType<WillowreachWorld>().transform;
        var terrain=scenery.Find("Editable terrain");if(terrain!=null) UnityEngine.Object.DestroyImmediate(terrain.gameObject);
        BuildTerrain();
        var world=scenery.GetComponent<WillowreachWorld>();world.mapSprite=CreateMap();
        PolishVillageDetails();
        PolishShortcut(world);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        CheckWillowreach();
    }
    private static void PolishShortcut(WillowreachWorld world)
    {
        var shortcut=world.nodes.First(n=>n.kind==WillowreachNode.Kind.Shortcut);
        shortcut.transform.position=new Vector3(-25,9.5f,0);shortcut.title="修复返乡渡桥";shortcut.hint="三处天气观测完成后，恢复西侧渡桥";
        shortcut.unlock.transform.position=new Vector3(-23,8.6f,0);
        if(world.shortcutBridge==null)
        {
            Bridge(-23,2);
            world.shortcutBridge=scenery.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Crossing - plank bridge"&&Mathf.Abs(r.transform.position.x+23)<.1f).gameObject;
        }
        world.shortcutBridgeArea=new Rect(-23.7f,-4.3f,1.4f,12.6f);world.shortcutBridge.SetActive(false);shortcut.activate=world.shortcutBridge;
        var water=world.river.GetComponent<PolygonCollider2D>();
        foreach(var renderer in scenery.GetComponentsInChildren<SpriteRenderer>())
            if(renderer.name=="Boundary pine"&&water.OverlapPoint(renderer.transform.position))renderer.gameObject.SetActive(false);
    }
    private static void PolishVillageDetails()
    {
        var camp=scenery.GetComponentsInChildren<WillowreachNode>().First(n=>n.kind==WillowreachNode.Kind.Camp);
        var fire=Art("Assets/Sprites/Effects/Fire/Fire.png");
        if(fire!=null)
        {
            var texture=fire.texture;var frames=new List<Sprite>();
            // This atlas contains a row of 128px fire animation frames.
            int frameHeight=texture.height;int frameWidth=frameHeight;
            for(int x=0;x+frameWidth<=texture.width;x+=frameWidth)
                frames.Add(Crop("CampFire"+x,"Assets/Sprites/Effects/Fire/Fire.png",new Rect(x,0,frameWidth,frameHeight),new Vector2(.5f,.15f)));
            var sr=camp.GetComponent<SpriteRenderer>();sr.sprite=frames[0];camp.transform.localScale=Vector3.one*1.4f;
            var animation=camp.GetComponent<WillowreachFlipbook>();if(animation==null) animation=camp.gameObject.AddComponent<WillowreachFlipbook>();animation.frames=frames.ToArray();animation.framesPerSecond=8;
        }
        var sheepArt=Art("Assets/Sprites/Sheep/HappySheep_Idle.png");
        if(sheepArt!=null && scenery.Find("Village sheep 0")==null)
        {
            var tex=sheepArt.texture;
            var sprite=Crop("VillageSheep","Assets/Sprites/Sheep/HappySheep_Idle.png",new Rect(0,0,Mathf.Min(tex.height,tex.width),tex.height),new Vector2(.5f,.3f));
            for(int i=0;i<3;i++)Place("Village sheep "+i,sprite,new Vector2(-15+i*1.1f,-20-(i%2)*1.1f),1);
        }
        // Bring tiny station signs into proportion with actors, rather than giant scarecrows.
        foreach(var n in scenery.GetComponentsInChildren<WillowreachNode>())
            if(n.kind==WillowreachNode.Kind.Survey || n.kind==WillowreachNode.Kind.Shortcut) n.transform.localScale=Vector3.one*1.15f;
        foreach(var r in scenery.GetComponentsInChildren<SpriteRenderer>())
            if(r.name=="Field scarecrow") r.transform.localScale=Vector3.one*1.15f;
    }
    private static Transform scenery;
    private static System.Random random;
    private static List<WillowreachNode> nodes;
    private static readonly Vector2[] route = {
        new Vector2(-23,-14),new Vector2(-17,-13),new Vector2(-12,-15),new Vector2(-8,-14),
        new Vector2(-8,2),new Vector2(-8,13),new Vector2(-5,16),new Vector2(9,17),new Vector2(17,15),new Vector2(23,16),
        new Vector2(24,10),new Vector2(23,4),new Vector2(23,-9),new Vector2(26,-13),new Vector2(23,-15),
        new Vector2(9,-15),new Vector2(3,-13),new Vector2(-10,-14),new Vector2(-23,-14)};

    [MenuItem("Tools/RPG/Scene Expansion/2 Build Willowreach")]
    public static void BuildWillowreach()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(!File.Exists(Baseline)) throw new InvalidOperationException("Capture original first.");
        if(File.Exists(Expanded)) throw new InvalidOperationException("Expanded scene already exists. Edit it directly; no automatic overwrite.");
        AssetDatabase.CopyAsset(Baseline,Expanded);
        var scene=EditorSceneManager.OpenScene(Expanded);
        random=new System.Random(171115);nodes=new List<WillowreachNode>();
        scenery=new GameObject("Willowreach - authored world").transform;
        var world=scenery.gameObject.AddComponent<WillowreachWorld>();
        var river=UnityEngine.Object.FindObjectOfType<WaterSurface2D>();
        river.transform.SetParent(scenery,true);river.transform.position=new Vector3(0,6,0);river.transform.localScale=new Vector3(76,44,1);
        world.river=river;
        // These deletions are only in the copied scene, never in RPG1 or its snapshot.
        foreach(var grid in UnityEngine.Object.FindObjectsOfType<Grid>()) UnityEngine.Object.DestroyImmediate(grid.gameObject);
        foreach(var loot in UnityEngine.Object.FindObjectsOfType<Loot>()) UnityEngine.Object.DestroyImmediate(loot.gameObject);
        var teleport=scene.GetRootGameObjects().FirstOrDefault(r=>r.name=="Teleport");if(teleport!=null) UnityEngine.Object.DestroyImmediate(teleport);
        var player=UnityEngine.Object.FindObjectOfType<PlayerMovement>();player.transform.position=new Vector3(-22,-14,0);world.player=player.transform;
        var shops=UnityEngine.Object.FindObjectsOfType<ShopKeeper>().OrderBy(s=>s.name).ToArray();
        for(int i=0;i<shops.Length;i++) { shops[i].transform.position=new Vector3(-24+i*8,-10.5f,0);ObliqueView2D.ConfigureActor(shops[i].GetComponent<SpriteRenderer>()); }
        ObliqueView2D.RepairWorldPrompts(scene);
        var enemies=UnityEngine.Object.FindObjectsOfType<Enemy_Movement>();
        Vector2[] camps={new Vector2(18,16),new Vector2(28,17),new Vector2(-24,16),new Vector2(28,-15)};
        for(int i=0;i<enemies.Length;i++)
        {
            var enemy=enemies[i];var at=camps[i%camps.Length];enemy.transform.position=at;
            enemy.speed=1.6f;enemy.detectionRange=3.2f;enemy.loseTargetRange=5;enemy.attackrange=1.15f;enemy.minimumAttackRange=1.15f;enemy.attackHitDelay=.4f;enemy.attackDuration=1.1f;
            var health=enemy.GetComponent<Enemy_Health>();health.maxHealth=3+i/2;health.expReward=3+i/2;
            var combat=enemy.GetComponent<Enemy_Combat>();combat.damage=2;combat.weaponRange=1.35f;combat.minimumHitRange=1.35f;combat.attackCooldown=1;
            var guard=enemy.gameObject.AddComponent<WillowreachGuard>();guard.home=at;guard.leash=4.6f;
        }
        var chests=UnityEngine.Object.FindObjectsOfType<TreasureChest>();Vector2[] chestPlaces={new Vector2(-28,17),new Vector2(29,20),new Vector2(31,-17)};
        for(int i=0;i<chests.Length;i++) chests[i].transform.position=chestPlaces[i%3];
        BuildTerrain();
        BuildVillage();BuildLandmarks();BuildBorders();BuildDecor(river);
        Bridge(-8,6.4f);Bridge(23,-1.7f);
        world.bridges=new[]{new Rect(-8.7f,.1f,1.4f,12.6f),new Rect(22.3f,-8,1.4f,12.6f)};
        world.nodes=nodes.ToArray();world.weather=UnityEngine.Object.FindObjectOfType<WeatherCycle2D>();
        PolishVillageDetails();PolishShortcut(world);
        world.weather.currentWeather=WeatherCycle2D.Weather.Sunny;world.weather.minimumDuration=75;world.weather.maximumDuration=120;
        world.weather.SetWeather(WeatherCycle2D.Weather.Sunny);
        world.panelSprite=AssetDatabase.LoadAssetAtPath<BackpackTheme>("Assets/Resources/BackpackTheme.asset").slot;
        world.mapSprite=CreateMap();
        foreach(var camera in UnityEngine.Object.FindObjectsOfType<Camera>())
        { if(camera.targetTexture!=null) continue; camera.orthographic=true;camera.orthographicSize=6;camera.transform.rotation=Quaternion.identity;camera.transparencySortMode=TransparencySortMode.CustomAxis;camera.transparencySortAxis=Vector3.up;camera.backgroundColor=new Color(.19f,.28f,.23f); }
        foreach(var vcam in UnityEngine.Object.FindObjectsOfType<CinemachineVirtualCamera>())
        { vcam.m_Lens.OrthographicSize=6;vcam.Follow=player.transform; }
        var confiner=scene.GetRootGameObjects().FirstOrDefault(o=>o.name=="Confiner");
        if(confiner!=null)
        {
            confiner.transform.position=Vector3.zero;confiner.transform.localScale=Vector3.one;
            var poly=confiner.GetComponent<PolygonCollider2D>();if(poly!=null){poly.offset=Vector2.zero;poly.points=new[]{new Vector2(-38,-26),new Vector2(-38,26),new Vector2(38,26),new Vector2(38,-26)};poly.isTrigger=true;}
            foreach(var box in confiner.GetComponents<BoxCollider2D>()){box.offset=Vector2.zero;box.size=new Vector2(76,52);box.isTrigger=true;}
        }
        foreach(var conf in UnityEngine.Object.FindObjectsOfType<CinemachineConfiner>()) conf.InvalidatePathCache();
        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        // Keep existing launch order intact. The new scene is available without changing original startup.
        if(!EditorBuildSettings.scenes.Any(s=>s.path==Expanded)) EditorBuildSettings.scenes=EditorBuildSettings.scenes.Concat(new[]{new EditorBuildSettingsScene(Expanded,true)}).ToArray();
        if(SceneView.lastActiveSceneView!=null) SceneView.lastActiveSceneView.LookAt(new Vector3(-20,-10,0),Quaternion.identity,12,true);
        FixReportedIssues();Inspect();CheckWillowreach();
        Debug.Log("Willowreach ready. Original scene and immutable live snapshot untouched.");
    }

    private static Sprite Art(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    private static Sprite Crop(string name,string path,Rect rect,Vector2 pivot)
    {
        string asset=Folder+"/"+name+".asset";var old=AssetDatabase.LoadAssetAtPath<Sprite>(asset);if(old!=null) return old;
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var sprite=Sprite.Create(texture,rect,pivot,64,0,SpriteMeshType.FullRect);sprite.name=name;
        AssetDatabase.CreateAsset(sprite,asset);return sprite;
    }
    private static SpriteRenderer Place(string name,Sprite sprite,Vector2 at,float scale=1,bool solid=false)
    {
        var go=new GameObject(name);go.transform.SetParent(scenery,false);go.transform.position=at;go.transform.localScale=Vector3.one*scale;
        var sr=go.AddComponent<SpriteRenderer>();sr.sprite=sprite;
        if(solid)
        {
            var body=go.AddComponent<BoxCollider2D>();float width=sprite.bounds.size.x;
            body.size=new Vector2(width*.58f,.30f);body.offset=new Vector2(0,sprite.bounds.min.y+.22f);
        }
        var depth=go.AddComponent<ObliqueDepth2D>();if(!solid) depth.footOffset=sprite.bounds.min.y*scale;depth.Refresh();return sr;
    }
    private static Tilemap NewMap(string name,Transform parent,int order)
    { var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(parent,false);go.GetComponent<TilemapRenderer>().sortingOrder=order;return go.GetComponent<Tilemap>(); }
    private static float PathDistance(Vector2 p)
    {
        float result=float.MaxValue;
        for(int i=0;i<route.Length-1;i++)
        { Vector2 a=route[i],d=route[i+1]-a;result=Mathf.Min(result,Vector2.Distance(p,a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude))); }
        // Western branch is the optional return route to the ruins.
        result=Mathf.Min(result,Vector2.Distance(p,new Vector2(-23,Mathf.Clamp(p.y,-14,16))));
        return result;
    }
    private static void BuildTerrain()
    {
        var grid=new GameObject("Editable terrain",typeof(Grid));grid.transform.SetParent(scenery,false);
        var ground=NewMap("Ground - grass",grid.transform,-2000);var paths=NewMap("Paths - painted loop",grid.transform,-1800);
        var grass=AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/Grass Tiles/Tilemap_Flat_11.asset");
        var dirt=AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/Grass Tiles/Tilemap_Flat_15.asset");
        for(int x=-40;x<40;x++)for(int y=-28;y<28;y++)
        {
            var cell=new Vector3Int(x,y,0);ground.SetTile(cell,grass);ground.SetTileFlags(cell,TileFlags.None);
            float shade=.92f+Mathf.PerlinNoise((x+100)*.12f,(y+100)*.12f)*.08f;
            ground.SetColor(cell,new Color(shade,shade,shade));
            var p=new Vector2(x+.5f,y+.5f);
            bool village=new Vector2((p.x+21)/9.2f,(p.y+13)/4.7f).sqrMagnitude<1;
            float pathWidth=1.15f+Mathf.PerlinNoise((x+60)*.18f,(y+60)*.18f)*.65f;
            if(PathDistance(p)<pathWidth || village || Vector2.Distance(p,new Vector2(23,16))<5 || Vector2.Distance(p,new Vector2(-23,16))<4)
            { paths.SetTile(cell,dirt);paths.SetTileFlags(cell,TileFlags.None);paths.SetColor(cell,new Color(.78f,.72f,.57f)); }
        }
        // Natural path margins, not hard rectangular strips: use existing grass fringe sprites.
        var fringe=NewMap("Path edges",grid.transform,-1750);
        var painted=new HashSet<Vector3Int>();foreach(var c in paths.cellBounds.allPositionsWithin) if(paths.HasTile(c)) painted.Add(c);
        foreach(var cell in painted)
        {
            bool left=!painted.Contains(cell+Vector3Int.left),right=!painted.Contains(cell+Vector3Int.right),up=!painted.Contains(cell+Vector3Int.up),down=!painted.Contains(cell+Vector3Int.down);
            if(!left&&!right&&!up&&!down) continue;
            // A 9-slice of the existing yellow terrain gives paths a grassy silhouette.
            int col=left?0:right?2:1;int row=up?2:down?0:1;
            string name="PathEdge"+col+row;var tile=AssetDatabase.LoadAssetAtPath<Tile>(Folder+"/"+name+"Tile.asset");
            if(tile==null){tile=ScriptableObject.CreateInstance<Tile>();tile.sprite=Crop(name,"Assets/Tilemap_Flat.png",new Rect(320+col*64,64+row*64,64,64),new Vector2(.5f,.5f));tile.colliderType=Tile.ColliderType.None;AssetDatabase.CreateAsset(tile,Folder+"/"+name+"Tile.asset");}
            fringe.SetTile(cell,tile);fringe.SetTileFlags(cell,TileFlags.None);fringe.SetColor(cell,new Color(.78f,.72f,.57f));
            paths.SetTile(cell,null);
        }
    }
    private static WillowreachNode Node(SpriteRenderer sr,WillowreachNode.Kind kind,string title,string hint,string item,int quantity=1)
    {
        var node=sr.gameObject.AddComponent<WillowreachNode>();node.kind=kind;node.title=title;node.hint=hint;node.quantity=quantity;node.indicator=sr;
        if(!string.IsNullOrEmpty(item))node.item=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/"+item+".asset");nodes.Add(node);return node;
    }
    private static void BuildVillage()
    {
        string house="Assets/Sprites/Building/House/House_";
        Place("House_Blue - village shop",Art(house+"Blue.png"),new Vector2(-24,-8.8f),1.6f,true);
        Place("House_Yellow - farm",Art(house+"Yellow.png"),new Vector2(-16,-8.8f),1.6f,true);
        Place("House_Purple - rest",Art(house+"Purple.png"),new Vector2(-29,-13),1.45f,true);
        Place("Village watchtower",Art("Assets/Sprites/Building/Tower/Tower_Blue.png"),new Vector2(-12,-9),1.2f,true);
        Node(Place("Camp - safe rest",Art("Assets/Sprites/Deco/18.png"),new Vector2(-18,-15),1.8f),WillowreachNode.Kind.Camp,"村口营火","恢复最多10生命；30秒恢复一次",null);
        // Harvestable crop rows remain readable, with a lane through the middle.
        for(int x=0;x<4;x++)for(int y=0;y<2;y++)
            Node(Place("Pumpkin patch",Art("Assets/Sprites/Deco/13.png"),new Vector2(-27+x*1.2f,-19-y*1.2f),1.4f),WillowreachNode.Kind.Supply,"采摘南瓜","一次性补给 · 南瓜可在背包中使用","pumpkin",1);
        Place("Field scarecrow",Art("Assets/Sprites/Deco/18.png"),new Vector2(-21,-20),2);
    }
    private static void BuildLandmarks()
    {
        Place("Old mine entrance",Art("Assets/Sprites/GoldMine/GoldMine_Inactive.png"),new Vector2(24,20),2,true);
        Place("Mine watchtower",Art("Assets/Sprites/Building/Tower/Tower_Red.png"),new Vector2(30,13),1.3f,true);
        Place("Ruin castle",Art("Assets/Sprites/Building/Castle/Castle_Destroyed.png"),new Vector2(-26,21),1.8f,true);
        Place("Ruin tower",Art("Assets/Sprites/Building/Tower/Tower_Destroyed.png"),new Vector2(-17,20),1.6f,true);
        var mine=Node(Place("Survey - mine",Art("Assets/Sprites/Deco/16.png"),new Vector2(23,12),2),WillowreachNode.Kind.Survey,"晴光矿场站","调谐晴天，再按 E 记录","Gold",12);mine.requiredWeather=WeatherCycle2D.Weather.Sunny;
        var forest=Node(Place("Survey - forest",Art("Assets/Sprites/Deco/17.png"),new Vector2(21,-18),2),WillowreachNode.Kind.Survey,"雨露林地站","调谐雨天；雨天采蘑菇额外 +1","pumpkin",2);forest.requiredWeather=WeatherCycle2D.Weather.Rain;
        var ruin=Node(Place("Survey - ruins",Art("Assets/Sprites/Deco/16.png"),new Vector2(-22,13),2),WillowreachNode.Kind.Survey,"雪迹遗迹站","调谐雪天，雪地可留下脚印","Gold",18);ruin.requiredWeather=WeatherCycle2D.Weather.Snow;
        var barrier=new GameObject("Shortcut - fallen timber");barrier.transform.SetParent(scenery,false);barrier.transform.position=new Vector3(-23,0,0);
        var collider=barrier.AddComponent<BoxCollider2D>();collider.size=new Vector2(4,1.1f);
        var wood=Crop("Driftwood","Assets/Sprites/Bridge/Bridge_All.png",new Rect(64,64,64,64),new Vector2(.5f,.5f));
        for(int i=-1;i<=1;i++){var sr=Place("Shortcut timber",wood,new Vector2(-23+i*1.1f,0),1.4f);sr.transform.SetParent(barrier.transform,true);}
        var shortcut=Node(Place("Shortcut lever",Art("Assets/Sprites/Deco/17.png"),new Vector2(-25,1.8f),2),WillowreachNode.Kind.Shortcut,"开启返乡捷径","三处观测完成后，移开倒木","Gold",25);shortcut.unlock=barrier;
        for(int i=0;i<9;i++)
        { float angle=i*2.399f;var p=new Vector2(26+Mathf.Cos(angle)*(3+i*.43f),-16+Mathf.Sin(angle)*(3+i*.37f));
          Node(Place("Forest mushroom",Art("Assets/Sprites/Deco/0"+(1+i%3)+".png"),p,1.7f),WillowreachNode.Kind.Supply,"采集蘑菇","材料；雨天采集额外 +1","Mushroom",2); }
        for(int i=0;i<5;i++)Node(Place("Mine ore cache",Art("Assets/Sprites/Resources/G_Idle_(NoShadow).png"),new Vector2(15+i*2,21),.8f),WillowreachNode.Kind.Supply,"矿石补给","旧矿场留下的少量金币","Gold",3);
        for(int i=0;i<5;i++) Place("Ruin remnants",Art("Assets/Sprites/Deco/14.png"),new Vector2(-30+i*2.4f,18+(i%2)*2),1.1f);
    }
    private static void Bridge(float x,float y)
    {
        var bridge=Crop("WillowBridge","Assets/Sprites/Bridge/Bridge_All.png",new Rect(0,16,64,160),new Vector2(.5f,.5f));
        var sr=Place("Crossing - plank bridge",bridge,new Vector2(x,y),1);UnityEngine.Object.DestroyImmediate(sr.GetComponent<ObliqueDepth2D>());
        RepairBridge(sr);
    }
    private static void Wall(string name,Vector2 p,Vector2 size)
    {var go=new GameObject(name);go.transform.SetParent(scenery,false);go.transform.position=p;go.AddComponent<BoxCollider2D>().size=size;}
    private static Sprite TreeSprite => Crop("WillowPine","Assets/Sprites/Tree/Tree.png",new Rect(0,384,192,192),new Vector2(.5f,.08f));
    private static void BuildBorders()
    {
        Wall("North boundary",new Vector2(0,25),new Vector2(77,1));Wall("South boundary",new Vector2(0,-25),new Vector2(77,1));
        Wall("West boundary",new Vector2(-37.5f,0),new Vector2(1,51));Wall("East boundary",new Vector2(37.5f,0),new Vector2(1,51));
        for(int x=-37;x<=37;x+=3)for(int side=-1;side<=1;side+=2) Place("Boundary pine",TreeSprite,new Vector2(x,side*24+.4f*(float)random.NextDouble()),.95f+(float)random.NextDouble()*.2f,true);
        for(int y=-22;y<24;y+=3)for(int side=-1;side<=1;side+=2) Place("Boundary pine",TreeSprite,new Vector2(side*36.5f,y),1,true);
    }
    private static void BuildDecor(WaterSurface2D river)
    {
        var water=river.GetComponent<PolygonCollider2D>();Physics2D.SyncTransforms();
        var existing=UnityEngine.Object.FindObjectsOfType<Collider2D>().Where(c=>!c.isTrigger).ToArray();
        for(int i=0;i<460;i++)
        {
            var p=new Vector2(-34+(float)random.NextDouble()*68,-22+(float)random.NextDouble()*44);
            if(PathDistance(p)<3 || new Rect(-32,-22,23,16).Contains(p) || new Rect(13,10,20,13).Contains(p) || new Rect(-32,10,19,13).Contains(p) || nodes.Any(n=>Vector2.Distance(n.transform.position,p)<2.5f)) continue;
            if(water.OverlapPoint(p)||water.OverlapPoint(p+Vector2.up*1.8f)||water.OverlapPoint(p-Vector2.up*1.8f)) continue;
            if(existing.Any(c=>Vector2.Distance(c.bounds.ClosestPoint(p),p)<1.6f)) continue;
            if(i%3==0) Place("Pine grove",TreeSprite,p,.85f+(float)random.NextDouble()*.4f,true);
            else {int index=i%4==0?4+i%3:7+i%3;Place("Ground detail",Art("Assets/Sprites/Deco/"+index.ToString("00")+".png"),p,1.3f,i%4==0);}
        }
        // Shore reeds and stones follow actual river geometry, never arbitrary blue land.
        for(int x=-33;x<34;x+=2)for(int y=-12;y<14;y++)
        {
            var p=new Vector2(x,y);
            if(!water.OverlapPoint(p)&&water.OverlapPoint(p+Vector2.up*.8f)&&PathDistance(p)>3)
                Place("Shore reeds",Art("Assets/Sprites/Deco/"+(x%3==0?"11":"10")+".png"),p,1.5f);
        }
    }
    private static Sprite CreateMap()
    {
        const int w=304,h=208;var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);
        var water=UnityEngine.Object.FindObjectOfType<WaterSurface2D>().GetComponent<PolygonCollider2D>();
        for(int y=0;y<h;y++)for(int x=0;x<w;x++)
        { var p=new Vector2((x/(float)w-.5f)*76,(y/(float)h-.5f)*52);var color=new Color(.27f,.40f,.31f);if(PathDistance(p)<1.7f)color=new Color(.68f,.60f,.40f);if(water.OverlapPoint(p))color=new Color(.23f,.48f,.57f);if((Mathf.Abs(p.x+8)<.9f&&Mathf.Abs(p.y-6.4f)<6.3f)||(Mathf.Abs(p.x-23)<.9f&&Mathf.Abs(p.y+1.7f)<6.3f))color=new Color(.7f,.57f,.37f);texture.SetPixel(x,y,color);}
        texture.Apply();string path=Folder+"/FieldMap.png";File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);AssetDatabase.ImportAsset(path);
        var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
