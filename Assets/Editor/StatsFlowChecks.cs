using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

[InitializeOnLoad]
public static class StatsFlowChecks
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static double deadline;
    static bool running;
    static readonly List<string> report=new List<string>();
    static StatsFlowChecks(){EditorApplication.update+=Tick;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);report.Add("PASS "+message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Temp/StatsFlow.request"))
        {
            string command;try{command=File.ReadAllText("Temp/StatsFlow.request").Trim();File.Delete("Temp/StatsFlow.request");}catch(IOException){return;}
            if(command=="run")
            {
                if(!EditorApplication.isPlaying){File.WriteAllText("Logs/StatsFlowChecks.txt","WAIT: enter Play Mode first");return;}
                if(GlobalSettings.Instance.IsOpen)GlobalSettings.Instance.SetOpen(false);
                SceneManager.LoadScene("RPG2");running=true;deadline=EditorApplication.timeSinceStartup+2;
            }
        }
        if(!running||EditorApplication.timeSinceStartup<deadline)return;
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();if(w==null||w.Player==null){deadline=EditorApplication.timeSinceStartup+.5;return;}
        running=false;Run(w);
    }
    static object Call(RPG2Adventure w,string method,params object[] args)=>typeof(RPG2Adventure).GetMethod(method,Flags).Invoke(w,args);
    static void Field(RPG2Adventure w,string name,object value)=>typeof(RPG2Adventure).GetField(name,Flags).SetValue(w,value);
    static void Run(RPG2Adventure w)
    {
        report.Clear();string account=LoginSession.Account,token=LoginSession.Token;
        var inv=InventortyManager.Instance;var stats=StatsManager.Instance;
        var items=inv.itemSlots.Select(s=>s.itemSO).ToArray();var counts=inv.itemSlots.Select(s=>s.quantity).ToArray();int gold=inv.gold,hp=stats.currentHealth;
        int xp=0;Action<int> track=value=>xp+=value;InventortyManager.OnExperienceGained+=track;
        var testAccount="stats-verification-"+Guid.NewGuid().ToString("N");var keys=new List<string>();
        try
        {
            LoginSession.Set(testAccount,"test");
            foreach(string key in new[]{"Rpg.FishCaught","Rpg.Cooked","Rpg.Quests"})keys.Add(PlayerStats.AccountKey(key));
            foreach(string id in new[]{"RPG2.quest.0","RPG2.quest.1","RPG2.quest.2","RPG2.quest.3","RPG2.sheep"})keys.Add(PlayerStats.AccountKey("Rpg.Task."+id));
            Check(PlayerStats.Quests==0&&PlayerStats.Cooked==0&&PlayerStats.FishCaught==0,"New account has separate progression");
            foreach(var slot in inv.itemSlots){slot.itemSO=null;slot.quantity=0;slot.UpdateUI();}
            w.Accept(0);w.Interact(w.points.First(p=>p.kind==RPG2Point.Kind.Camp));Check(!w.SuppliesDelivered&&PlayerStats.Quests==0,"Missing materials cannot complete or gain reputation");
            inv.AddItem(w.mushroom,3);inv.AddItem(w.pumpkin,2);w.Interact(w.points.First(p=>p.kind==RPG2Point.Kind.Camp));
            Check(w.SuppliesDelivered&&PlayerStats.Quests==1&&inv.gold==gold+18&&xp==4,"Soup delivery pays 18 gold, 4 XP, one distinct task");
            Check(inv.GetItemQuantity(w.mushroom)==0&&inv.GetItemQuantity(w.pumpkin)==0&&inv.GetItemQuantity(w.meat)==1,"Soup delivery consumes exact ingredients and pays food");
            foreach(var p in w.points.Where(p=>p.kind==RPG2Point.Kind.Timber))w.Interact(p);
            Check(w.Timber==3,"Gathering provides real timber inventory");
            var snapshot=inv.itemSlots.Select(s=>s.itemSO).ToArray();var qty=inv.itemSlots.Select(s=>s.quantity).ToArray();
            for(int i=0;i<inv.itemSlots.Length;i++){if(inv.itemSlots[i].itemSO==null){inv.itemSlots[i].itemSO=w.mushroom;inv.itemSlots[i].quantity=w.mushroom.stackSize;}else if(inv.itemSlots[i].itemSO!=Resources.Load<ItemSO>("Items/timber"))inv.itemSlots[i].quantity=inv.itemSlots[i].itemSO.stackSize;inv.itemSlots[i].UpdateUI();}
            w.Interact(w.points.First(p=>p.kind==RPG2Point.Kind.Bridge));Check(!w.BridgeRepaired&&w.Timber==3&&PlayerStats.Quests==1,"Full rewards retain all bridge materials and no reputation");
            for(int i=0;i<inv.itemSlots.Length;i++){inv.itemSlots[i].itemSO=snapshot[i];inv.itemSlots[i].quantity=qty[i];inv.itemSlots[i].UpdateUI();}
            w.Interact(w.points.First(p=>p.kind==RPG2Point.Kind.Bridge));Check(w.BridgeRepaired&&w.Timber==0&&PlayerStats.Quests==2&&inv.gold==gold+30,"Bridge retry consumes 3 timber, pays 12 gold and 2 pumpkin");
            var beacons=w.points.Where(p=>p.kind==RPG2Point.Kind.Beacon).OrderBy(p=>p.number).ToArray();
            w.Interact(beacons[1]);Check(w.BeaconCount==0&&PlayerStats.Quests==2,"Wrong beacon order pays nothing");
            foreach(int index in new[]{0,2,1})w.Interact(beacons[index]);Check(w.BeaconCount==3&&PlayerStats.Quests==3&&xp==8&&inv.gold==gold+46,"Beacon sequence pays 16 gold,4 XP and 2 mushrooms");
            foreach(var guard in w.guards)guard.GetComponent<Enemy_Health>().currenetHealth=0;
            var treasure=w.points.First(p=>p.kind==RPG2Point.Kind.Treasure);w.Interact(treasure);
            Check(w.TreasureClaimed&&inv.gold==gold+78&&xp==16&&PlayerStats.Quests==4,"Four guards plus beacons unlock treasure; all four tasks total 78 gold /16 XP");
            w.Interact(treasure);w.Interact(beacons[0]);w.Interact(w.points.First(p=>p.kind==RPG2Point.Kind.Bridge));
            Check(inv.gold==gold+78&&PlayerStats.Quests==4&&!PlayerStats.AddQuest("RPG2.quest.3"),"Repeated claims do not pay or add reputation twice");
            Check(PlayerStats.ReputationLevel==3&&PlayerStats.CarryMax==26,"Four distinct tasks unlock reputation 3 and six extra carrying capacity");
            var fishing=w.points.First(p=>p.kind==RPG2Point.Kind.Fishing);
            Field(w,"fishing",fishing);Field(w,"fishReady",Time.time+2);Field(w,"fishEnd",Time.time+4);Call(w,"ResolveFishing");Check(PlayerStats.FishCaught==0,"Early reel grants no fishing progress");
            Field(w,"fishing",fishing);Field(w,"fishReady",Time.time-1);Field(w,"fishEnd",Time.time+2);Field(w,"deepCast",false);Call(w,"ResolveFishing");Check(PlayerStats.FishCaught==1,"Successful landed fish grants exactly one progress");
            for(int i=1;i<10;i++)PlayerStats.AddFish();Check(PlayerStats.FishingLevel==1&&Mathf.Approximately(PlayerStats.FishingWindowBonus,.1f),"Fishing 10 grants level 1 and .1s extra reaction window");
            for(int i=0;i<5;i++){inv.AddItem(w.mushroom,2);Check((bool)Call(w,"TryCook",false),"Cook succeeds with actual ingredients "+(i+1));}
            Check(PlayerStats.Cooked==5&&PlayerStats.CookingLevel==1,"Five successful cooks unlock cooking level 1");
            var soup=Resources.Load<ItemSO>("Items/mushroom_soup");stats.currentHealth=1;var effects=inv.useItem!=null?inv.useItem:inv.gameObject.AddComponent<Useltem>();
            Check(effects.TryApplyItemEffects(soup)&&stats.currentHealth==Mathf.Min(stats.maxHealth,1+soup.currentHealth+1),"Cooking level adds one actual soup healing point");
            Check(PlayerStats.Value("攻击")==NightCombat2D.PlayerDamage(stats.damage).ToString()&&PlayerStats.Value("生命")==stats.currentHealth+"/"+stats.maxHealth,"Panel values match gameplay stats");
            LoginSession.Set(testAccount+"-other","test");Check(PlayerStats.Quests==0&&PlayerStats.FishCaught==0&&PlayerStats.Cooked==0,"Switching accounts cannot inherit progress");
            LoginSession.Set(testAccount,"test");Check(PlayerStats.Quests==4&&PlayerStats.Cooked==5,"Returning account retains its own progress");
        }
        catch(Exception e){report.Add("FAIL "+e);Debug.LogException(e);}
        finally
        {
            InventortyManager.OnExperienceGained-=track;
            foreach(var key in keys)PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();LoginSession.Set(account,token);
            for(int i=0;i<items.Length;i++){inv.itemSlots[i].itemSO=items[i];inv.itemSlots[i].quantity=counts[i];inv.itemSlots[i].UpdateUI();}inv.gold=gold;if(inv.goldText!=null)inv.goldText.text=gold.ToString();stats.currentHealth=hp;
            File.WriteAllLines("Logs/StatsFlowChecks.txt",report);
        }
    }
}
