using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static class RPG1FrontierChecks
{
    [MenuItem("Tools/RPG/RPG1 Frontier/Verify Scene")]
    public static void VerifyScene()
    {
        var results=new List<string>();Action<bool,string> check=(ok,s)=>results.Add((ok?"PASS ":"FAIL ")+s);
        var w=UnityEngine.Object.FindObjectOfType<RPG1Frontier>();if(w==null){File.WriteAllText("Logs/RPG1FrontierSceneChecks.txt","FAIL frontier missing");return;}
        check(SceneManager.GetActiveScene().name=="RPG1","Target scene RPG1");
        check(w.hud.posts.Length==7&&w.hud.posts.All(p=>p!=null),"Seven configured interaction points");
        check(w.hud.chart!=null&&w.hud.parchment!=null&&w.hud.button!=null,"Map and existing UI assets assigned");
        check(w.arena.Length==2&&w.arena.All(e=>e!=null),"Two arena enemies assigned");
        check(w.GetComponentsInChildren<RPG1Sentinel>(true).Length==8,"Eight original animated enemies");
        check(w.GetComponentsInChildren<RPG1Harvest>(true).Length==6,"Six harvest sites use real Loot prefab");
        check(w.GetComponentsInChildren<RPG1Harvest>(true).All(h=>h.item!=null&&h.prefab!=null),"Harvest references complete");
        check(w.GetComponentsInChildren<RPG1ChestSession>(true).Length==4,"Four chests protected against scene re-entry");
        check(w.GetComponentsInChildren<Transform>(true).All(t=>!t.name.Contains("符石")),"No memory-stone objects");
        check(w.GetComponentsInChildren<WillowreachFlipbook>().Where(f=>f.name.Contains("pine")).All(f=>f.frames.Length==4&&f.frames.All(s=>s!=null)&&f.framesPerSecond==5),"Original four-frame tree animations at 5 fps");
        check(w.crate!=null&&w.plate!=null&&w.gate!=null,"Physical crate plate and gate assigned");
        check(w.crate.GetComponent<Rigidbody2D>().gravityScale==0,"Crate remains in 2D plane");
        check(w.GetComponentsInChildren<Collider2D>().Where(c=>c.name.Contains("watchtower")).All(c=>!c.isTrigger),"Watchtower bases are solid");
        Physics2D.SyncTransforms();
        bool prior=w.gate.activeSelf;w.gate.SetActive(true);Physics2D.SyncTransforms();var closed=Reach(w.spawn);
        foreach(int i in new[]{0,1,2,3,4})check(Near(closed,w.hud.posts[i].position),"Reachable interaction with gate closed: "+i);
        check(!Near(closed,w.hud.posts[5].position),"Closed fort gate cannot be bypassed");
        w.gate.SetActive(false);Physics2D.SyncTransforms();var open=Reach(w.spawn);check(Near(open,w.hud.posts[5].position)&&Near(open,w.hud.posts[6].position),"Both fort stations reachable after gate opens");
        foreach(var h in w.GetComponentsInChildren<RPG1Harvest>())check(Near(open,h.transform.position),"Reachable harvest "+h.name+" "+h.transform.position);
        w.gate.SetActive(prior);Physics2D.SyncTransforms();
        foreach(var g in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)))checkMissing(g.gameObject,results);
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/RPG1FrontierSceneChecks.txt",results);Debug.Log("RPG1 scene checks: "+results.Count+"; failures="+results.Count(s=>s.StartsWith("FAIL")));
    }
    static void checkMissing(GameObject g,List<string> results){if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(g)>0)results.Add("FAIL Missing script: "+g.name);}
    static HashSet<Vector2Int> Reach(Vector2 start)
    {
        const int width=125,height=89;const float step=.5f;var pass=new bool[width,height];var colliders=UnityEngine.Object.FindObjectsOfType<Collider2D>().Where(c=>c.enabled&&!c.isTrigger&&c.gameObject.layer==7&&c.GetComponent<RPG1PushCrate>()==null).ToArray();
        for(int x=0;x<width;x++)for(int y=0;y<height;y++){var p=new Vector2(-31+x*step,-21+y*step);pass[x,y]=!colliders.Any(c=>Vector2.Distance(c.ClosestPoint(p),p)<.24f);}
        var s=new Vector2Int(Mathf.RoundToInt((start.x+31)/step),Mathf.RoundToInt((start.y+21)/step));var visited=new HashSet<Vector2Int>();var q=new Queue<Vector2Int>();if(pass[s.x,s.y]){q.Enqueue(s);visited.Add(s);}var dirs=new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
        while(q.Count>0){var a=q.Dequeue();foreach(var d in dirs){var b=a+d;if(b.x>=0&&b.y>=0&&b.x<width&&b.y<height&&pass[b.x,b.y]&&visited.Add(b))q.Enqueue(b);}}
        return visited;
    }
    static bool Near(HashSet<Vector2Int> reach,Vector2 p)=>reach.Any(c=>Vector2.Distance(new Vector2(-31+c.x*.5f,-21+c.y*.5f),p)<1.2f);
    [MenuItem("Tools/RPG/RPG1 Frontier/Verify Play")]
    public static void VerifyPlay()
    {
        if(!Application.isPlaying)return;var w=UnityEngine.Object.FindObjectOfType<RPG1Frontier>();if(w==null)return;
        var lines=new List<string>();Action<bool,string> check=(ok,s)=>lines.Add((ok?"PASS ":"FAIL ")+s);
        var p=UnityEngine.Object.FindObjectOfType<PlayerMovement>();var position=p.transform.position;var cratePosition=w.crate.transform.position;int hp=StatsManager.Instance.currentHealth;var inv=InventortyManager.Instance;int coins=inv.gold;var originalXP=w.hud.experience;
        var fields=new[]{"west","east","won","paid"}.Select(n=>typeof(RPG1Frontier).GetField(n,BindingFlags.Static|BindingFlags.NonPublic)).ToArray();var values=fields.Select(f=>f.GetValue(null)).ToArray();
        try{
            check(w.GetComponentsInChildren<Canvas>().Length==1,"Exactly one activity HUD");check(w.transform.Find("RPG1 Field HUD/Mini map")!=null,"Minimap active");
            check(w.arena.All(e=>e.GetComponent<Enemy_Health>().invulnerable),"Dormant guards cannot be damaged");
            foreach(var f in fields)f.SetValue(null,false);w.ApplyState();w.Interact(5);check(!w.ArenaStarted,"Arena requires both power sources");
            w.crate.transform.position=w.plate.position;w.crate.GetComponent<Rigidbody2D>().position=w.plate.position;check(w.PlatePressed,"Crate activates pressure plate");
            w.crate.transform.position=cratePosition;w.crate.GetComponent<Rigidbody2D>().position=cratePosition;check(!w.PlatePressed,"Plate rejects absent crate");
            p.transform.position=w.crate.origin;p.rb.position=w.crate.origin;Physics2D.SyncTransforms();check(!w.crate.ResetCrate(),"Crate reset refuses occupied starting position");p.transform.position=position;p.rb.position=position;Physics2D.SyncTransforms();check(w.crate.ResetCrate(),"Crate reset works when clear");
            fields[0].SetValue(null,true);w.ApplyState();check(w.gate.activeSelf,"One power source does not open gate");fields[1].SetValue(null,true);w.ApplyState();check(!w.gate.activeSelf,"Two power sources open gate");
            w.Interact(5);check(w.ArenaStarted&&w.arena.All(e=>!e.GetComponent<Enemy_Health>().invulnerable),"Arena begins with live guards");w.ResetArena();check(!w.ArenaStarted&&w.arena.All(e=>e.Health>0&&e.GetComponent<Enemy_Health>().invulnerable),"Arena retry restores dormant guards");
            fields[2].SetValue(null,true);w.hud.experience=w.hud.gold;w.Interact(0);int after=inv.gold;w.Interact(0);check(after==coins+36&&inv.gold==after,"Completion rewards issued exactly once");
            var trees=w.GetComponentsInChildren<WillowreachFlipbook>();check(trees.All(f=>f.frames.All(s=>s!=null)),"Runtime flipbooks have complete sprites");
            check(w.GetComponentsInChildren<RPG1LostSheep>().Length==1,"Sheep rescue active");check(w.GetComponentsInChildren<RPG1SupplyOrder>().Length==1,"Bone commission active");
            check(w.GetComponentsInChildren<RPG1Sentinel>().All(e=>e.GetComponent<Animator>().runtimeAnimatorController!=null&&!e.GetComponent<Enemy_Movement>().enabled),"Original animators and single AI driver");
        }catch(Exception e){lines.Add("FAIL "+e);}
        finally{for(int i=0;i<fields.Length;i++)fields[i].SetValue(null,values[i]);w.hud.experience=originalXP;w.ApplyState();inv.gold=coins;if(inv.goldText!=null)inv.goldText.text=coins.ToString();StatsManager.Instance.currentHealth=hp;StatsManager.Instance.UpdateHealthText();p.transform.position=position;p.rb.position=position;p.rb.velocity=Vector2.zero;w.crate.transform.position=cratePosition;w.crate.GetComponent<Rigidbody2D>().position=cratePosition;}
        File.WriteAllLines("Logs/RPG1FrontierPlayChecks.txt",lines);Debug.Log("RPG1 play checks failures="+lines.Count(s=>s.StartsWith("FAIL")));
    }
}
