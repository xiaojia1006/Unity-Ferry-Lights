using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static partial class RPG2Workshop
{
    static Transform buildRoot;
    static RPG2Adventure adventure;
    static PolygonCollider2D riverShape;
    static readonly List<RPG2Point> builtPoints=new List<RPG2Point>();
    static readonly List<Vector2> used=new List<Vector2>();
    static Tilemap newRoad,newEdges;
    static readonly List<Vector2[]> roads=new List<Vector2[]>();
    static Sprite Art(string path)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path) ?? AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if(sprite!=null)return sprite;
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);if(texture==null)throw new Exception("Missing texture: "+path);
        string destination="Assets/RPG2Expansion/Art-"+Path.GetFileNameWithoutExtension(path)+".asset";
        sprite=AssetDatabase.LoadAssetAtPath<Sprite>(destination);if(sprite!=null)return sprite;
        sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),64);sprite.name=Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(sprite,destination);return sprite;
    }
    static Sprite ExpansionArt(string name)=>Art("Assets/SceneExpansion/"+name+".asset");
    static ItemSO Item(string name)=>AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/"+name+".asset");
    static partial void RunCommand(string command)
    {
        if(command=="build")BuildRPG2();
        else if(command=="finish")FinishRPG2();
        else if(command=="polish")PolishRPG2();
        else if(command=="repair")RepairRPG2();
        else if(command=="layers")RepairTerrainLayers();
        else if(command=="fix-bugs")FixReportedBugs();
        else if(command=="bug-check")CheckReportedBugs();
        else if(command=="bridge-layers")FixBridgeLayers();
        else if(command=="bridge-preview")PreviewBridgeLayers();
        else if(command=="capture-bridge")ScreenCapture.CaptureScreenshot("Logs/RPG2BridgeLayersFixed.png");
        else if(command=="prompt-view")
        {
            var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();w.CloseMenu();var p=w.points.First(n=>n.kind==RPG2Point.Kind.Camp);ViewRPG2((Vector2)p.transform.position+Vector2.down*1.2f);
            w.Notify("苇岸渡口：R 打开委托，E 与附近物体互动。任务奖励在下方图标栏查看。");
        }
        else if(command=="capture-prompt")ScreenCapture.CaptureScreenshot("Logs/RPG2PromptFixed.png");
        else if(command=="capture-guard")ScreenCapture.CaptureScreenshot("Logs/RPG2GuardFixed.png");
        else if(command=="map")BakeAtlas();
        else if(command=="scene-check")CheckScene();
        else if(command=="play-check")BeginPlayChecks();
        else if(command=="quest-view"){UnityEngine.Object.FindObjectOfType<RPG2Adventure>().OpenMenu(false);ScreenCapture.CaptureScreenshot("Logs/RPG2QuestPreview.png");}
        else if(command=="map-view"){var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();w.CloseMenu();w.OpenMenu(true);ScreenCapture.CaptureScreenshot("Logs/RPG2MapPreview.png");}
        else if(command=="camp-view")ViewRPG2(new Vector2(-2,-9));
        else if(command=="north-view")ViewRPG2(new Vector2(-19,12));
        else if(command=="east-view")ViewRPG2(new Vector2(17,10));
        else if(command=="close"){UnityEngine.Object.FindObjectOfType<RPG2Adventure>().CloseMenu();}
        else throw new Exception("Unknown RPG2 workshop request: "+command);
    }
    static void RepairTerrainLayers()
    {
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();
        foreach(var map in UnityEngine.Object.FindObjectsOfType<Tilemap>())
        {
            var depth=map.GetComponent<ObliqueTilemap2D>();
            int order=map.name=="Ground"?-30000:map.name=="xiaolu"?-24000:map.transform.IsChildOf(w.transform)?(map.name.Contains("fringes")?-18999:-19000):-15000;
            if(depth!=null)depth.terrainOrder=order;map.GetComponent<TilemapRenderer>().sortingOrder=order;
        }
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);BakeAtlas();EditorSceneManager.SaveScene(w.gameObject.scene);CheckScene();Overview();
    }
    static void RepairRPG2()
    {
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();
        var water=UnityEngine.Object.FindObjectOfType<WaterSurface2D>();
        // Original Water had out-of-plane rotation, so rendered shoreline and 2D physics disagreed.
        // Preserve its XY footprint while flattening the sheet for the XY game.
        var t=water.transform;var scale=t.localScale;float projectedY=t.TransformVector(Vector3.up).y;
        t.localRotation=Quaternion.identity;scale.y=Mathf.Abs(projectedY);t.localScale=scale;
        var guard=w.guards[1];guard.home=new Vector2(-20,16.2f);guard.end=new Vector2(-16,16.2f);guard.transform.position=guard.home;
        foreach(var point in w.points)
        {
            if(point.kind==RPG2Point.Kind.Fishing)continue;
            var shape=water.GetComponent<PolygonCollider2D>();int tries=0;
            while(shape.OverlapPoint(point.transform.position)&&tries++<14)point.transform.position+=Vector3.down*.5f;
        }
        // Water draws over terrain; bridge deck alone draws over water.
        var sr=water.GetComponent<SpriteRenderer>();sr.sortingOrder=-100;
        Physics2D.SyncTransforms();
        foreach(var prop in w.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Animated riverside pine"||s.name=="Forest floor rock"||s.name=="Meadow grasses and flowers"||s.name=="Riverbank reed cluster"))
            if(water.GetComponent<PolygonCollider2D>().OverlapPoint(prop.transform.position))prop.gameObject.SetActive(false);
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);BakeAtlas();EditorSceneManager.SaveScene(w.gameObject.scene);CheckScene();Overview();
    }
    static void PolishRPG2()
    {
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();buildRoot=w.transform;
        var water=UnityEngine.Object.FindObjectOfType<WaterSurface2D>().GetComponent<PolygonCollider2D>();
        var ground=UnityEngine.Object.FindObjectsOfType<Tilemap>().First(t=>t.name=="Ground");
        var grass=AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/Grass Tiles/Tilemap_Flat_11.asset");
        for(int x=-34;x<31;x++)for(int y=-19;y<21;y++)if(!ground.HasTile(new Vector3Int(x,y,0)))ground.SetTile(new Vector3Int(x,y,0),grass);
        foreach(var p in w.points.Where(p=>p.kind!=RPG2Point.Kind.Fishing))
        {
            int count=0;while(water.OverlapPoint(p.transform.position)&&count++<12)p.transform.position+=Vector3.down*.5f;
        }
        var repair=w.points.First(p=>p.kind==RPG2Point.Kind.Bridge);repair.transform.position=new Vector2(-10,-4.8f);
        foreach(Transform child in w.bridge.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        for(int i=0;i<19;i++)
        {
            var sr=Place("Crossing plank "+i,ExpansionArt("BridgeBoardRepeat"),new Vector2(-10,-5+i*.55f),1.5f);UnityEngine.Object.DestroyImmediate(sr.GetComponent<ObliqueDepth2D>());sr.sortingOrder=10;sr.transform.SetParent(w.bridge.transform,true);
        }
        w.bridgeArea=new Rect(-11.05f,-5.5f,2.1f,11);
        var oldPost=w.transform.Find("Broken bridge posts");if(oldPost!=null)oldPost.position=new Vector2(-10,-4.9f);
        var paths=w.GetComponentsInChildren<Tilemap>().First(t=>t.name.StartsWith("New footpaths"));
        var dirt=AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/Grass Tiles/Tilemap_Flat_15.asset");
        foreach(var segment in new[]{new[]{new Vector2(-4,-9),new Vector2(-10,-5)},new[]{new Vector2(-10,5),new Vector2(-6,7)}})
        {
            for(int i=0;i<50;i++)
            {
                var at=Vector2.Lerp(segment[0],segment[1],i/49f);
                for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)
                {var cell=paths.WorldToCell(at)+new Vector3Int(dx,dy,0);if(!water.OverlapPoint(paths.GetCellCenterWorld(cell)))paths.SetTile(cell,dirt);}
            }
        }
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);BakeAtlas();EditorSceneManager.SaveScene(w.gameObject.scene);CheckScene();Overview();
    }
    static void FinishRPG2()
    {
        adventure=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();if(adventure==null)throw new Exception("Missing expansion");
        buildRoot=adventure.transform;riverShape=UnityEngine.Object.FindObjectOfType<WaterSurface2D>().GetComponent<PolygonCollider2D>();
        builtPoints.Clear();builtPoints.AddRange(UnityEngine.Object.FindObjectsOfType<RPG2Point>());used.Clear();used.AddRange(builtPoints.Select(p=>(Vector2)p.transform.position));
        foreach(var g in UnityEngine.Object.FindObjectsOfType<Enemy_Movement>(true).Where(e=>e.GetComponent<RPG2Guard>()==null&&e.gameObject.scene==adventure.gameObject.scene).ToArray())UnityEngine.Object.DestroyImmediate(g.gameObject);
        foreach(var child in buildRoot.Cast<Transform>().Where(t=>t.name=="Animated riverside pine"||t.name=="Forest floor rock"||t.name=="Meadow grasses and flowers"||t.name=="Riverbank reed cluster").ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
        foreach(var g in adventure.guards){used.Add(g.home);used.Add(g.end);}
        roads.Clear();roads.Add(new[]{new Vector2(-2,-9),new Vector2(-8,-11),new Vector2(-15,-13),new Vector2(-25,-16)});roads.Add(new[]{new Vector2(0,-5),new Vector2(0,6)});
        roads.Add(new[]{new Vector2(0,5),new Vector2(-7,7),new Vector2(-17,8),new Vector2(-22,14),new Vector2(-20,17)});roads.Add(new[]{new Vector2(0,5),new Vector2(13,10),new Vector2(25,16)});
        DecorateRPG2();adventure.points=builtPoints.ToArray();
        foreach(var map in UnityEngine.Object.FindObjectsOfType<Tilemap>())
        {
            int order=map.name=="Ground"?-30000:map.name.StartsWith("New footpath fringes")?-18999:map.name.StartsWith("New footpaths")?-19000:map.GetComponent<TilemapRenderer>().sortingOrder;
            var depth=map.GetComponent<ObliqueTilemap2D>();if(depth!=null)depth.terrainOrder=order;map.GetComponent<TilemapRenderer>().sortingOrder=order;
        }
        Physics2D.SyncTransforms();EditorSceneManager.MarkSceneDirty(adventure.gameObject.scene);EditorSceneManager.SaveScene(adventure.gameObject.scene);BakeAtlas();EditorSceneManager.SaveScene(adventure.gameObject.scene);CheckScene();Overview();
    }
    static void ViewRPG2(Vector2 at)
    {
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();w.CloseMenu();w.Player.transform.position=at;w.Player.rb.position=at;w.Player.rb.velocity=Vector2.zero;
    }
    [MenuItem("Tools/RPG/RPG2 Workshop/Expand Existing RPG2")]
    public static void BuildRPG2()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play first");
        var scene=SceneManager.GetActiveScene();if(scene.path!=ScenePath)throw new Exception("Open RPG2 before expanding");
        if(UnityEngine.Object.FindObjectOfType<RPG2Adventure>()!=null)throw new Exception("RPG2 expansion already exists; edit native objects instead of rebuilding");
        string backup="Backups/RPG2Expansion-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        File.Copy(ScenePath,backup+"/RPG2-disk.unity");File.Copy(ScenePath+".meta",backup+"/RPG2.unity.meta");
        EditorSceneManager.SaveScene(scene,backup+"/RPG2-live.unity",true);
        File.Copy("Assets/Scenes/RPG1.unity",backup+"/RPG1-unchanged.unity");
        File.WriteAllText("Backups/LatestRPG2Expansion.txt",backup);
        if(!AssetDatabase.IsValidFolder("Assets/RPG2Expansion"))AssetDatabase.CreateFolder("Assets","RPG2Expansion");
        var enemyScene=EditorSceneManager.OpenScene("Assets/Scenes/RPG1.unity",OpenSceneMode.Additive);
        var enemySource=enemyScene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Enemy_Movement>(true)).FirstOrDefault();
        if(enemySource==null)throw new Exception("Original enemy art/controller missing");
        SceneManager.SetActiveScene(scene);
        var prototype=UnityEngine.Object.Instantiate(enemySource.gameObject);prototype.SetActive(false);
        foreach(var component in prototype.GetComponents<RPG1Sentinel>())UnityEngine.Object.DestroyImmediate(component);
        EditorSceneManager.CloseScene(enemyScene,true);
        buildRoot=new GameObject("RPG2 - Reedbank Crossing expansion").transform;
        adventure=buildRoot.gameObject.AddComponent<RPG2Adventure>();
        var theme=Resources.Load<BackpackTheme>("BackpackTheme");adventure.parchment=Art("Assets/SceneExpansion/JournalParchment.png");adventure.ribbon=theme.ribbon;adventure.button=theme.button;
        adventure.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Login/LoginChinese.asset");
        adventure.mushroom=Item("Mushroom");adventure.pumpkin=Item("pumpkin");adventure.gold=Item("Gold");adventure.experience=Item("EXP");adventure.meat=Item("meat");
        builtPoints.Clear();used.Clear();roads.Clear();
        var water=UnityEngine.Object.FindObjectOfType<WaterSurface2D>();riverShape=water.GetComponent<PolygonCollider2D>();Physics2D.SyncTransforms();
        // Archive the broken tutorial UI in place. Other scenes and shared prefabs are untouched.
        foreach(var g in scene.GetRootGameObjects().Where(g=>g.name=="QuestCanvas"))g.SetActive(false);
        foreach(var b in UnityEngine.Object.FindObjectsOfType<QuestBoard>())b.enabled=false;
        var board=scene.GetRootGameObjects().First(g=>g.name=="QuestBoard");
        foreach(var circle in board.GetComponents<CircleCollider2D>())circle.isTrigger=true;
        foreach(var box in board.GetComponents<BoxCollider2D>()){box.size=new Vector2(.55f,.35f);box.offset=new Vector2(0,-.45f);}
        var boardPoint=board.AddComponent<RPG2Point>();boardPoint.kind=RPG2Point.Kind.Board;boardPoint.title="查看渡口委托";boardPoint.description="接取四项渡口委托，R 可随时查看进度。";builtPoints.Add(boardPoint);used.Add(board.transform.position);
        foreach(var npc in UnityEngine.Object.FindObjectsOfType<NPC>())
        {foreach(var circle in npc.GetComponents<CircleCollider2D>())circle.isTrigger=true;used.Add(npc.transform.position);}
        foreach(var house in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>()).Where(s=>s.name.StartsWith("House_")))used.Add(house.transform.position);
        Terrain();CampAndSouth();NorthAndBeacons();Guards(prototype);DecorateRPG2();
        UnityEngine.Object.DestroyImmediate(prototype);
        adventure.points=builtPoints.ToArray();
        ObliqueView2D.Configure(scene);
        newRoad.GetComponent<ObliqueTilemap2D>().terrainOrder=-19000;
        newEdges.GetComponent<ObliqueTilemap2D>().terrainOrder=-18999;
        foreach(var map in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Tilemap>()))
            if(map.name=="Ground") {map.GetComponent<TilemapRenderer>().sortingOrder=-30000;map.GetComponent<ObliqueTilemap2D>().terrainOrder=-30000;}
        newRoad.GetComponent<TilemapRenderer>().sortingOrder=-19000;newEdges.GetComponent<TilemapRenderer>().sortingOrder=-18999;
        Physics2D.SyncTransforms();
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
        BakeAtlas();EditorSceneManager.SaveScene(scene);
        PolishRPG2();RepairRPG2();RepairTerrainLayers();
        Debug.Log("RPG2 expanded on original authored terrain. Backup: "+backup);
    }
    static GameObject Obj(string name,Vector2 at)
    {var g=new GameObject(name);g.transform.SetParent(buildRoot,false);g.transform.position=at;return g;}
    static SpriteRenderer Place(string name,Sprite sprite,Vector2 at,float scale=1,Vector2 footprint=default(Vector2))
    {
        if(sprite==null)throw new Exception("Missing sprite: "+name);
        var g=Obj(name,at);g.transform.localScale=Vector3.one*scale;var sr=g.AddComponent<SpriteRenderer>();sr.sprite=sprite;
        if(footprint.sqrMagnitude>0){g.layer=7;var c=g.AddComponent<BoxCollider2D>();c.size=footprint/scale;c.offset=new Vector2(0,sprite.bounds.min.y+footprint.y/(2*scale));}
        g.AddComponent<ObliqueDepth2D>();return sr;
    }
    static RPG2Point Point(string name,RPG2Point.Kind kind,Sprite art,Vector2 at,float scale=1,string description="")
    {var sr=Place(name,art,at,scale);var p=sr.gameObject.AddComponent<RPG2Point>();p.kind=kind;p.title=name;p.description=description;builtPoints.Add(p);used.Add(at);return p;}
    static void Building(string name,string path,Vector2 at,float scale=1.25f)
    {
        var sr=Place(name,Art(path),at,scale,new Vector2(2,1));
        sr.transform.position=new Vector3(at.x,at.y-sr.sprite.bounds.min.y*scale,0);used.Add(at);
    }
    static void Tree(Vector2 at,float scale=1)
    {
        var sr=Place("Animated riverside pine",ExpansionArt("WillowPineFrame0"),at,scale,new Vector2(.5f,.35f));
        var flip=sr.gameObject.AddComponent<WillowreachFlipbook>();flip.frames=Enumerable.Range(0,4).Select(i=>ExpansionArt("WillowPineFrame"+i)).ToArray();flip.framesPerSecond=5;
    }
    static Tilemap TileLayer(string name,int order)
    {
        var go=new GameObject(name,typeof(Tilemap),typeof(TilemapRenderer));go.transform.SetParent(buildRoot.Find("Editable additions"),false);go.GetComponent<TilemapRenderer>().sortingOrder=order;return go.GetComponent<Tilemap>();
    }
    static void Terrain()
    {
        var grid=Obj("Editable additions",Vector2.zero);grid.AddComponent<Grid>();newRoad=TileLayer("New footpaths - original paths retained",-29000);newEdges=TileLayer("New footpath fringes",-28999);
        roads.Add(new[]{new Vector2(-2,-9),new Vector2(-8,-11),new Vector2(-15,-13),new Vector2(-25,-16)});
        roads.Add(new[]{new Vector2(-2,-9),new Vector2(0,-5),new Vector2(0,-3)});
        roads.Add(new[]{new Vector2(0,5),new Vector2(-7,7),new Vector2(-17,8),new Vector2(-22,14),new Vector2(-20,17)});
        roads.Add(new[]{new Vector2(0,5),new Vector2(7,8),new Vector2(13,10),new Vector2(21,13),new Vector2(25,16)});
        roads.Add(new[]{new Vector2(13,10),new Vector2(21,5),new Vector2(27,4)});
        var dirt=AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Sprites/Grass Tiles/Tilemap_Flat_15.asset");
        var cells=new HashSet<Vector3Int>();
        for(int x=-33;x<30;x++)for(int y=-18;y<20;y++)
        {
            Vector2 p=new Vector2(x+.5f,y+.5f);
            bool clearing=(p-new Vector2(-3,-10)).sqrMagnitude<14 || (p-new Vector2(-21,15)).sqrMagnitude<18 || (p-new Vector2(21,13)).sqrMagnitude<12;
            if(!riverShape.OverlapPoint(p)&&(DistanceRoad(p)<.65f+Mathf.PerlinNoise(x*.27f+80,y*.27f+40)*.4f||clearing))cells.Add(new Vector3Int(x,y,0));
        }
        foreach(var cell in cells)
        {
            bool l=!cells.Contains(cell+Vector3Int.left),r=!cells.Contains(cell+Vector3Int.right),u=!cells.Contains(cell+Vector3Int.up),d=!cells.Contains(cell+Vector3Int.down);
            if(l||r||u||d)
            {var edge=AssetDatabase.LoadAssetAtPath<TileBase>("Assets/SceneExpansion/PathEdge"+(l?0:r?2:1)+(u?2:d?0:1)+"Tile.asset");newEdges.SetTile(cell,edge??dirt);}
            else newRoad.SetTile(cell,dirt);
        }
        var ground=UnityEngine.Object.FindObjectsOfType<Tilemap>().First(t=>t.name=="Ground");
        foreach(var cell in ground.cellBounds.allPositionsWithin)
        {ground.SetTileFlags(cell,TileFlags.None);float shade=.92f+Mathf.PerlinNoise(cell.x*.14f+100,cell.y*.14f+100)*.08f;ground.SetColor(cell,new Color(shade,shade,shade));}
    }
    static float DistanceRoad(Vector2 p)
    {
        float best=999;foreach(var road in roads)for(int i=1;i<road.Length;i++){var a=road[i-1];var ab=road[i]-a;float t=Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);best=Mathf.Min(best,Vector2.Distance(p,a+ab*t));}return best;
    }
    static void CampAndSouth()
    {
        var camp=Point("营火 · 热汤交付",RPG2Point.Kind.Camp,ExpansionArt("CampFire0"),new Vector2(-4,-9),1.15f,"恢复生命，也可交付3蘑菇和2南瓜。");
        var fire=camp.gameObject.AddComponent<WillowreachFlipbook>();fire.frames=Enumerable.Range(0,7).Select(i=>ExpansionArt("CampFire"+(i*128))).ToArray();fire.framesPerSecond=8;
        Building("Boatkeeper hut","Assets/Sprites/Building/House/House_Blue.png",new Vector2(-7,-8));
        Place("Camp supply crates",ExpansionArt("Driftwood"),new Vector2(-8.5f,-10.7f),1.1f,new Vector2(1,.5f));
        Point("渡口告示：R 查看委托",RPG2Point.Kind.Guide,Art("Assets/Sprites/Deco/17.png"),new Vector2(-1,-12),.8f,"R 委托 · Tab 地图 · E 互动。沿南岸找木料，修桥后前往北岸。");
        var timberPositions=new[]{new Vector2(-10,-10),new Vector2(-18,-14),new Vector2(5,-16)};
        for(int i=0;i<3;i++)Point("收集修桥木料 "+(i+1),RPG2Point.Kind.Timber,ExpansionArt("Driftwood"),timberPositions[i],1.25f,"修桥木料，不占背包格。");
        var mushrooms=new[]{new Vector2(-11,-13),new Vector2(-13,-15),new Vector2(-8,-16),new Vector2(-23,-16),new Vector2(5,-13),new Vector2(8,-16)};
        foreach(var p in mushrooms)Point("采集蘑菇",RPG2Point.Kind.Mushroom,adventure.mushroom.icon,p,.8f);
        foreach(var p in new[]{new Vector2(-6,-14),new Vector2(-5,-16),new Vector2(-3,-16),new Vector2(-25,-17)})Point("采摘南瓜",RPG2Point.Kind.Pumpkin,adventure.pumpkin.icon,p,.9f);
        Place("Pumpkin patch scarecrow",Art("Assets/Sprites/Deco/18.png"),new Vector2(-4,-14),1);
        Point("修复渡桥",RPG2Point.Kind.Bridge,Art("Assets/Sprites/Deco/16.png"),new Vector2(0,-3.6f),.75f,"寻找南岸三捆木料后，在这里修桥。");
        adventure.bridge=Obj("Repaired crossing - plank segments",Vector2.zero);
        for(int i=0;i<14;i++)
        {
            var sr=Place("Crossing plank "+i,ExpansionArt("BridgeBoardRepeat"),new Vector2(0,-3+i*.55f),1.5f);UnityEngine.Object.DestroyImmediate(sr.GetComponent<ObliqueDepth2D>());sr.sortingOrder=10;sr.transform.SetParent(adventure.bridge.transform,true);
        }
        adventure.bridgeArea=new Rect(-1.0f,-3.4f,2.0f,8.5f);adventure.bridge.SetActive(false);
        var pole=Place("Broken bridge posts",ExpansionArt("BridgeHeadCap"),new Vector2(0,-3.5f),1.5f);UnityEngine.Object.DestroyImmediate(pole.GetComponent<ObliqueDepth2D>());pole.sortingOrder=11;
        Point("河岸垂钓",RPG2Point.Kind.Fishing,Art("Assets/Sprites/Deco/13.png"),new Vector2(-11,-7),.85f,"按 E 抛竿，等鱼儿咬钩提示后再按 E 收竿，成功得到肉。");
        for(int i=0;i<4;i++){var deck=Place("Fishing pier planks",ExpansionArt("BridgeBoardRepeat"),new Vector2(-11,-6.7f+i*.4f),1.2f);UnityEngine.Object.DestroyImmediate(deck.GetComponent<ObliqueDepth2D>());deck.sortingOrder=10;}
        var sheep=Point("护送迷路小羊",RPG2Point.Kind.Sheep,ExpansionArt("SheepIdle0"),new Vector2(-24,-14),1,"E 开始/停止跟随。沿南岸带回营火，获得14金币。");adventure.sheep=sheep.transform;
        adventure.sheepIdle=Enumerable.Range(0,8).Select(i=>ExpansionArt("SheepIdle"+i)).ToArray();adventure.sheepWalk=Enumerable.Range(0,6).Select(i=>ExpansionArt("SheepWalk"+i)).ToArray();
    }
    static void NorthAndBeacons()
    {
        Building("Ruined customs house","Assets/Sprites/Building/Castle/Castle_Destroyed.png",new Vector2(-22,17),1.4f);
        Building("Eastern old mine","Assets/Sprites/GoldMine/GoldMine_Inactive.png",new Vector2(25,16),1.4f);
        Building("Forest watchtower","Assets/Sprites/Building/Tower/Tower_Yellow.png",new Vector2(16,13),1.25f);
        Building("Ruins watchtower","Assets/Sprites/Building/Tower/Tower_Purple.png",new Vector2(-26,15),1.25f);
        var inn=Point("旅舍休息",RPG2Point.Kind.Inn,Art("Assets/Sprites/Deco/15.png"),new Vector2(10.7f,1),.6f,"恢复最多12生命，与营火共用30秒冷却。");
        var positions=new[]{new Vector2(-5,6),new Vector2(14,10),new Vector2(-18,15)};
        for(int i=0;i<3;i++)
        {
            var beacon=Point(new[]{"水边信标","林地信标","遗迹信标"}[i],RPG2Point.Kind.Beacon,Art("Assets/Sprites/Deco/17.png"),positions[i],.9f,"点灯暗号：水边 → 遗迹 → 林地。");beacon.number=i;
            var glow=Place("Beacon flame",ExpansionArt("CampFire0"),positions[i]+Vector2.up*.9f,.45f);glow.transform.SetParent(beacon.transform,true);var flip=glow.gameObject.AddComponent<WillowreachFlipbook>();flip.frames=Enumerable.Range(0,7).Select(f=>ExpansionArt("CampFire"+(f*128))).ToArray();flip.framesPerSecond=8;beacon.glow=glow.gameObject;glow.gameObject.SetActive(false);
        }
        var chestArt=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Treasure Chest.prefab").GetComponent<SpriteRenderer>().sprite;
        Point("开启遗迹宝藏",RPG2Point.Kind.Treasure,chestArt,new Vector2(-22,15),1,"三灯齐明且击败四名守卫后领取32金币、8经验。");
        Point("古老渡口路标",RPG2Point.Kind.Guide,Art("Assets/Sprites/Deco/16.png"),new Vector2(4,9),.8f,"古老暗号：水边第一盏，遗迹第二盏，林地最后亮。西北是遗迹，东北是矿场。");
        for(int i=0;i<6;i++)Place("Mine ore stock",ExpansionArt(i%2==0?"GroundRock05":"GroundRock06"),new Vector2(25+i%3*.8f,12+i/3*.8f),.8f);
    }
    static void Guards(GameObject prototype)
    {
        var positions=new[]{new Vector2(-24,12),new Vector2(-20,18),new Vector2(19,14),new Vector2(25,8)};
        var targets=new[]{new Vector2(-18,12),new Vector2(-15,18),new Vector2(23,14),new Vector2(20,8)};
        var guards=new List<RPG2Guard>();
        for(int i=0;i<4;i++)
        {
            var go=UnityEngine.Object.Instantiate(prototype,buildRoot);go.name="RPG2 Patrol guard "+(i+1);go.transform.position=positions[i];go.transform.localScale=Vector3.one;
            go.layer=6;
            foreach(var trigger in go.GetComponents<Collider2D>().Where(c=>c.isTrigger))UnityEngine.Object.DestroyImmediate(trigger);
            var body=go.GetComponent<Rigidbody2D>();body.constraints=RigidbodyConstraints2D.FreezeRotation;body.bodyType=RigidbodyType2D.Dynamic;body.simulated=true;
            var health=go.GetComponent<Enemy_Health>();health.maxHealth=4;health.currenetHealth=4;health.expReward=2;health.invulnerable=false;health.preserveForRetry=true;
            go.GetComponent<Enemy_Movement>().enabled=false;go.GetComponent<Enemy_Combat>().enabled=false;
            var guard=go.AddComponent<RPG2Guard>();guard.world=adventure;guard.home=positions[i];guard.end=targets[i];guard.territory=i<2?new Rect(-29,9,17,11):new Rect(16,6,13,13);guards.Add(guard);used.Add(positions[i]);used.Add(targets[i]);
            go.SetActive(true);
        }
        adventure.guards=guards.ToArray();
    }
    static void DecorateRPG2()
    {
        var random=new System.Random(211126);
        for(int i=0;i<470;i++)
        {
            var p=new Vector2(-33+(float)random.NextDouble()*62,-18+(float)random.NextDouble()*37);
            if(riverShape.OverlapPoint(p)||DistanceRoad(p)<2.1f||used.Any(o=>Vector2.Distance(o,p)<2.4f)||new Rect(-10,-2,18,8).Contains(p)||new Rect(15,6,15,14).Contains(p)||new Rect(-29,9,17,11).Contains(p))continue;
            if(i%3==0)Tree(p,.85f+(float)random.NextDouble()*.25f);
            else if(i%7==0)Place("Forest floor rock",ExpansionArt(i%2==0?"GroundRock05":"GroundRock06"),p,.8f);
            else Place("Meadow grasses and flowers",Art("Assets/Sprites/Deco/"+(1+i%12).ToString("00")+".png"),p,.7f+(float)random.NextDouble()*.3f);
        }
        for(int x=-33;x<=29;x+=2){if(!riverShape.OverlapPoint(new Vector2(x,19)))Tree(new Vector2(x,19),1.1f);if(!riverShape.OverlapPoint(new Vector2(x,-18)))Tree(new Vector2(x,-18),1.05f);}
        for(int y=-16;y<19;y+=2){if(!riverShape.OverlapPoint(new Vector2(-33,y)))Tree(new Vector2(-33,y));}
        // Reed clusters follow the existing shoreline; water geometry itself is unchanged.
        for(int x=-30;x<29;x+=2)for(int y=-15;y<5;y++)
        {
            Vector2 p=new Vector2(x+.3f,y+.4f);
            if(riverShape.OverlapPoint(p)||!riverShape.OverlapPoint(p+Vector2.down*.8f)||used.Any(o=>Vector2.Distance(o,p)<2)||Mathf.Abs(p.x)<2)continue;
            Place("Riverbank reed cluster",Art("Assets/Sprites/Deco/08.png"),p,.8f);
        }
    }
    [MenuItem("Tools/RPG/RPG2 Workshop/Refresh Detailed Atlas")]
    public static void BakeAtlas()
    {
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();if(w==null||EditorApplication.isPlaying)throw new Exception("Open expanded RPG2 in edit mode");
        var cameraObject=new GameObject("Temporary atlas baker");var cam=cameraObject.AddComponent<Camera>();cam.orthographic=true;cam.orthographicSize=w.bounds.height*.5f;
        cam.transform.position=new Vector3(w.bounds.center.x,w.bounds.center.y,-20);cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.24f,.36f,.27f);
        const int width=1280,height=780;var target=new RenderTexture(width,height,24);cam.targetTexture=target;
        cam.Render();var prior=RenderTexture.active;RenderTexture.active=target;var png=new Texture2D(width,height,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,width,height),0,0);png.Apply();
        const string path="Assets/RPG2Expansion/ReedbankAtlas.png";File.WriteAllBytes(path,png.EncodeToPNG());RenderTexture.active=prior;cam.targetTexture=null;
        UnityEngine.Object.DestroyImmediate(png);UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);
        AssetDatabase.ImportAsset(path);var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.textureType=TextureImporterType.Sprite;importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        w.chart=Art(path);EditorUtility.SetDirty(w);EditorSceneManager.MarkSceneDirty(w.gameObject.scene);
    }
}
