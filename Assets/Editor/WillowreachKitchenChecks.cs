using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class WillowreachKitchenChecks
{
    [MenuItem("Tools/RPG/Scene Expansion/Preview Kitchen In Play")]
    public static void Preview()
    {
        if(!EditorApplication.isPlaying)return;var k=UnityEngine.Object.FindObjectOfType<WillowreachKitchen>();
        k.world.player.position=k.counters[0].transform.position;
        var inv=InventortyManager.Instance;if(inv.GetItemQuantity(k.pumpkin)==0)inv.AddItem(k.pumpkin,1);if(inv.GetItemQuantity(k.mushroom)==0)inv.AddItem(k.mushroom,1);
        k.world.weather.SetWeather(WeatherCycle2D.Weather.Sunny);
    }
    [MenuItem("Tools/RPG/Scene Expansion/Check Kitchen In Play")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        var k=UnityEngine.Object.FindObjectOfType<WillowreachKitchen>();var w=k.world;var a=w.GetComponent<WillowreachAdventure>();var inv=InventortyManager.Instance;
        var items=inv.itemSlots.Select(s=>s.itemSO).ToArray();var quantities=inv.itemSlots.Select(s=>s.quantity).ToArray();
        int seals=a.cargoSeals,success=k.successes,hp=StatsManager.Instance.currentHealth;bool cooking=k.cooking,carrying=a.carrying,ready=a.ready;float start=k.started,retry=k.retryAt,deadline=a.deadline;int integrity=a.integrity;Vector3 position=w.player.position;
        var lines=new List<string>();Action<bool,string> check=(ok,s)=>lines.Add((ok?"PASS ":"FAIL ")+s);
        try
        {
            foreach(var s in inv.itemSlots){s.itemSO=null;s.quantity=0;s.UpdateUI();}
            k.cooking=false;k.successes=0;k.retryAt=0;a.cargoSeals=0;
            check(!k.Press(k.counters[0])&&!k.cooking,"Missing ingredients cannot start kitchen challenge");
            inv.AddItem(k.pumpkin,4);inv.AddItem(k.mushroom,4);
            check(k.Press(k.counters[0])&&k.cooking,"Window starts a timed cooking challenge");
            check(inv.GetItemQuantity(k.pumpkin)==4&&inv.GetItemQuantity(k.mushroom)==4,"Starting does not consume ingredients");
            check(!k.Press(k.counters[0])&&!k.cooking&&a.cargoSeals==0,"Early press fails without granting seal");
            check(inv.GetItemQuantity(k.pumpkin)==4&&inv.GetItemQuantity(k.mushroom)==4,"Failed timing retains both ingredients");
            k.retryAt=0;k.Press(k.counters[0]);k.started=Time.time-k.target-.3f;
            check(k.Press(k.counters[0])&&k.successes==1&&a.cargoSeals==1,"Correct timing grants exactly one cargo seal");
            check(inv.GetItemQuantity(k.pumpkin)==3&&inv.GetItemQuantity(k.mushroom)==3,"Success consumes exactly one of each ingredient");
            check(!k.Press(k.counters[0])&&a.cargoSeals==1,"Cooldown blocks duplicate reward on repeated keypress");
            a.carrying=true;a.deadline=Time.time+150;a.integrity=3;w.player.position=new Vector3(0,6,0);a.Tick(2.1f);
            check(a.cargoSeals==0&&a.integrity==3,"Seal absorbs an actual open-water cargo damage event");
            a.Tick(2.1f);check(a.integrity==2,"Further damage reduces integrity after seal is spent");
            a.carrying=false;k.retryAt=0;k.Press(k.counters[1]);k.started=Time.time-k.target-1.5f;
            check(!k.Press(k.counters[1])&&inv.GetItemQuantity(k.pumpkin)==3,"Late timing cannot consume ingredients or pay");
            k.retryAt=0;k.successes=3;check(!k.Press(k.counters[0]),"Three completed meals cap repeatable seal production");
            foreach(var counter in k.counters)
            {
                var f=counter.transform.parent.GetComponent<WillowreachWindowShop>();
                check(f.display.sprite!=null&&!f.source.enabled&&f.display.sortingOrder>f.house.sortingOrder,"Live merchant is framed in open window");
                check(f.counterFeet.GetComponent<SpriteRenderer>().sortingOrder>f.display.sortingOrder,"Window sill renders in front of merchant lower edge");
                check(f.portrait.sprite==f.source.sprite&&f.merchant.portraitTarget==f.portrait.transform,"Portrait retains full animated merchant sprite");
            }
        }
        finally
        {
            for(int i=0;i<items.Length;i++){inv.itemSlots[i].itemSO=items[i];inv.itemSlots[i].quantity=quantities[i];inv.itemSlots[i].UpdateUI();}
            a.cargoSeals=seals;a.carrying=carrying;a.ready=ready;a.deadline=deadline;a.integrity=integrity;k.successes=success;k.cooking=cooking;k.started=start;k.retryAt=retry;StatsManager.Instance.currentHealth=hp;w.player.position=position;
            File.WriteAllLines("Logs/WillowreachKitchenPlay.txt",lines);Debug.Log(string.Join("\n",lines));
        }
    }
}
