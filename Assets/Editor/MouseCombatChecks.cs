using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
[InitializeOnLoad]
public static class MouseCombatChecks
{
    static bool pending;static double at;static string account;static readonly List<string> report=new List<string>();
    static MouseCombatChecks(){EditorApplication.update+=Tick;}
    static void Check(bool pass,string message){if(!pass)throw new Exception(message);report.Add("PASS "+message);}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Temp/MouseCombat.request"))
        {
            try{File.Delete("Temp/MouseCombat.request");}catch(IOException){return;}if(!EditorApplication.isPlaying)return;
            account="mouse-combat-test-"+Guid.NewGuid().ToString("N");LoginSession.Set(account,"test");SceneManager.LoadScene("RPG1");pending=true;at=EditorApplication.timeSinceStartup+2;
        }
        if(!pending||EditorApplication.timeSinceStartup<at||!SaveSystem.IsReady)return;pending=false;report.Clear();var cleanup=new List<GameObject>();
        var stats=StatsManager.Instance;int originalDamage=stats.damage;float originalRange=stats.weaponRange;bool error=false;
        Application.LogCallback capture=(message,stack,type)=>{if(type==LogType.Error||type==LogType.Exception)error=true;};Application.logMessageReceived+=capture;
        try
        {
            var player=UnityEngine.Object.FindObjectOfType<PlayerMovement>();var sword=player.GetComponent<player_combat>();var bow=player.GetComponent<Player_Bow>();
            bow.enabled=false;sword.enabled=true;sword.FinishAttacking();sword.Attack();Check(sword.anim.GetBool("isAttacking"),"Sword mode attack starts original animation");sword.FinishAttacking();
            typeof(player_combat).GetField("timer",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(sword,0f);
            Time.timeScale=0;sword.Attack();Check(!sword.anim.GetBool("isAttacking"),"Paused sword cannot attack");Time.timeScale=1;
            bow.enabled=true;sword.Attack();Check(!sword.anim.GetBool("isAttacking"),"Bow mode rejects sword even if component was accidentally enabled");bow.enabled=false;
            var target=new GameObject("Lethal sword test target");cleanup.Add(target);target.layer=6;target.transform.position=new Vector3(2000,2000,0);
            var health=target.AddComponent<Enemy_Health>();health.maxHealth=100;health.currenetHealth=1;health.expReward=0;health.preserveForRetry=true;
            var body=target.AddComponent<Rigidbody2D>();body.gravityScale=0;target.AddComponent<BoxCollider2D>();var knock=target.AddComponent<Enemy_Knockback>();
            var point=sword.attackPoint.position;stats.damage=10;stats.weaponRange=.5f;sword.attackPoint.position=target.transform.position;Physics2D.SyncTransforms();
            sword.DealDamage();Check(!target.activeSelf&&health.currenetHealth<=0,"Lethal sword hit disables enemy");
            knock.Knockback(player.transform,3,.1f,.1f);Check(!error,"Inactive enemy ignores knockback without coroutine errors");sword.attackPoint.position=point;
            target.SetActive(true);health.Revive();health.currenetHealth=1;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Arrow_0.prefab");var arrow=UnityEngine.Object.Instantiate(prefab,target.transform.position+Vector3.left,Quaternion.identity);cleanup.Add(arrow);arrow.GetComponent<Arrow>().damage=10;
            Check(arrow.GetComponent<Arrow>().TryHit(target.GetComponent<Collider2D>())&&!target.activeSelf,"Lethal arrow hit disables enemy");knock.Knockback(player.transform,3,.1f,.1f);Check(!error,"Lethal arrow does not start coroutine on disabled enemy");
            health.Revive();body.velocity=Vector2.zero;knock.Knockback(player.transform,3,.1f,.1f);Check(body.velocity.sqrMagnitude>0,"Living enemy still receives knockback");target.SetActive(false);Check(body.velocity==Vector2.zero,"Interrupted knockback clears velocity");
            Check(!error,"No Error or Exception emitted by combat checks");File.WriteAllLines("Logs/MouseCombatChecks.txt",report);
        }
        catch(Exception e){report.Add("FAIL "+e);File.WriteAllLines("Logs/MouseCombatChecks.txt",report);Debug.LogException(e);}
        finally
        {
            Application.logMessageReceived-=capture;stats.damage=originalDamage;stats.weaponRange=originalRange;foreach(var go in cleanup)if(go!=null)UnityEngine.Object.DestroyImmediate(go);
            Time.timeScale=1;EditorApplication.isPlaying=false;string cleanupAccount=account;
            EditorApplication.delayCall+=()=>{foreach(string suffix in new[]{"",".bak",".tmp"}){string path=SaveSystem.FilePath(cleanupAccount)+suffix;if(File.Exists(path))File.Delete(path);}};
        }
    }
}
