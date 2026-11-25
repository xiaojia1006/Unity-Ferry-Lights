using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
[InitializeOnLoad]
public static class ArrowHitChecks
{
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static ArrowHitChecks(){EditorApplication.update+=Tick;}
    static void Check(bool pass,string message,List<string> report){if(!pass)throw new Exception(message);report.Add("PASS "+message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/ArrowHit.request"))return;
        try{File.Delete("Temp/ArrowHit.request");}catch(IOException){return;}if(!EditorApplication.isPlaying)return;
        var report=new List<string>();var cleanup=new List<GameObject>();
        try
        {
            var from=Vector2.right;var target=Vector2.up;
            var one=Player_Bow.SmoothDirection(from,target,24,.1f);var split=from;for(int i=0;i<10;i++)split=Player_Bow.SmoothDirection(split,target,24,.01f);
            Check(Vector2.Distance(one,split)<.0001f&&one.x>0&&one.y>.9f,"Aim smoothing is continuous, responsive and frame-rate independent",report);
            Check(Vector2.Distance(Player_Bow.SmoothDirection(from,target,24,0),from)<.0001f,"Paused aim does not drift",report);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Arrow_0.prefab");
            for(int mode=0;mode<3;mode++)
            {
                Vector2 origin=new Vector2(1000+mode*10,1000);
                var enemy=new GameObject("Arrow damage test enemy");cleanup.Add(enemy);enemy.layer=6;enemy.transform.position=origin+Vector2.right*.9f;
                var health=enemy.AddComponent<Enemy_Health>();health.maxHealth=100;health.currenetHealth=100;
                var child=new GameObject("Thin hurtbox");child.transform.SetParent(enemy.transform,false);var hurt=child.AddComponent<BoxCollider2D>();hurt.size=new Vector2(.03f,1);hurt.isTrigger=true;
                var go=UnityEngine.Object.Instantiate(prefab,origin,Quaternion.identity);cleanup.Add(go);var arrow=go.GetComponent<Arrow>();arrow.speed=100;arrow.direction=Vector2.right;
                typeof(Arrow).GetMethod("Start",Flags).Invoke(arrow,null);
                if(mode==1){var wall=new GameObject("Test wall");cleanup.Add(wall);wall.layer=7;wall.transform.position=origin+Vector2.right*.4f;wall.AddComponent<BoxCollider2D>().size=new Vector2(.05f,1);}
                if(mode==2)health.invulnerable=true;
                Physics2D.SyncTransforms();typeof(Arrow).GetMethod("FixedUpdate",Flags).Invoke(arrow,null);
                int expected=mode==0?100-NightCombat2D.PlayerDamage(arrow.damage):100;
                Check(health.currenetHealth==expected&&arrow.HasHit,mode==0?"Fast arrow hits thin child hurtbox and subtracts real enemy health":mode==1?"Wall blocks arrow before enemy, no damage through wall":"Invulnerable enemy respects damage immunity",report);
                int before=health.currenetHealth;arrow.TryHit(hurt);Check(health.currenetHealth==before,"Repeated collision cannot deal damage twice (case "+mode+")",report);
            }
            File.WriteAllLines("Logs/ArrowHitChecks.txt",report);
        }
        catch(Exception e){report.Add("FAIL "+e);File.WriteAllLines("Logs/ArrowHitChecks.txt",report);Debug.LogException(e);}
        finally{foreach(var go in cleanup)if(go!=null)UnityEngine.Object.DestroyImmediate(go);}
    }
}
