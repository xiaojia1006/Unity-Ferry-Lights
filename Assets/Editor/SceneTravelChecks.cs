using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
[InitializeOnLoad]
public static class SceneTravelChecks
{
    static int stage=-1;static double at;static string testAccount;static SceneChanger portal;static Vector2 center,outside;
    static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Require(bool ok,string text){if(!ok)throw new Exception(text);File.AppendAllText("Logs/SceneTravelChecks.txt","PASS "+text+"\n");}
    static SceneTravelChecks(){EditorApplication.update+=Tick;}
    static void Move(Vector2 p){var player=UnityEngine.Object.FindObjectOfType<PlayerMovement>();player.transform.position=p;player.rb.position=p;player.rb.velocity=Vector2.zero;Physics2D.SyncTransforms();}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        if(File.Exists("Temp/SceneTravel.request"))
        {
            try{File.Delete("Temp/SceneTravel.request");}catch(IOException){return;}if(!EditorApplication.isPlaying)return;
            File.WriteAllText("Logs/SceneTravelChecks.txt","");testAccount="travel-test-"+Guid.NewGuid().ToString("N");LoginSession.Set(testAccount,"test");SceneManager.LoadScene("RPG2");stage=0;at=EditorApplication.timeSinceStartup+2;
        }
        if(stage<0||EditorApplication.timeSinceStartup<at||!SaveSystem.IsReady)return;
        try
        {
            if(stage==0)
            {
                portal=UnityEngine.Object.FindObjectsOfType<SceneChanger>().First(p=>p.sceneToLoad=="RPG1");var zone=portal.GetComponents<Collider2D>().First(c=>c.isTrigger);
                center=zone.bounds.center;outside=center+Vector2.up*(zone.bounds.extents.y+2);
                typeof(SceneChanger).GetMethod("OnEnable",Flags).Invoke(portal,null);Move(center);stage=1;at=EditorApplication.timeSinceStartup+2;
            }
            else if(stage==1)
            {
                Require(SceneManager.GetActiveScene().name=="RPG2"&&!portal.IsArmed,"Arrival inside exit remains inert beyond cooldown");
                SaveSystem.SaveNow();SaveSystem.ReloadFromDisk();stage=2;at=EditorApplication.timeSinceStartup+2;
            }
            else if(stage==2)
            {
                portal=UnityEngine.Object.FindObjectsOfType<SceneChanger>().First(p=>p.sceneToLoad=="RPG1");
                Require(SceneManager.GetActiveScene().name=="RPG2"&&!portal.IsArmed,"Disk-restored exit position does not bounce");
                Move(outside);stage=3;at=EditorApplication.timeSinceStartup+1;
            }
            else if(stage==3)
            {
                Require(portal.IsArmed,"Leaving exit arms it again");
                Move(center);var body=UnityEngine.Object.FindObjectOfType<PlayerMovement>().GetComponents<Collider2D>().First(c=>!c.isTrigger);
                typeof(SceneChanger).GetMethod("OnTriggerEnter2D",Flags).Invoke(portal,new object[]{body});stage=4;at=EditorApplication.timeSinceStartup+2;
            }
            else if(stage==4)
            {
                Require(SceneManager.GetActiveScene().name=="RPG1","Re-entry performs intended RPG2 to RPG1 travel");
                SaveSystem.SaveNow();SceneManager.LoadScene("RPG2");stage=5;at=EditorApplication.timeSinceStartup+3;
            }
            else if(stage==5)
            {
                portal=UnityEngine.Object.FindObjectsOfType<SceneChanger>().First(p=>p.sceneToLoad=="RPG1");
                Require(SceneManager.GetActiveScene().name=="RPG2"&&!portal.IsArmed,"Returning to saved exit stays in destination instead of looping");Finish();
            }
        }
        catch(Exception e){File.AppendAllText("Logs/SceneTravelChecks.txt","FAIL "+e);Debug.LogException(e);Finish();}
    }
    static void Finish()
    {
        stage=-1;EditorApplication.isPlaying=false;string account=testAccount;
        EditorApplication.delayCall+=()=>{foreach(string suffix in new[]{"",".bak",".tmp"}){string path=SaveSystem.FilePath(account)+suffix;if(File.Exists(path))File.Delete(path);}};
    }
}
