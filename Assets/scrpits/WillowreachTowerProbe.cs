#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class WillowreachTowerProbe : MonoBehaviour
{
    [MenuItem("Tools/RPG/Scene Expansion/Check Tower Walking In Play")]
    public static void Run()
    {
        if(EditorApplication.isPlaying)new GameObject("Tower collision check").AddComponent<WillowreachTowerProbe>().StartCoroutineRoutine();
    }
    private void StartCoroutineRoutine()=>StartCoroutine(Check());
    private IEnumerator Check()
    {
        var world=FindObjectOfType<WillowreachWorld>();var player=world.player.GetComponent<PlayerMovement>();
        var rb=player.rb;var body=player.GetComponents<Collider2D>().First(c=>!c.isTrigger);
        var pos=rb.position;var velocity=rb.velocity;bool enabled=player.enabled;float scale=Time.timeScale;
        var lines=new List<string>();Action<bool,string> check=(ok,s)=>lines.Add((ok?"PASS ":"FAIL ")+s);
        try
        {
            Time.timeScale=1;player.enabled=false;
            foreach(var sr in world.GetComponentsInChildren<SpriteRenderer>().Where(s=>s.name=="Village watchtower"||s.name=="Mine watchtower"||s.name=="Ruin tower"))
            {
                var tower=sr.GetComponent<BoxCollider2D>();
                Vector2 offset=(Vector2)body.bounds.center-rb.position;
                rb.position=new Vector2(tower.bounds.center.x,tower.bounds.min.y-body.bounds.extents.y-.4f)-offset;
                Physics2D.SyncTransforms();rb.velocity=Vector2.up*3;
                yield return new WaitForSeconds(.75f);
                check(body.bounds.max.y<=tower.bounds.min.y+.06f&&body.bounds.max.y>=tower.bounds.min.y-.1f,"Actual Rigidbody walk stops in front of "+sr.name);
                rb.velocity=Vector2.zero;
                rb.position=new Vector2(tower.bounds.min.x-body.bounds.extents.x-.4f,tower.bounds.center.y)-offset;
                Physics2D.SyncTransforms();rb.velocity=Vector2.right*3;
                yield return new WaitForSeconds(.75f);
                check(body.bounds.max.x<=tower.bounds.min.x+.06f&&body.bounds.max.x>=tower.bounds.min.x-.1f,"Actual Rigidbody walk stops beside "+sr.name);
                rb.velocity=Vector2.zero;
            }
        }
        finally
        {
            rb.position=pos;rb.velocity=velocity;player.enabled=enabled;Time.timeScale=scale;Physics2D.SyncTransforms();
            File.WriteAllLines("Logs/WillowreachTowerWalking.txt",lines);Debug.Log(string.Join("\n",lines));Destroy(gameObject);
        }
    }
}
#endif
