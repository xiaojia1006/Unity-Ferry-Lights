using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static partial class SceneExpansionWorkshop
{
    [MenuItem("Tools/RPG/Scene Expansion/Check Expanded Map")]
    public static void CheckWillowreach()
    {
        var lines=new List<string>();
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();
        if(world==null) throw new InvalidOperationException("Open Willowreach first.");
        Action<bool,string> check=(ok,name)=>lines.Add((ok?"PASS ":"FAIL ")+name);
        check(world.nodes.Count(n=>n.kind==WillowreachNode.Kind.Survey)==3,"Three weather objectives");
        check(world.nodes.Where(n=>n.kind==WillowreachNode.Kind.Survey).Select(n=>n.requiredWeather).Distinct().Count()==3,"Each weather has a purpose");
        check(world.nodes.All(n=>n.Reusable || n.item!=null),"All rewards reference existing item assets");
        check(world.bridges.Length==2,"Two safe crossings");
        check(UnityEngine.Object.FindObjectsOfType<ShopKeeper>().Length==2,"Both original merchants retained");
        check(UnityEngine.Object.FindObjectsOfType<TreasureChest>().Length==3,"Three existing random-loot chests retained");
        foreach(var shop in UnityEngine.Object.FindObjectsOfType<ShopKeeper>())
        {
            var sprites=shop.GetComponentsInChildren<SpriteRenderer>(true);
            check(sprites.Any(s=>s.name=="G_Idle" && s.sortingOrder>30000),"Merchant gold prompt stays above its background: "+shop.name);
        }
        var roots=SceneManager.GetActiveScene().GetRootGameObjects();
        check(!roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0),"No missing scripts");
        var allSprites=roots.SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).Where(s=>s.gameObject.activeInHierarchy && s.gameObject.hideFlags==HideFlags.None).ToArray();
        check(allSprites.All(s=>s.sprite!=null),"Every active world sprite has art");
        // Flood fill a player-sized disk through actual solid static colliders, not just path art.
        Physics2D.SyncTransforms();
        const float step=.5f;const int width=145,height=97;
        var solid=new HashSet<Vector2Int>();var hits=new Collider2D[16];var filter=new ContactFilter2D();filter.NoFilter();filter.useTriggers=false;
        for(int x=0;x<width;x++)for(int y=0;y<height;y++)
        {
            var p=new Vector2(-36+x*step,-24+y*step);int count=Physics2D.OverlapCircle(p,.32f,filter,hits);
            for(int i=0;i<count;i++) if(hits[i]!=null&&hits[i].attachedRigidbody==null) { solid.Add(new Vector2Int(x,y));break; }
        }
        Func<Vector2,Vector2Int> cell=p=>new Vector2Int(Mathf.RoundToInt((p.x+36)/step),Mathf.RoundToInt((p.y+24)/step));
        var start=cell(world.player.position);var seen=new HashSet<Vector2Int>();var queue=new Queue<Vector2Int>();queue.Enqueue(start);seen.Add(start);
        Vector2Int[] directions={Vector2Int.up,Vector2Int.down,Vector2Int.left,Vector2Int.right};
        while(queue.Count>0)
        {
            var c=queue.Dequeue();foreach(var d in directions){var n=c+d;if(n.x<0||n.y<0||n.x>=width||n.y>=height||solid.Contains(n)||!seen.Add(n))continue;queue.Enqueue(n);}
        }
        check(!solid.Contains(start),"Spawn is clear of solid scenery");
        foreach(var node in world.nodes)
        {
            var target=cell(node.transform.position);bool reachable=false;
            for(int dx=-3;dx<=3;dx++)for(int dy=-3;dy<=3;dy++)if(seen.Contains(target+new Vector2Int(dx,dy))&&new Vector2(dx,dy).magnitude*step<node.range)reachable=true;
            check(reachable,"Reachable interaction: "+node.title+" "+node.transform.position);
        }
        foreach(var chest in UnityEngine.Object.FindObjectsOfType<TreasureChest>())
        {var c=cell(chest.transform.position);check(directions.Any(d=>seen.Contains(c+d*2)),"Reachable chest: "+chest.transform.position);}
        var spawn=world.player.position;
        check(UnityEngine.Object.FindObjectsOfType<Enemy_Movement>().All(e=>Vector2.Distance(e.transform.position,spawn)>15),"No combat at the village spawn");
        check(UnityEngine.Object.FindObjectsOfType<WillowreachGuard>().Length==4,"All four enemies have local pursuit leashes");
        string backup=File.ReadAllText("Backups/LatestSceneExpansion.txt").Trim();
        check(Hash(Baseline)==Hash(Path.Combine(backup,"BeforeExpansion-live.unity")),"Live original snapshot is byte-identical to immutable backup");
        foreach(string name in new[]{"RPG1.unity","RPG2.unity"})
        {
            string beforeRepair=File.ReadAllText("Backups/LatestWillowreachFix.txt").Trim();
            bool same=Hash("Assets/Scenes/"+name)==Hash(Path.Combine(beforeRepair,name));
            if(File.Exists("Backups/LatestVillageAdventure.txt"))
            {
                string currentBackup=File.ReadAllText("Backups/LatestVillageAdventure.txt").Trim();
                // Later user edits must not be overwritten to satisfy an obsolete comparison.
                check(File.GetLastWriteTimeUtc("Assets/Scenes/"+name)<Directory.GetCreationTimeUtc(currentBackup),"Original scene was not written during village/adventure repairs: "+name);
                if(!same)lines.Add("INFO "+name+" differs from the older repair snapshot; its last write predates this repair session.");
            }
            else check(same,"Original scene unchanged by these repairs: "+name);
            if(Hash("Assets/Scenes/"+name)!=Hash(Path.Combine(backup,"Assets/Scenes/"+name)))lines.Add("INFO "+name+" had edits after the initial expansion backup; preserved without overwrite.");
        }
        Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/WillowreachChecks.txt",lines);
        Debug.Log(string.Join("\n",lines));
    }
    private static string Hash(string path) { using(var hash=SHA256.Create()) using(var stream=File.OpenRead(path)) return BitConverter.ToString(hash.ComputeHash(stream)); }

    [MenuItem("Tools/RPG/Scene Expansion/3 Roll Back To Original (Live Snapshot)")]
    public static void RollBackOriginal()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) { Debug.LogWarning("Stop Play before restoring.");return; }
        string backup=File.ReadAllText("Backups/LatestSceneExpansion.txt").Trim();
        if(Hash(Baseline)!=Hash(Path.Combine(backup,"BeforeExpansion-live.unity"))) throw new IOException("Snapshot checksum mismatch. Restore from immutable backup instead.");
        if(!EditorUtility.DisplayDialog("恢复原场景","恢复制作前的现场（包括当时未保存的改动），并恢复原 Build Settings。新地图文件仍保留。是否继续？","恢复原场景","取消")) return;
        string original=File.ReadAllText(Folder+"/OriginalPath.txt").Trim();
        EditorSceneManager.OpenScene(Baseline);
        // Copy bytes instead of resaving through the serializer: preserve the captured scene exactly.
        File.Copy(Baseline,original,true);
        File.Copy(Path.Combine(backup,"ProjectSettings/EditorBuildSettings.asset"),"ProjectSettings/EditorBuildSettings.asset",true);
        AssetDatabase.Refresh();EditorSceneManager.OpenScene(original);
        if(Hash(original)!=Hash(Baseline)) throw new IOException("Restored scene checksum mismatch.");
        Debug.Log("Original live scene restored. Full pre-expansion disk state remains in "+backup);
    }

    [MenuItem("Tools/RPG/Scene Expansion/Open Expanded Scene")]
    public static void OpenExpanded() { if(!EditorApplication.isPlayingOrWillChangePlaymode) EditorSceneManager.OpenScene(Expanded); }

    [MenuItem("Tools/RPG/Scene Expansion/Frame Whole Map")]
    public static void FrameWholeMap()
    { if(SceneView.lastActiveSceneView!=null)SceneView.lastActiveSceneView.LookAt(Vector3.zero,Quaternion.identity,43,true); }

    [MenuItem("Tools/RPG/Scene Expansion/Capture Map Overview")]
    public static void CaptureMapOverview()
    {
        var world=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();if(world==null||EditorApplication.isPlaying)return;
        var go=new GameObject("Temporary map overview");var camera=go.AddComponent<Camera>();
        var rt=new RenderTexture(1600,1100,24);var texture=new Texture2D(1600,1100,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;
        try
        {
            camera.orthographic=true;camera.orthographicSize=27.5f;camera.transform.position=new Vector3(0,0,-30);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.24f,.20f);camera.targetTexture=rt;
            camera.transparencySortMode=TransparencySortMode.CustomAxis;camera.transparencySortAxis=Vector3.up;camera.Render();
            RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,1600,1100),0,0);texture.Apply();
            File.WriteAllBytes("Logs/WillowreachOverview.png",texture.EncodeToPNG());
        }
        finally {RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(rt);UnityEngine.Object.DestroyImmediate(texture);}
        FrameWholeMap();
    }
}
