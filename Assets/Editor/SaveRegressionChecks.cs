using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

[InitializeOnLoad]
public static class SaveRegressionChecks
{
    static int stage=-1;static double next;static string account,originalAccount,originalToken;static int expectedGold;static readonly List<string> report=new List<string>();
    
    static SaveRegressionChecks(){EditorApplication.update+=Tick;}
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);report.Add("PASS "+message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Temp/SaveRegression.request"))
        {
            try{File.Delete("Temp/SaveRegression.request");}catch(IOException){return;}
            if(!EditorApplication.isPlaying)return;
            report.Clear();originalAccount=LoginSession.Account;originalToken=LoginSession.Token;account="save-test-"+Guid.NewGuid().ToString("N");LoginSession.Set(account,"test");stage=0;SceneManager.LoadScene("RPG2");next=EditorApplication.timeSinceStartup+2;
        }
        if(stage<0||EditorApplication.timeSinceStartup<next||(stage!=6&&!SaveSystem.IsReady))return;
        try
        {
            var world=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();
            if(stage==0)
            {
                Check(world!=null&&world.Player!=null,"Scene initialization before restore");
                var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");Check(catalog!=null&&catalog.items.Length>0,"Build item GUID catalog");
                foreach(var slot in InventortyManager.Instance.itemSlots){slot.itemSO=null;slot.quantity=0;slot.UpdateUI();}
                var inventory=InventortyManager.Instance;var timber=Resources.Load<ItemSO>("Items/timber");inventory.AddItem(timber,3);
                world.Interact(world.points.First(p=>p.kind==RPG2Point.Kind.Bridge));Check(world.BridgeRepaired,"Real bridge interaction completes");expectedGold=inventory.gold;
                var stats=StatsManager.Instance;var effects=inventory.useItem;int speed=stats.speed;
                var buff=ScriptableObject.CreateInstance<ItemSO>();buff.speed=2;buff.duration=30;
                effects.TryApplyItemEffects(buff);effects.TryApplyItemEffects(buff);effects.TryApplyItemEffects(buff);Check(Useltem.SpeedStacks==3&&stats.speed==speed+6,"Three real speed buffs apply");
                SaveSystem.SaveNow();var saved=JsonUtility.FromJson<SaveSystem.Data>(File.ReadAllText(SaveSystem.FilePath(account)));Check(saved.speed==speed,"Snapshot excludes temporary buff bonuses");
                effects.ClearTemporaryEffects();Check(Useltem.SpeedStacks==0&&stats.speed==speed,"Interrupted buff cleanup restores base speed and slots");
                effects.TryApplyItemEffects(buff);Check(Useltem.SpeedStacks==1,"New buff works after cleanup");effects.ClearTemporaryEffects();UnityEngine.Object.Destroy(buff);
                bool animator=world.Player.GetComponent<Animator>().enabled;
                FishingRig2D.Begin(world.Player.transform,world.Player.transform.position+Vector3.up);var rig=UnityEngine.Object.FindObjectOfType<FishingRig2D>();UnityEngine.Object.DestroyImmediate(rig.gameObject);
                Check(world.Player.GetComponent<Animator>().enabled==animator&&!FishingRig2D.Busy,"Destroyed fishing rig restores animator");
                world.Player.transform.position=new Vector3(-3,-8,0);world.Player.rb.position=world.Player.transform.position;
                var harvest=world.points.First(p=>p.kind==RPG2Point.Kind.Mushroom);world.Interact(harvest);
                world.Accept(0);inventory.AddItem(world.mushroom,3);inventory.AddItem(world.pumpkin,2);world.Interact(world.points.First(p=>p.kind==RPG2Point.Kind.Camp));
                var lights=world.points.Where(p=>p.kind==RPG2Point.Kind.Beacon).OrderBy(p=>p.number).ToArray();foreach(int index in new[]{0,2,1})world.Interact(lights[index]);
                foreach(var guard in world.guards)guard.GetComponent<Enemy_Health>().ChangeHealth(-10000);
                world.Interact(world.points.First(p=>p.kind==RPG2Point.Kind.Treasure));
                Check(world.SuppliesDelivered&&world.BeaconCount==3&&world.TreasureClaimed,"All main task states complete before save");expectedGold=inventory.gold;
                SaveSystem.SaveNow();Check(File.Exists(SaveSystem.FilePath(account)),"Account JSON snapshot written");
                SaveSystem.ReloadFromDisk();stage=1;next=EditorApplication.timeSinceStartup+2;
            }
            else if(stage==1)
            {
                Check(world.guards[0].Health==0&&!world.guards[0].gameObject.activeSelf,"Defeated guard remains defeated after disk reload");
                Check(world.SuppliesDelivered&&world.BeaconCount==3&&world.TreasureClaimed&&world.Accepted(0),"Delivery, accepted quests, beacon puzzle and treasure restored");
                Check(world.points.Where(p=>p.kind==RPG2Point.Kind.Beacon).All(p=>p.glow.activeSelf),"All three beacon flames restored");
                Check(!world.points.First(p=>p.kind==RPG2Point.Kind.Mushroom).GetComponent<SpriteRenderer>().enabled,"Collected point remains depleted");
                world.Interact(world.points.First(p=>p.kind==RPG2Point.Kind.Treasure));Check(InventortyManager.Instance.gold==expectedGold,"Restored treasure cannot reward twice");
                Check(world.BridgeRepaired&&world.bridge.activeSelf&&world.Timber==0,"Disk reload restores repaired bridge and consumed timber");
                Check(InventortyManager.Instance.gold==expectedGold,"Disk reload restores exact reward balance");
                world.Interact(world.points.First(p=>p.kind==RPG2Point.Kind.Bridge));Check(InventortyManager.Instance.gold==expectedGold,"Restored bridge cannot reward twice");
                Check(world.QuestProgress(1).Contains("已交付")&&!world.QuestProgress(1).Contains("0 / 3"),"Completed bridge text coherent");
                Check(Vector2.Distance(world.Player.transform.position,new Vector2(-3,-8))<.2f,"Player position restored after Start");
                Check(UnityEngine.Object.FindObjectOfType<ChapterRoute>()!=null,"RPG2 route exists");
                SaveSystem.SaveNow();SceneManager.LoadScene("RPG1");stage=2;next=EditorApplication.timeSinceStartup+2;
            }
            else if(stage==2){SaveSystem.SaveNow();SceneManager.LoadScene("RPG2");stage=3;next=EditorApplication.timeSinceStartup+2;}
            else if(stage==3)
            {
                Check(world.BridgeRepaired&&InventortyManager.Instance.gold==expectedGold,"RPG2 to RPG1 and back retains progress without duplicate rewards");
                SaveSystem.SaveNow();SceneManager.LoadScene("Willowreach");stage=4;next=EditorApplication.timeSinceStartup+2;
            }
            else if(stage==4)
            {
                Check(UnityEngine.Object.FindObjectOfType<ChapterRoute>()!=null,"Willowreach return route exists");
                SaveSystem.SaveNow();Check(SaveSystem.ResumeScene("RPG1")=="Willowreach","Login resumes last saved scene");
                string path=SaveSystem.FilePath(account);Check(path!=SaveSystem.FilePath(account+"other"),"Different accounts have different files");
                SaveSystem.SaveNow();File.WriteAllText(path,"broken-json");SaveSystem.ReloadFromDisk();stage=5;next=EditorApplication.timeSinceStartup+2;
            }
            else if(stage==5)
            {
                Check(SceneManager.GetActiveScene().name=="Willowreach"&&InventortyManager.Instance.gold==expectedGold,"Corrupt primary save falls back to backup");
                GlobalSettings.Instance.SendMessage("Logout");stage=6;next=EditorApplication.timeSinceStartup+2;
            }
            else if(stage==6)
            {
                Check(SceneManager.GetActiveScene().name=="Login"&&SaveSystem.Instance!=null,"Real logout preserves save service and returns to Login");
                LoginSession.Set(account,"test");SceneManager.LoadScene(SaveSystem.ResumeScene("RPG1"));stage=7;next=EditorApplication.timeSinceStartup+2;
            }
            else if(stage==7)
            {
                Check(SceneManager.GetActiveScene().name=="Willowreach"&&InventortyManager.Instance!=null&&InventortyManager.Instance.gold==expectedGold,"After logout new player and inventory resume from saved account");
                File.WriteAllText("Logs/SaveRegressionChecks.txt",string.Join("\n",report));Finish();
            }
        }
        catch(Exception e){report.Add("FAIL "+e);File.WriteAllLines("Logs/SaveRegressionChecks.txt",report);Debug.LogException(e);Finish();}
    }
    static void Finish()
    {
        stage=-1; // Stop Play so the temporary test world cannot overwrite another account's data.
        EditorApplication.isPlaying=false;
        string cleanup=account;
        EditorApplication.delayCall+=()=>{
            foreach(string suffix in new[]{"",".bak",".tmp"}){string p=SaveSystem.FilePath(cleanup)+suffix;if(File.Exists(p))File.Delete(p);}
        };
        foreach(string key in new[]{"Rpg.Quests","Rpg.Task.RPG2.quest.0","Rpg.Task.RPG2.quest.1","Rpg.Task.RPG2.quest.2","Rpg.Task.RPG2.quest.3"})PlayerPrefs.DeleteKey(PlayerStats.AccountKey(key));PlayerPrefs.Save();
        LoginSession.Set(originalAccount,originalToken);
    }
}
