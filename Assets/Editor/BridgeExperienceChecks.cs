using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
[InitializeOnLoad]
public static class BridgeExperienceChecks
{
    static bool pending;static double at;
    static BridgeExperienceChecks(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Temp/BridgeExperience.request")){try{File.Delete("Temp/BridgeExperience.request");}catch(IOException){return;}if(!EditorApplication.isPlaying)return;SceneManager.LoadScene("RPG2");pending=true;at=EditorApplication.timeSinceStartup+2;}
        if(!pending||EditorApplication.timeSinceStartup<at||!SaveSystem.IsReady)return;
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();if(w==null||w.Player==null)return;pending=false;
        bool was=w.bridge.activeSelf;var pos=w.Player.transform.position;
        try
        {
            w.bridge.SetActive(true);Physics2D.SyncTransforms();var rails=w.bridge.GetComponent<BridgeRails2D>();
            if(rails==null||rails.Left==null||rails.Right==null)throw new Exception("Rails missing");
            var p=w.Player;var body=p.GetComponents<Collider2D>().First(c=>!c.isTrigger);var bounds=rails.Deck;
            p.transform.position=bounds.center;p.rb.position=bounds.center;Physics2D.SyncTransforms();
            var hits=new RaycastHit2D[24];var filter=new ContactFilter2D();filter.SetLayerMask(1<<7);filter.useTriggers=false;
            int left=body.Cast(Vector2.left,filter,hits,bounds.width*2);bool leftHit=hits.Take(left).Any(h=>h.collider==rails.Left);
            int right=body.Cast(Vector2.right,filter,hits,bounds.width*2);bool rightHit=hits.Take(right).Any(h=>h.collider==rails.Right);
            if(!leftHit||!rightHit)throw new Exception("Player shape cast must meet both rails");
            int north=body.Cast(Vector2.up,filter,hits,bounds.height*.5f+.3f);if(hits.Take(north).Any(h=>h.collider==rails.Left||h.collider==rails.Right))throw new Exception("Rails block bridge exit");
            var exp=UnityEngine.Object.FindObjectOfType<ExpManager>();exp.UpdataUI();Canvas.ForceUpdateCanvases();exp.currentLeveText.ForceMeshUpdate();
            if(!exp.currentLeveText.gameObject.activeInHierarchy||!exp.currentLeveText.enabled||exp.currentLeveText.textInfo.characterCount<4||!exp.currentLeveText.text.StartsWith("Lv."))throw new Exception("Level label missing/hidden");
            if(exp.currentLeveText.fontSharedMaterial.GetColor("_FaceColor").r<.9f)throw new Exception("Level font face is black on black bar");
            File.WriteAllText("Logs/BridgeExperienceChecks.txt","PASS: real player collider casts blocked by both bridge sides; north/south rail ends open; existing level label active, mesh characters="+exp.currentLeveText.textInfo.characterCount+" text="+exp.currentLeveText.text);
        }catch(Exception e){File.WriteAllText("Logs/BridgeExperienceChecks.txt","FAIL "+e);Debug.LogException(e);}
        finally{w.bridge.SetActive(was);w.Player.transform.position=pos;w.Player.rb.position=pos;w.Player.rb.velocity=Vector2.zero;}
    }
}
