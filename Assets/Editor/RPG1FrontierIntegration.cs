using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;

// Exercises real trigger pickup and delayed destruction over live player-loop frames.
public static class RPG1FrontierIntegration
{
    static RPG1Frontier w;static PlayerMovement player;static Vector3 originalPosition;static int originalGold,originalHealth;static ItemSO[] items;static int[] counts;static Loot loot;static RPG1Harvest patch;static double due;static int stage;static Vector2 enemyStart;static RPG1Sentinel enemy;static Sprite treeStart;static WillowreachFlipbook tree;static readonly List<string> report=new List<string>();
    public static void Run()
    {
        if(!Application.isPlaying)return;w=UnityEngine.Object.FindObjectOfType<RPG1Frontier>();player=UnityEngine.Object.FindObjectOfType<PlayerMovement>();report.Clear();stage=0;
        var inv=InventortyManager.Instance;items=inv.itemSlots.Select(s=>s.itemSO).ToArray();counts=inv.itemSlots.Select(s=>s.quantity).ToArray();originalGold=inv.gold;originalHealth=StatsManager.Instance.currentHealth;originalPosition=player.transform.position;
        patch=w.GetComponentsInChildren<RPG1Harvest>().First(h=>h.item.name=="bone");Check(patch.TryHarvest(),"Bone supply produces real animated Loot");Check(!patch.TryHarvest(),"Same supply cannot be harvested twice");loot=UnityEngine.Object.FindObjectsOfType<Loot>().First(l=>l.itemSO==patch.item);Move(loot.transform.position);due=EditorApplication.timeSinceStartup+1.5;EditorApplication.update+=Tick;
    }
    static void Check(bool ok,string s)=>report.Add((ok?"PASS ":"FAIL ")+s);
    static void Move(Vector3 p){player.transform.position=p;player.rb.position=p;player.rb.velocity=Vector2.zero;Physics2D.SyncTransforms();}
    static void Tick()
    {
        if(!Application.isPlaying){EditorApplication.update-=Tick;return;}if(EditorApplication.timeSinceStartup<due)return;
        try{
            var inv=InventortyManager.Instance;
            if(stage==0)
            {
                Check(loot==null,"Bone pickup disappears after trigger and pickup animation");Check(inv.GetItemQuantity(patch.item)>=patch.quantity,"Bone pickup enters shared backpack inventory");
                var order=w.GetComponentInChildren<RPG1SupplyOrder>();int before=inv.gold;Check(order.Complete()&&inv.gold==before+10,"Collected bones fulfill real commission");Check(!order.Complete()&&inv.gold==before+10,"Bone commission cannot pay twice");
                var prefab=AssetDatabase.LoadAssetAtPath<Loot>("Assets/prefabs/Loot Prefab.prefab");loot=UnityEngine.Object.Instantiate(prefab,w.spawn,Quaternion.identity);loot.Initialize(w.hud.gold,3);Move(w.spawn);due=EditorApplication.timeSinceStartup+1.5;stage++;return;
            }
            if(stage==1)
            {
                Check(loot==null&&inv.gold==originalGold+13,"Gold trigger pickup vanishes and credits exactly once");
                Move(w.spawn);tree=w.GetComponentsInChildren<WillowreachFlipbook>().First(f=>f.name.Contains("pine"));treeStart=tree.GetComponent<SpriteRenderer>().sprite;enemy=w.GetComponentsInChildren<RPG1Sentinel>().First(e=>!e.arenaOnly);enemyStart=enemy.transform.position;due=EditorApplication.timeSinceStartup+.27;stage++;return;
            }
            if(stage==2)
            {
                Check(tree.GetComponent<SpriteRenderer>().sprite!=treeStart,"Tree changes animation frame in live player loop");Check(Vector2.Distance(enemy.transform.position,enemyStart)>.005f&&enemy.GetComponent<Animator>().GetBool("isChasing"),"Guard patrol moves and drives walk animation");
                Move(new Vector3(-5,-14,0));player.GetComponent<PlayerHealth>().ChangeHealth(-999);due=EditorApplication.timeSinceStartup+.5;stage++;return;
            }
            if(stage==3)
            {
                Check(player.gameObject.activeInHierarchy&&StatsManager.Instance.currentHealth>0&&Vector2.Distance(player.transform.position,w.spawn)<.5f,"Death outside arena respawns at camp");
                var sentinel=w.arena[0];int before=sentinel.Health;sentinel.GetComponent<Enemy_Health>().ChangeHealth(-99);Check(sentinel.Health==before,"Dormant guard ignores lethal attack");
                var wall=w.GetComponentsInChildren<Collider2D>().First(c=>c.name=="Fort south west");Check(!sentinel.ClearLine((Vector2)wall.bounds.center+Vector2.down*2,(Vector2)wall.bounds.center+Vector2.up*2),"Enemy cannot hit through fort wall");
                var crate=w.crate;crate.ResetCrate();Move(crate.origin+Vector2.left*.7f);player.enabled=false;player.rb.velocity=Vector2.right*3;due=EditorApplication.timeSinceStartup+.4;stage++;return;
            }
            if(stage==4)
            {
                Check(w.crate.transform.position.x>w.crate.origin.x+.03f,"Player Rigidbody2D physically pushes crate");player.enabled=true;Move(originalPosition);w.crate.ResetCrate();Finish();
            }
        }catch(Exception e){report.Add("FAIL "+e);Finish();}
    }
    static void Finish()
    {
        EditorApplication.update-=Tick;player.enabled=true;var inv=InventortyManager.Instance;for(int i=0;i<items.Length;i++){inv.itemSlots[i].itemSO=items[i];inv.itemSlots[i].quantity=counts[i];inv.itemSlots[i].UpdateUI();}inv.gold=originalGold;if(inv.goldText!=null)inv.goldText.text=originalGold.ToString();StatsManager.Instance.currentHealth=originalHealth;StatsManager.Instance.UpdateHealthText();Move(originalPosition);
        typeof(RPG1SupplyOrder).GetField("paid",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,false);typeof(RPG1Harvest).GetField("spent",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(patch,false);patch.GetComponent<SpriteRenderer>().color=Color.white;
        File.WriteAllLines("Logs/RPG1FrontierIntegration.txt",report);Debug.Log("RPG1 live integration failures="+report.Count(s=>s.StartsWith("FAIL")));
    }
}
