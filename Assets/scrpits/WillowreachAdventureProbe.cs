#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class WillowreachAdventureProbe : MonoBehaviour
{
    [MenuItem("Tools/RPG/Scene Expansion/Check Adventures In Play")]
    public static void Run()
    {if(EditorApplication.isPlaying)new GameObject("Adventure validation").AddComponent<WillowreachAdventureProbe>().StartCoroutine(Check());}
    private static IEnumerator Check()
    {
        var a=FindObjectOfType<WillowreachAdventure>();var w=a.world;var inv=InventortyManager.Instance;
        var items=inv.itemSlots.Select(s=>s.itemSO).ToArray();var counts=inv.itemSlots.Select(s=>s.quantity).ToArray();int gold=inv.gold;
        var pos=w.player.position;int hp=StatsManager.Instance.currentHealth;
        var report=new List<string>();Action<bool,string> test=(ok,s)=>report.Add((ok?"PASS ":"FAIL ")+s);
        GameObject bone=null;
        try
        {
            foreach(var s in inv.itemSlots){s.itemSO=null;s.quantity=0;s.UpdateUI();}
            a.carrying=false;a.ready=false;a.deliveries=0;a.retryAt=0;
            test(a.Dispatch()&&a.carrying&&a.integrity==3,"Dispatch starts without consuming player items or money");
            int target=a.target;test(!a.Deliver((target+1)%3)&&a.carrying,"Wrong destination cannot finish delivery");
            w.player.position=new Vector3(-8,6,0);a.Tick(2.1f);test(a.integrity==3,"Bridge crossing protects cargo");
            w.player.position=new Vector3(0,6,0);a.Tick(2.1f);test(a.integrity==2,"Open water damages cargo after two seconds");
            StatsManager.Instance.currentHealth=hp-1;a.Tick(0);test(a.integrity==1,"Taking damage reduces cargo exactly once");a.Tick(0);test(a.integrity==1,"Unchanged health cannot damage cargo repeatedly");
            a.Cancel("test");a.retryAt=0;a.Dispatch();test(a.target!=target,"New contract chooses a different destination");
            a.deadline=Time.time-1;a.Tick(0);test(!a.carrying&&!a.ready,"Expired delivery fails without paying");
            a.retryAt=0;a.Dispatch();int chosen=a.target;w.player.position=a.destinations[chosen].transform.position;
            test(a.Deliver(chosen)&&a.ready&&!a.carrying,"Correct delivery becomes a pending reward");
            test(!a.Deliver(chosen)&&a.ready,"Repeated delivery cannot duplicate pending rewards");
            foreach(var s in inv.itemSlots){s.itemSO=a.mushroom;s.quantity=a.mushroom.stackSize;s.UpdateUI();}
            test(!a.PayDelivery()&&a.ready&&a.deliveries==0&&inv.gold==gold,"Full bag retains entire delivery reward without partial payment");
            foreach(var s in inv.itemSlots){s.itemSO=null;s.quantity=0;s.UpdateUI();}
            test(a.PayDelivery()&&!a.ready&&a.deliveries==1&&inv.gold==gold+24&&inv.GetItemQuantity(a.mushroom)==2,"Intact delivery pays exact gold and mushrooms");
            test(!a.PayDelivery()&&inv.gold==gold+24,"Claimed delivery cannot pay twice");
            a.deliveries=3;test(!a.Dispatch()&&!a.carrying,"Three-contract cap prevents unlimited gold farming");
            a.sequence=new int[0];a.puzzleSolved=false;
            test(!a.TouchRune(0)&&!a.puzzleSolved&&a.sequence.Length==4,"Fresh serialized empty sequence starts demonstration without paying");
            a.BeginPuzzle();
            test(a.sequence.Length==4&&a.sequence.Zip(a.sequence.Skip(1),(x,y)=>x!=y).All(v=>v),"Rune sequence contains four distinguishable flashes");
            test(!a.TouchRune(a.sequence[0])&&a.entered==0,"Puzzle rejects input during demonstration");
            a.showingUntil=0;a.TouchRune(a.sequence[0]);test(a.entered==1,"Correct rune advances one step");
            a.TouchRune((a.sequence[1]+1)%3);test(a.entered==0,"Wrong rune resets sequence without consuming items");
            foreach(var s in inv.itemSlots){s.itemSO=a.mushroom;s.quantity=a.mushroom.stackSize;s.UpdateUI();}
            foreach(int rune in a.sequence)a.TouchRune(rune);
            test(a.entered==4&&!a.puzzleSolved&&inv.gold==gold+24,"Solved puzzle retains reward when inventory is full");
            foreach(var s in inv.itemSlots){s.itemSO=null;s.quantity=0;s.UpdateUI();}
            a.TouchRune(0);test(a.puzzleSolved&&inv.gold==gold+49&&inv.GetItemQuantity(a.mushroom)==3,"Pending rune treasure pays exact reward after freeing space");
            test(!a.TouchRune(1)&&inv.gold==gold+49,"Rune treasure cannot pay twice");
            var source=w.GetComponentsInChildren<Loot>().First(l=>l.itemSO.name=="bone");
            bone=Instantiate(source.gameObject,w.player.position,Quaternion.identity);var loot=bone.GetComponent<Loot>();
            Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return null;
            test(loot.quantity==0&&loot.anim.GetCurrentAnimatorStateInfo(0).IsName("LootPinkUp"),"Bone trigger plays original RPG1 LootPinkUp state");
            yield return new WaitForSeconds(.15f);
            test(loot.sr.transform.localPosition.y>.1f||loot.sr.color.a<.9f,"Bone visibly lifts and fades instead of rigid delay disappearance");
            yield return new WaitForSeconds(.45f);test(bone==null,"Animated bone is removed after pickup");
        }
        finally
        {
            if(bone!=null)Destroy(bone);
            for(int i=0;i<items.Length;i++){inv.itemSlots[i].itemSO=items[i];inv.itemSlots[i].quantity=counts[i];inv.itemSlots[i].UpdateUI();}
            inv.gold=gold;if(inv.goldText!=null)inv.goldText.text=gold.ToString();w.player.position=pos;StatsManager.Instance.currentHealth=hp;
            a.carrying=a.ready=a.puzzleSolved=false;a.deliveries=a.entered=0;a.target=-1;a.retryAt=a.showingUntil=0;a.sequence=null;
            File.WriteAllLines("Logs/WillowreachAdventurePlay.txt",report);Debug.Log(string.Join("\n",report));
        }
    }
}
#endif
