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
    [MenuItem("Tools/RPG/Scene Expansion/Repair Towers Rain And Bones")]
    public static void RepairTowersRainAndBones()
    {
        var scene=SceneManager.GetActiveScene();
        if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=Expanded)return;
        var backup=File.ReadAllText("Backups/LatestRainTowerBones.txt").Trim();
        if(!File.Exists(backup+"/Willowreach-live.unity"))EditorSceneManager.SaveScene(scene,backup+"/Willowreach-live.unity",true);
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();
        foreach(var sr in world.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Village watchtower"||s.name=="Mine watchtower"||s.name=="Ruin tower"))
        {
            Undo.RecordObject(sr.GetComponent<BoxCollider2D>(),"Align tower foundation");
            var alpha=AlphaBounds(sr.sprite);float ppu=sr.sprite.pixelsPerUnit;
            float height=alpha.height*.32f/ppu;
            var body=sr.GetComponent<BoxCollider2D>();body.enabled=true;body.isTrigger=false;
            body.size=new Vector2(alpha.width*.88f/ppu,height);
            body.offset=new Vector2((alpha.center.x-sr.sprite.pivot.x)/ppu,(alpha.yMin-sr.sprite.pivot.y+2)/ppu+height*.5f);
            sr.GetComponent<ObliqueDepth2D>().Refresh();
            var door=sr.transform.Find("Door interaction");
            if(door!=null)door.position=new Vector3(body.bounds.center.x,body.bounds.min.y-.65f,0);
        }
        var bone=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/bone.asset");
        var femur=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Femur.asset");
        bone.itemName="骨头";bone.itemDescription="制作材料：在柳湾村口营火旁，用3根骨头制作骨笛，之后按R呼唤或放牧羊群。";
        femur.inventoryMaterial=bone;femur.icon=bone.icon;femur.itemName="骨头";femur.itemDescription=bone.itemDescription;
        EditorUtility.SetDirty(bone);EditorUtility.SetDirty(femur);
        // Shared loot remains compatible; existing Femur rewards now join the same material stack.
        foreach(var loot in UnityEngine.Object.FindObjectsOfType<Loot>())if(loot.itemSO==femur){loot.itemSO=bone;if(loot.sr!=null)loot.sr.sprite=bone.icon;}
        EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene);
        Debug.Log("Tower foundations aligned; legacy bones unified; rain contacts enabled automatically in Play.");
    }
    [MenuItem("Tools/RPG/Scene Expansion/Check Towers Rain And Bones In Play")]
    public static void CheckRainTowerBones()
    {
        if(!EditorApplication.isPlaying)return;
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();var rain=world.weather.GetComponent<RainFootsteps2D>();
        var inv=InventortyManager.Instance;var pastoral=world.GetComponent<WillowreachPastoral>();
        var items=inv.itemSlots.Select(s=>s.itemSO).ToArray();var counts=inv.itemSlots.Select(s=>s.quantity).ToArray();bool flute=pastoral.fluteCrafted;
        var previousWeather=world.weather.currentWeather;bool random=world.weather.randomWeather;var lines=new List<string>();Action<bool,string> check=(ok,s)=>lines.Add((ok?"PASS ":"FAIL ")+s);
        try
        {
            Physics2D.SyncTransforms();
            foreach(var sr in world.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Village watchtower"||s.name=="Mine watchtower"||s.name=="Ruin tower"))
            {
                var alpha=AlphaBounds(sr.sprite);var p=new Vector2((alpha.center.x-sr.sprite.pivot.x)/sr.sprite.pixelsPerUnit,(alpha.yMin-sr.sprite.pivot.y+5)/sr.sprite.pixelsPerUnit);
                var col=sr.GetComponent<BoxCollider2D>();
                check(col.enabled&&!col.isTrigger&&col.OverlapPoint(sr.transform.TransformPoint(p)),"Visible tower entrance is solid: "+sr.name);
                check(!world.GetComponent<WillowreachNavigation>().Safe(col.bounds.center),"Sheep navigation excludes tower base: "+sr.name);
            }
            foreach(var slot in inv.itemSlots){slot.itemSO=null;slot.quantity=0;slot.UpdateUI();}
            var femur=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Femur.asset");
            inv.AddItem(pastoral.bone,6);inv.AddItem(femur,2);
            check(inv.GetItemQuantity(pastoral.bone)==8&&inv.itemSlots.Count(s=>s.itemSO==pastoral.bone)==1,"Legacy femur joins bone stack with exact count");
            check(femur.icon==pastoral.bone.icon,"Both variants use identical pixel-art icon");
            inv.itemSlots[1].itemSO=femur;inv.itemSlots[1].quantity=2;inv.NormalizeMaterials();
            check(inv.GetItemQuantity(pastoral.bone)==10&&inv.itemSlots.Count(s=>s.itemSO==pastoral.bone)==1,"Existing legacy inventory merges without losing quantity");
            inv.itemSlots[0].quantity=8;inv.itemSlots[0].UpdateUI();
            pastoral.fluteCrafted=false;check(pastoral.Craft()&&inv.GetItemQuantity(pastoral.bone)==5,"Unified bones craft functional sheep flute, costing exactly three");
            check(!pastoral.Craft()&&inv.GetItemQuantity(pastoral.bone)==5,"Crafted flute cannot consume materials twice");
            world.weather.randomWeather=false;world.weather.SetWeather(WeatherCycle2D.Weather.Sunny);world.weather.Advance(10);
            check(!rain.TryStep(new Vector2(-18,-12)),"Sunny ground does not create water rings");
            int before=rain.ThunderCount;rain.AdvanceThunder(100);check(before==rain.ThunderCount,"No thunder in sunshine");
            world.weather.SetWeather(WeatherCycle2D.Weather.Rain);world.weather.Advance(10);
            check(rain.TryStep(new Vector2(-18,-12))&&rain.ActiveCount>0,"Rain foot contact creates visible ground ripple");
            check(!rain.TryStep(new Vector2(-8,6)),"Wooden bridge does not emit puddle rings");
            check(!rain.TryStep(new Vector2(0,6)),"River uses water response instead of ground rings");
            rain.AdvanceThunder(0);rain.AdvanceThunder(10);check(before==rain.ThunderCount,"Rain begins quietly without immediate thunder");
            rain.AdvanceThunder(30);check(rain.ThunderCount==before+1,"Occasional thunder starts after randomized delay");
            rain.AdvanceThunder(.1f);check(rain.ThunderCount==before+1,"Thunder cannot fire every frame");
        }
        finally
        {
            for(int i=0;i<items.Length;i++){inv.itemSlots[i].itemSO=items[i];inv.itemSlots[i].quantity=counts[i];inv.itemSlots[i].UpdateUI();}
            pastoral.fluteCrafted=flute;world.weather.randomWeather=random;world.weather.SetWeather(previousWeather);
            File.WriteAllLines("Logs/WillowreachRainTowerBones.txt",lines);Debug.Log(string.Join("\n",lines));
        }
    }
    [MenuItem("Tools/RPG/Scene Expansion/Preview Rain Steps In Play")]
    public static void PreviewRainSteps()
    {
        if(!EditorApplication.isPlaying)return;var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();
        world.player.position=new Vector3(-18,-12,0);world.weather.randomWeather=false;world.weather.SetWeather(WeatherCycle2D.Weather.Rain);world.weather.Advance(10);
    }
}
