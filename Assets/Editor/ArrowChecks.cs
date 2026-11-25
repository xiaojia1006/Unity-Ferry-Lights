using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class ArrowChecks
{
    static bool pending;static double at;static string account;static readonly System.Collections.Generic.List<string> report=new System.Collections.Generic.List<string>();
    static ArrowChecks(){EditorApplication.update+=Tick;}
    static void Check(bool value,string message){if(!value)throw new Exception(message);report.Add("PASS "+message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Temp/ArrowChecks.request"))
        {
            try{File.Delete("Temp/ArrowChecks.request");}catch(IOException){return;}if(!EditorApplication.isPlaying)return;
            account="arrow-test-"+Guid.NewGuid().ToString("N");LoginSession.Set(account,"test");SceneManager.LoadScene("RPG1");pending=true;at=EditorApplication.timeSinceStartup+2;
        }
        if(!pending||EditorApplication.timeSinceStartup<at||!SaveSystem.IsReady)return;pending=false;report.Clear();
        try
        {
            var inv=InventortyManager.Instance;var player=UnityEngine.Object.FindObjectOfType<PlayerMovement>();var bow=player.GetComponent<Player_Bow>();
            foreach(var s in inv.itemSlots){s.itemSO=null;s.quantity=0;s.UpdateUI();}
            Check(ArrowCrafting.Ammo!=null&&ArrowCrafting.Bone!=null,"Crafting assets available in builds");
            Check(!ArrowCrafting.Craft()&&ArrowCrafting.Count==0,"No bone cannot craft");
            inv.AddItem(ArrowCrafting.Bone,1);Check(ArrowCrafting.Craft()&&ArrowCrafting.Count==5&&inv.GetItemQuantity(ArrowCrafting.Bone)==0,"1 bone becomes exactly 5 arrows");
            var catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");Check(catalog.Find(catalog.Id(ArrowCrafting.Ammo))==ArrowCrafting.Ammo,"Arrow item round trips through save GUID catalog");
            var camera=Camera.main;var origin=player.transform.position;var screen=camera.WorldToScreenPoint(origin);
            foreach(var dir in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down})
            {Vector2 aim;Check(Player_Bow.TryMouseAim(camera,screen+(Vector3)(dir*60),origin,out aim)&&Vector2.Dot(aim,dir)>.99f,"Mouse world aim "+dir);}
            player.GetComponent<player_combat>().enabled=false;bow.enabled=true;
            int before=UnityEngine.Object.FindObjectsOfType<Arrow>().Length;Check(bow.RequestShot(),"Loaded bow accepts one shot");bow.ReleaseShot();
            Check(ArrowCrafting.Count==4&&UnityEngine.Object.FindObjectsOfType<Arrow>().Length==before+1,"Shot spawns arrow and consumes exactly one");bow.ReleaseShot();Check(ArrowCrafting.Count==4,"Duplicate animation event cannot consume another arrow");
            bow.enabled=false;bow.enabled=true;Time.timeScale=0;Check(!bow.RequestShot()&&ArrowCrafting.Count==4,"Paused menu does not spend ammunition");Time.timeScale=1;
            while(ArrowCrafting.SpendOne()){}Check(ArrowCrafting.Count==0&&!bow.RequestShot(),"Empty quiver rejects shooting");
            foreach(var s in inv.itemSlots){s.itemSO=ArrowCrafting.Ammo;s.quantity=64;s.UpdateUI();}
            inv.itemSlots[0].itemSO=ArrowCrafting.Bone.InventoryMaterial;inv.itemSlots[0].quantity=2;
            Check(!ArrowCrafting.Craft()&&inv.itemSlots[0].quantity==2,"Full backpack rejects batch without consuming material");
            inv.itemSlots[0].quantity=1;Check(ArrowCrafting.Craft()&&inv.GetItemQuantity(ArrowCrafting.Bone)==0,"Consuming final bone frees slot for crafting");
            File.WriteAllLines("Logs/ArrowChecks.txt",report);
        }
        catch(Exception e){report.Add("FAIL "+e);File.WriteAllLines("Logs/ArrowChecks.txt",report);Debug.LogException(e);}
        finally
        {
            Time.timeScale=1;EditorApplication.isPlaying=false;string cleanup=account;
            EditorApplication.delayCall+=()=>{foreach(string suffix in new[]{"",".bak",".tmp"}){string path=SaveSystem.FilePath(cleanup)+suffix;if(File.Exists(path))File.Delete(path);}};
        }
    }
}
