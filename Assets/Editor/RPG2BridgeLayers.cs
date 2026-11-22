using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static partial class RPG2Workshop
{
    [MenuItem("Tools/RPG/RPG2 Workshop/Fix Bridge Surface Layers")]
    public static void FixBridgeLayers()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Stop Play before saving bridge layers");
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();if(w==null||w.gameObject.scene.path!=ScenePath)throw new Exception("Open RPG2 first");
        var backup="Backups/RPG2BridgeLayers-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        EditorSceneManager.SaveScene(w.gameObject.scene,backup+"/RPG2-live.unity",true);
        var water=UnityEngine.Object.FindObjectOfType<WaterSurface2D>().GetComponent<SpriteRenderer>();
        foreach(var sr in w.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.transform.IsChildOf(w.bridge.transform)||s.name=="Fishing pier planks"||s.name=="Broken bridge posts"))
        {
            var depth=sr.GetComponent<ObliqueDepth2D>();if(depth!=null)UnityEngine.Object.DestroyImmediate(depth);
            sr.sortingLayerID=water.sortingLayerID;sr.sortingOrder=sr.name=="Broken bridge posts"?11:10;
        }
        EditorSceneManager.MarkSceneDirty(w.gameObject.scene);EditorSceneManager.SaveScene(w.gameObject.scene);
        File.WriteAllText("Logs/RPG2BridgeLayers.txt","Saved bridge deck and pier above water (-1) at order 10; actors retain Y depth around 5000.\nBackup: "+backup+"\n");
    }
    public static void PreviewBridgeLayers()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Enter Play first");
        var w=UnityEngine.Object.FindObjectOfType<RPG2Adventure>();w.CloseMenu();
        foreach(var timber in w.points.Where(p=>p.kind==RPG2Point.Kind.Timber))w.Interact(timber);
        w.Interact(w.points.First(p=>p.kind==RPG2Point.Kind.Bridge));
        if(!w.BridgeRepaired)throw new Exception("Bridge was not repaired");
        var water=UnityEngine.Object.FindObjectOfType<WaterSurface2D>().GetComponent<SpriteRenderer>();
        var planks=w.bridge.GetComponentsInChildren<SpriteRenderer>();
        if(planks.Length!=19||planks.Any(s=>s.sortingLayerID!=water.sortingLayerID||s.sortingOrder<=water.sortingOrder))throw new Exception("Bridge must render continuously above water after Awake");
        var player=w.Player.GetComponent<SpriteRenderer>();
        if(player.sortingOrder<=planks.Max(s=>s.sortingOrder))throw new Exception("Player must render above deck");
        ViewRPG2(new Vector2(-10,0));
        File.AppendAllText("Logs/RPG2BridgeLayers.txt","PASS after fresh Play: 19 bridge sections above water; player above bridge.\n");
    }
}
