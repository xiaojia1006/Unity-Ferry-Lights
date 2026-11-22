using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static partial class SceneExpansionWorkshop
{
    public const string Folder = "Assets/SceneExpansion";
    public const string Baseline = Folder + "/BeforeExpansion.unity";
    public const string Expanded = "Assets/Scenes/Willowreach.unity";

    [InitializeOnLoadMethod]
    private static void ScheduleSceneRelocation()
    {
        EditorApplication.delayCall += RelocateExpandedScene;
    }

    // Move through Unity so its GUID, open-scene identity and asset references survive.
    private static void RelocateExpandedScene()
    {
        const string previous = "Assets/SceneExpansion/Willowreach.unity";
        if (!File.Exists(previous) || File.Exists(Expanded)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorApplication.delayCall += RelocateExpandedScene;
            return;
        }
        string error = AssetDatabase.MoveAsset(previous, Expanded);
        if (!string.IsNullOrEmpty(error)) { Debug.LogError("Scene relocation failed: " + error); return; }
        EditorBuildSettings.scenes = EditorBuildSettings.scenes.Select(s =>
            s.path == previous ? new EditorBuildSettingsScene(Expanded, s.enabled) : s).ToArray();
        Directory.CreateDirectory("Logs");
        File.WriteAllText("Logs/WillowreachRelocation.txt", "Moved to " + Expanded + "\nGUID: " + AssetDatabase.AssetPathToGUID(Expanded));
        Debug.Log("Willowreach moved beside RPG1 and RPG2: " + Expanded);
    }

    [MenuItem("Tools/RPG/Scene Expansion/1 Capture Original And Inspect")]
    public static void Capture()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Directory.CreateDirectory(Folder);
        var scene = SceneManager.GetActiveScene();
        if (scene.path == Expanded || scene.path == Baseline) throw new InvalidOperationException("Capture the original RPG scene only.");
        if (File.Exists(Baseline)) throw new InvalidOperationException("The original snapshot already exists; refusing to overwrite it.");
        File.WriteAllText(Folder+"/OriginalPath.txt",scene.path);
        if (!EditorSceneManager.SaveScene(scene,Baseline,true)) throw new IOException("Unable to capture live scene.");
        Inspect();
        AssetDatabase.Refresh();
        Debug.Log("Original disk scene is untouched. Live scene including unsaved edits captured in " + Baseline);
    }

    [MenuItem("Tools/RPG/Scene Expansion/Inspect Current Scene")]
    public static void Inspect()
    {
        var log = new StringBuilder(); var scene = SceneManager.GetActiveScene();
        log.AppendLine("SCENE "+scene.path);
        foreach(var root in scene.GetRootGameObjects())
        {
            log.AppendLine("ROOT "+root.name+" at "+root.transform.position+" active="+root.activeSelf);
            foreach(var map in root.GetComponentsInChildren<Tilemap>(true))
            {
                var r=map.GetComponent<TilemapRenderer>();
                var tiles=new System.Collections.Generic.HashSet<TileBase>();
                foreach(var cell in map.cellBounds.allPositionsWithin) { var tile=map.GetTile(cell); if(tile!=null) tiles.Add(tile); }
                log.AppendLine(" MAP "+map.name+" pos="+map.transform.position+" scale="+map.transform.lossyScale+" bounds="+map.cellBounds+" order="+r.sortingOrder+" colliders="+string.Join(",",map.GetComponents<Collider2D>().Select(c=>c.GetType().Name)));
                foreach(var tile in tiles.Take(18)) log.AppendLine("  TILE "+tile.name+" "+AssetDatabase.GetAssetPath(tile));
            }
            foreach(var sprite in root.GetComponentsInChildren<SpriteRenderer>(true).Where(s=>s.gameObject.hideFlags==HideFlags.None).Take(45))
                log.AppendLine(" SPRITE "+sprite.name+" at="+sprite.transform.position+" size="+sprite.bounds.size+" art="+(sprite.sprite!=null?AssetDatabase.GetAssetPath(sprite.sprite)+"/"+sprite.sprite.name:"null")+" components="+string.Join(",",sprite.GetComponents<Component>().Select(c=>c!=null?c.GetType().Name:"MISSING")));
            foreach(var collider in root.GetComponentsInChildren<Collider2D>(true).Where(c=>c is PolygonCollider2D || c.name=="Confiner"))
                log.AppendLine(" COLLIDER "+collider.name+" bounds="+collider.bounds+" trigger="+collider.isTrigger);
        }
        Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/SceneExpansionInventory.txt",log.ToString());
    }

    [MenuItem("Tools/RPG/Scene Expansion/Open Original Snapshot")]
    public static void OpenOriginal()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        if(File.Exists(Baseline)) EditorSceneManager.OpenScene(Baseline);
    }
}
