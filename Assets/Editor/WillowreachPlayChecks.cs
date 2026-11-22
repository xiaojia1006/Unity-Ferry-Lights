using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WillowreachPlayChecks
{
    [MenuItem("Tools/RPG/Scene Expansion/Check Gameplay In Play Mode %#F4")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();var inv=InventortyManager.Instance;
        if(world==null||inv==null) return;
        var report=new List<string>();Action<bool,string> check=(ok,name)=>report.Add((ok?"PASS ":"FAIL ")+name);
        var items=inv.itemSlots.Select(s=>s.itemSO).ToArray();var quantities=inv.itemSlots.Select(s=>s.quantity).ToArray();
        int gold=inv.gold;var player=world.player;Vector3 position=player.position;
        var completed=world.nodes.Select(n=>n.completed).ToArray();var colors=world.nodes.Select(n=>n.indicator!=null?n.indicator.color:Color.white).ToArray();
        var visible=world.nodes.Select(n=>n.indicator!=null&&n.indicator.enabled).ToArray();
        var weather=world.weather.currentWeather;float rest=world.nextRest;var stats=StatsManager.Instance;int health=stats.currentHealth;
        var shortcut=world.nodes.First(n=>n.kind==WillowreachNode.Kind.Shortcut);bool active=shortcut.unlock.activeSelf;bool repaired=shortcut.activate!=null&&shortcut.activate.activeSelf;
        var food=world.nodes.First(n=>n.kind==WillowreachNode.Kind.Supply&&n.item.name=="pumpkin");
        try
        {
            foreach(var slot in inv.itemSlots){slot.itemSO=food.item;slot.quantity=food.item.stackSize;}
            food.completed=false;
            food.indicator.enabled=true;
            check(!food.TryInteract(world)&&!food.completed,"Full bag retains harvest and its unclaimed state");
            check(food.indicator.enabled,"Full bag keeps unclaimed harvest visible");
            foreach(var slot in inv.itemSlots){slot.itemSO=null;slot.quantity=0;}
            check(food.TryInteract(world)&&food.completed&&inv.GetItemQuantity(food.item)==food.quantity,"Harvest grants exact quantity");
            check(!food.indicator.enabled,"Successful harvest removes its visible sprite");
            var ore=world.nodes.First(n=>n.kind==WillowreachNode.Kind.Supply&&n.indicator!=null&&n.item.name=="Gold");
            ore.completed=false;ore.indicator.enabled=true;int beforeGold=inv.gold;
            check(ore.TryInteract(world)&&!ore.indicator.enabled&&inv.gold==beforeGold+ore.quantity,"Gold disappears only after exact one-time payment");
            check(!ore.TryInteract(world)&&inv.gold==beforeGold+ore.quantity,"Collected gold cannot be claimed again");
            int count=inv.GetItemQuantity(food.item);check(!food.TryInteract(world)&&inv.GetItemQuantity(food.item)==count,"Repeated harvest cannot duplicate items");
            var forest=world.nodes.First(n=>n.kind==WillowreachNode.Kind.Supply&&n.item.name=="Mushroom");forest.completed=false;
            world.weather.SetWeather(WeatherCycle2D.Weather.Rain);int old=inv.GetItemQuantity(forest.item);
            check(forest.TryInteract(world)&&inv.GetItemQuantity(forest.item)==old+forest.quantity+1,"Rain mushroom bonus conserves reward count");
            foreach(var n in world.nodes.Where(n=>n.kind==WillowreachNode.Kind.Survey))n.completed=false;
            shortcut.completed=false;check(!shortcut.TryInteract(world)&&shortcut.unlock.activeSelf,"Shortcut cannot unlock before all observations");
            foreach(var station in world.nodes.Where(n=>n.kind==WillowreachNode.Kind.Survey))
            {
                world.weather.SetWeather((WeatherCycle2D.Weather)(((int)station.requiredWeather+1)%3));
                check(!station.TryInteract(world)&&!station.completed,"Wrong weather rejects observation: "+station.title);
                world.weather.SetWeather(station.requiredWeather);
                check(station.TryInteract(world)&&station.completed,"Matching weather completes: "+station.title);
            }
            check(world.SurveyCount==3&&shortcut.TryInteract(world)&&!shortcut.unlock.activeSelf,"All observations unlock shortcut and reward");
            check(shortcut.activate!=null&&shortcut.activate.activeSelf,"Shortcut rebuilds its crossing, not just removing a barrier");
            int paid=inv.gold;check(!shortcut.TryInteract(world)&&inv.gold==paid,"Shortcut reward is one-time");
            var camp=world.nodes.First(n=>n.kind==WillowreachNode.Kind.Camp);world.nextRest=0;stats.currentHealth=Mathf.Max(1,stats.maxHealth-20);int before=stats.currentHealth;
            check(camp.TryInteract(world)&&stats.currentHealth==Mathf.Min(stats.maxHealth,before+10),"Camp heal is capped at ten HP");
            check(!camp.TryInteract(world),"Camp cooldown blocks repeated free healing");
            var inn=world.nodes.First(n=>n.kind==WillowreachNode.Kind.Camp&&n.transform.name=="Door interaction");
            check(!inn.TryInteract(world),"Inn shares camp cooldown; no alternating free healing");
            var reserve=world.nodes.First(n=>n.transform.parent.name=="Old mine entrance"&&n.transform.name=="Door interaction");
            reserve.completed=false;
            foreach(var station in world.nodes.Where(n=>n.kind==WillowreachNode.Kind.Survey))station.completed=false;
            int reserveGold=inv.gold;
            check(!reserve.TryInteract(world)&&inv.gold==reserveGold&&!reserve.completed,"Mine reserve locked before first observation");
            world.nodes.First(n=>n.kind==WillowreachNode.Kind.Survey).completed=true;
            check(reserve.TryInteract(world)&&inv.gold==reserveGold+12,"Mine building pays its bounded exploration reward");
            check(!reserve.TryInteract(world)&&inv.gold==reserveGold+12,"Mine building reward cannot be repeated");
            MenuPause.SetPaused(world,true);check(Time.timeScale==0,"Map pause holds the world");MenuPause.SetPaused(world,false);check(Time.timeScale>0,"Map pause releases correctly");
            var river=world.river.GetComponent<PolygonCollider2D>();
            foreach(var bridge in world.bridges)
            {
                bool intersects=false;for(float y=bridge.yMin;y<=bridge.yMax;y+=.25f)if(river.OverlapPoint(new Vector2(bridge.center.x,y)))intersects=true;
                check(intersects,"Bridge actually crosses water: "+bridge.center);
                var hits=Physics2D.OverlapBoxAll(bridge.center,new Vector2(.7f,bridge.height-.7f),0);
                check(!hits.Any(c=>!c.isTrigger&&c.attachedRigidbody==null),"Bridge center lane clear: "+bridge.center);
            }
        }
        catch(Exception ex){report.Add("FAIL Exception "+ex);}
        finally
        {
            for(int i=0;i<items.Length;i++){inv.itemSlots[i].itemSO=items[i];inv.itemSlots[i].quantity=quantities[i];inv.itemSlots[i].UpdateUI();}
            inv.gold=gold;if(inv.goldText!=null)inv.goldText.text=gold.ToString();
            for(int i=0;i<world.nodes.Length;i++){world.nodes[i].completed=completed[i];if(world.nodes[i].indicator!=null){world.nodes[i].indicator.color=colors[i];world.nodes[i].indicator.enabled=visible[i];}}
            shortcut.unlock.SetActive(active);if(shortcut.activate!=null)shortcut.activate.SetActive(repaired);world.weather.SetWeather(weather);world.nextRest=rest;stats.currentHealth=health;stats.UpdateHealthText();player.position=position;
            MenuPause.SetPaused(world,false);Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/WillowreachPlayChecks.txt",report);Debug.Log(string.Join("\n",report));
        }
    }
    [MenuItem("Tools/RPG/Scene Expansion/Preview Forest In Play")]
    public static void PreviewForest() { var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();if(EditorApplication.isPlaying&&world!=null){world.player.position=new Vector3(21,-16,0);world.weather.SetWeather(WeatherCycle2D.Weather.Rain);} }
    [MenuItem("Tools/RPG/Scene Expansion/Preview Ruins In Play")]
    public static void PreviewRuins() { var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();if(EditorApplication.isPlaying&&world!=null){world.player.position=new Vector3(-22,12,0);world.weather.SetWeather(WeatherCycle2D.Weather.Snow);} }
    [MenuItem("Tools/RPG/Scene Expansion/Preview Gold In Play")]
    public static void PreviewGold() { var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();if(!EditorApplication.isPlaying||world==null)return;world.player.position=world.nodes.First(n=>n.kind==WillowreachNode.Kind.Supply&&n.item!=null&&n.item.name=="Gold"&&n.indicator!=null).transform.position+Vector3.down;world.weather.SetWeather(WeatherCycle2D.Weather.Sunny); }
    [MenuItem("Tools/RPG/Scene Expansion/Preview Bridge In Play")]
    public static void PreviewBridge() { var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();if(EditorApplication.isPlaying&&world!=null)world.player.position=new Vector3(-8,5,0); }
    [MenuItem("Tools/RPG/Scene Expansion/Preview Shop Door In Play")]
    public static void PreviewShopDoor() { var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();if(EditorApplication.isPlaying&&world!=null)world.player.position=world.nodes.First(n=>n.kind==WillowreachNode.Kind.Shop).transform.position; }
}
