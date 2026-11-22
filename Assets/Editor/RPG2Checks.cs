using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class RPG2Workshop
{
    static readonly List<string> checks=new List<string>();
    static void Require(bool pass,string label){checks.Add((pass?"PASS ":"FAIL ")+label);if(!pass)throw new Exception(label);}
    [MenuItem("Tools/RPG/RPG2 Workshop/Check Expanded Scene")]
    public static void CheckScene()
    {
        checks.Clear();
        try
        {
            var scene=SceneManager.GetActiveScene();var world=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();
            Require(scene.path==ScenePath&&world!=null,"RPG2 is the expanded active scene");
            Require(scene.GetRootGameObjects().Any(g=>g.name=="Grid"),"Original Grid retained");
            Require(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Any(s=>s.name=="House_Yellow"),"Original yellow house retained");
            Require(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<SpriteRenderer>(true)).Any(s=>s.name=="House_Purple"),"Original purple house retained");
            Require(UnityEngine.Object.FindObjectOfType<WaterSurface2D>()!=null,"Original animated river retained");
            Require(UnityEngine.Object.FindObjectOfType<SceneChanger>()!=null,"Original RPG1 exit retained");
            Require(world.points.Length>=25&&world.points.All(p=>p!=null),"Interactive node references present");
            Require(world.guards.Length==4&&world.guards.All(g=>g!=null&&g.isActiveAndEnabled&&g.home!=g.end),"All four guards active with distinct patrol endpoints");
            Require(world.guards.All(g=>g.GetComponent<Rigidbody2D>().constraints==RigidbodyConstraints2D.FreezeRotation&&g.GetComponent<Rigidbody2D>().simulated),"All four guard bodies can move on XY");
            Require(world.guards.All(g=>!g.GetComponent<Enemy_Movement>().enabled&&!g.GetComponent<Enemy_Combat>().enabled),"No competing legacy movement/combat on guards");
            Require(world.points.Count(p=>p.kind==RPG2Point.Kind.Timber)==3,"Exactly three discoverable bridge timber bundles");
            Require(world.points.Count(p=>p.kind==RPG2Point.Kind.Beacon)==3,"Three beacon puzzle nodes");
            Require(world.GetComponentsInChildren<WillowreachFlipbook>().Where(f=>f.name.Contains("pine")).All(f=>f.frames.Length==4&&f.framesPerSecond==5),"Original four-frame tree animation at 5 FPS");
            Require(world.chart!=null&&world.font!=null&&world.parchment!=null,"Atlas and Chinese UI assets connected");
            Require(scene.GetRootGameObjects().First(g=>g.name=="QuestCanvas").activeSelf==false,"Broken tutorial quest canvas archived inactive in RPG2 only");
            string backup=File.ReadAllText("Backups/LatestRPG2Expansion.txt").Trim();
            Require(File.ReadAllBytes("Assets/Scenes/RPG1.unity").SequenceEqual(File.ReadAllBytes(backup+"/RPG1-unchanged.unity")),"RPG1 scene unchanged byte for byte");
            Require(scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).All(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)==0),"No missing scripts");
            Physics2D.SyncTransforms();var water=UnityEngine.Object.FindObjectOfType<WaterSurface2D>().GetComponent<PolygonCollider2D>();
            Require(world.points.Where(p=>p.kind!=RPG2Point.Kind.Fishing).All(p=>!water.OverlapPoint(p.transform.position)),"Every interaction point is placed on land");
            foreach(var guard in world.guards)
            {
                Require(!water.OverlapPoint(guard.home)&&!water.OverlapPoint(guard.end),guard.name+" patrol endpoints on land");
                Require(!Physics2D.LinecastAll(guard.home,guard.end,1<<7).Any(h=>h.collider!=null&&!h.collider.isTrigger),guard.name+" patrol lane unobstructed");
            }
        }
        finally{File.WriteAllLines("Logs/RPG2SceneChecks.txt",checks);}
    }

    static double patrolUntil;
    static RPG2Adventure checkedWorld;
    static Vector2[] startPositions;
    static float[] movement;
    [MenuItem("Tools/RPG/RPG2 Workshop/Check Gameplay In Play Mode")]
    public static void BeginPlayChecks()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play first");
        checkedWorld=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();
        if(checkedWorld==null||checkedWorld.Player==null)throw new Exception("RPG2 not initialized");
        checks.Clear();checkedWorld.CloseMenu();startPositions=checkedWorld.guards.Select(g=>(Vector2)g.transform.position).ToArray();movement=new float[4];
        patrolUntil=EditorApplication.timeSinceStartup+3;EditorApplication.update-=CheckPlayTick;EditorApplication.update+=CheckPlayTick;
    }
    static void CheckPlayTick()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=CheckPlayTick;return;}
        for(int i=0;i<4;i++)movement[i]=Mathf.Max(movement[i],Vector2.Distance(startPositions[i],checkedWorld.guards[i].transform.position));
        if(EditorApplication.timeSinceStartup<patrolUntil)return;
        EditorApplication.update-=CheckPlayTick;
        try
        {
            var w=checkedWorld;
            Require(w.Player!=null&&w.Player.gameObject.activeInHierarchy,"Player initialized in RPG2");
            for(int i=0;i<4;i++)Require(movement[i]>.65f,"Guard "+(i+1)+" patrol moves more than 0.65 units: "+movement[i].ToString("F2"));
            float originalScale=Time.timeScale;w.OpenMenu(false);Require(w.MenuOpen&&Time.timeScale==0,"Quest journal opens and pauses world");w.CloseMenu();Require(Time.timeScale==originalScale,"Closing journal restores prior pause state");
            w.OpenMenu(true);Require(w.MenuOpen&&Time.timeScale==0,"Detailed atlas opens and pauses world");w.CloseMenu();
            var inv=InventortyManager.Instance;int coins=inv.gold;
            var bridge=w.points.First(p=>p.kind==RPG2Point.Kind.Bridge);w.Interact(bridge);Require(!w.BridgeRepaired,"Cannot repair bridge without timber");
            var treasure=w.points.First(p=>p.kind==RPG2Point.Kind.Treasure);w.Interact(treasure);Require(!w.TreasureClaimed,"Treasure is gated by signals and all four guards");
            foreach(var timber in w.points.Where(p=>p.kind==RPG2Point.Kind.Timber)){w.Interact(timber);w.Interact(timber);}
            Require(w.Timber==3,"Timber cannot be collected twice");
            var reservedItems=inv.itemSlots.Select(s=>s.itemSO).ToArray();var reservedCounts=inv.itemSlots.Select(s=>s.quantity).ToArray();
            try
            {
                foreach(var slot in inv.itemSlots){slot.itemSO=w.mushroom;slot.quantity=w.mushroom.stackSize;slot.UpdateUI();}
                w.Interact(bridge);Require(!w.BridgeRepaired&&inv.gold==coins,"Full reward bag does not complete bridge or partially pay gold");
            }
            finally{for(int i=0;i<inv.itemSlots.Length;i++){inv.itemSlots[i].itemSO=reservedItems[i];inv.itemSlots[i].quantity=reservedCounts[i];inv.itemSlots[i].UpdateUI();}}
            int pumpkinBefore=inv.GetItemQuantity(w.pumpkin);w.Interact(bridge);Require(w.BridgeRepaired&&w.bridge.activeSelf,"Three timber repair and reveal the crossing");
            Require(inv.GetItemQuantity(w.pumpkin)==pumpkinBefore+2,"Bridge awards the two pumpkins shown in reward UI");
            int afterBridge=inv.gold;w.Interact(bridge);Require(inv.gold==afterBridge,"Bridge reward not repeated");
            var beacons=w.points.Where(p=>p.kind==RPG2Point.Kind.Beacon).ToArray();
            w.Interact(beacons.First(p=>p.number==1));Require(w.BeaconCount==0,"Wrong beacon order resets puzzle");
            int mushroomsBefore=inv.GetItemQuantity(w.mushroom);
            foreach(int order in new[]{0,2,1})w.Interact(beacons.First(p=>p.number==order));Require(w.BeaconCount==3,"Water ruins forest sequence solves puzzle");
            Require(inv.GetItemQuantity(w.mushroom)==mushroomsBefore+2,"Beacon quest awards the two mushrooms shown in reward UI");
            int afterBeacon=inv.gold;w.Interact(beacons[0]);Require(inv.gold==afterBeacon,"Beacon reward cannot repeat");
            foreach(var guard in w.guards)guard.GetComponent<Enemy_Health>().ChangeHealth(-100);
            int treasureMeatBefore=inv.GetItemQuantity(w.meat);
            Require(w.Defeated==4,"All four guard defeats tracked");w.Interact(treasure);Require(w.TreasureClaimed,"Treasure unlocks after four guard defeats and signals");
            Require(inv.GetItemQuantity(w.meat)==treasureMeatBefore+2,"Treasure awards the two meats shown in reward UI");
            int afterTreasure=inv.gold;w.Interact(treasure);Require(inv.gold==afterTreasure,"Treasure cannot be paid twice");
            int soupMeatBefore=inv.GetItemQuantity(w.meat);
            inv.AddItem(w.mushroom,3);inv.AddItem(w.pumpkin,2);w.Accept(0);w.Interact(w.points.First(p=>p.kind==RPG2Point.Kind.Camp));Require(w.SuppliesDelivered,"Soup commission consumes real inventory materials and completes");
            Require(inv.GetItemQuantity(w.meat)==soupMeatBefore+1,"Soup awards the one meat shown in reward UI");
            Require(inv.gold>=coins+78,"Main route pays configured 78 gold total");
            var itemState=inv.itemSlots.Select(s=>s.itemSO).ToArray();var quantities=inv.itemSlots.Select(s=>s.quantity).ToArray();
            var harvest=w.points.First(p=>p.kind==RPG2Point.Kind.Pumpkin);
            try
            {
                foreach(var slot in inv.itemSlots){slot.itemSO=w.mushroom;slot.quantity=w.mushroom.stackSize;slot.UpdateUI();}
                var oldColor=harvest.GetComponent<SpriteRenderer>().color;w.Interact(harvest);
                Require(harvest.GetComponent<SpriteRenderer>().color==oldColor,"Full bag leaves harvest point unconsumed");
            }
            finally{for(int i=0;i<inv.itemSlots.Length;i++){inv.itemSlots[i].itemSO=itemState[i];inv.itemSlots[i].quantity=quantities[i];inv.itemSlots[i].UpdateUI();}}
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var fishing=w.points.First(p=>p.kind==RPG2Point.Kind.Fishing);int meatBefore=inv.GetItemQuantity(w.meat);
            w.Interact(fishing);typeof(RPG2Adventure).GetMethod("ResolveFishing",flags).Invoke(w,null);
            Require(inv.GetItemQuantity(w.meat)==meatBefore,"Early fishing pull awards no food");
            typeof(RPG2Adventure).GetField("fishCooldown",flags).SetValue(w,0f);w.Interact(fishing);
            typeof(RPG2Adventure).GetField("fishReady",flags).SetValue(w,Time.time-.1f);typeof(RPG2Adventure).GetField("fishEnd",flags).SetValue(w,Time.time+1f);
            typeof(RPG2Adventure).GetMethod("ResolveFishing",flags).Invoke(w,null);
            Require(inv.GetItemQuantity(w.meat)==meatBefore+1,"Fishing inside bite window awards exactly one food item");
            w.sheep.position=w.spawn+Vector2.right;w.Interact(w.points.First(p=>p.kind==RPG2Point.Kind.Sheep));
            typeof(RPG2Adventure).GetMethod("UpdateSheep",flags).Invoke(w,null);Require(w.SheepRescued,"Following sheep reaching camp completes rescue");
            int afterSheep=inv.gold;w.Interact(w.points.First(p=>p.kind==RPG2Point.Kind.Sheep));typeof(RPG2Adventure).GetMethod("UpdateSheep",flags).Invoke(w,null);Require(inv.gold==afterSheep,"Sheep rescue reward cannot repeat");
            w.CloseMenu();Debug.Log("RPG2 gameplay checks passed: "+checks.Count);
        }
        catch(Exception e){Debug.LogException(e);}
        finally{File.WriteAllLines("Logs/RPG2PlayChecks.txt",checks);}
    }
}
