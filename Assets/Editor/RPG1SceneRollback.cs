using System;
using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class RPG1SceneRollback
{
    [MenuItem("Tools/RPG/RPG1 Rollback/Restore Before Frontier Build")]
    public static void Restore()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        string sceneBackup=File.ReadAllText("Backups/LatestRPG1SceneBuild.txt").Trim();
        string codeBackup=File.ReadAllText("Backups/LatestRPG1Rebuild.txt").Trim();
        var replacements=new Dictionary<string,string>{
            {sceneBackup+"/RPG1.unity","Assets/Scenes/RPG1.unity"},
            {sceneBackup+"/RPG1.unity.meta","Assets/Scenes/RPG1.unity.meta"},
            {codeBackup+"/RPG1Activities.cs","Assets/scrpits/RPG1Activities.cs"},
            {codeBackup+"/RPG1ActivitiesSetup.cs","Assets/Editor/RPG1ActivitiesSetup.cs"},
            {codeBackup+"/RPG1ActivitiesChecks.cs","Assets/Editor/RPG1ActivitiesChecks.cs"},
            {codeBackup+"/Enemy_Health.cs","Assets/scrpits/Enemy/Enemy_Health.cs"},
            {codeBackup+"/Enemy_Movement.cs","Assets/scrpits/Enemy/Enemy_Movement.cs"},
            {sceneBackup+"/RPG1FieldMap.png","Assets/SceneExpansion/RPG1FieldMap.png"},
            {sceneBackup+"/RPG1FieldMap.png.meta","Assets/SceneExpansion/RPG1FieldMap.png.meta"}
        };
        foreach(var p in replacements)if(!File.Exists(p.Key))throw new FileNotFoundException("Rollback source missing",p.Key);
        if(!EditorUtility.DisplayDialog("恢复搭建前 RPG1","先保存当前版本，再恢复搭建前场景、脚本和地图。其他场景不回滚。","保存并恢复","取消"))return;
        string saved="Backups/RPG1BeforeRollback-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(saved);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),saved+"/current-live.unity",true);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        AssetDatabase.StartAssetEditing();
        try{
            foreach(var p in replacements){if(File.Exists(p.Value)){string copy=saved+"/"+p.Value;Directory.CreateDirectory(Path.GetDirectoryName(copy));File.Copy(p.Value,copy);}File.Copy(p.Key,p.Value,true);}
            var additions=new List<string>();additions.AddRange(Directory.GetFiles("Assets/Editor","RPG1Frontier*.cs"));
            foreach(string n in new[]{"RPG1Frontier","RPG1Sentinel","RPG1PushCrate","RPG1SawHazard","RPG1Harvest","RPG1LostSheep","RPG1ChestSession","RPG1TimberMotion","RPG1SupplyOrder"})additions.Add("Assets/scrpits/"+n+".cs");
            foreach(string p in additions)foreach(string f in new[]{p,p+".meta"})if(File.Exists(f)){string copy=saved+"/"+f;Directory.CreateDirectory(Path.GetDirectoryName(copy));File.Move(f,copy);}
        }finally{AssetDatabase.StopAssetEditing();}
        AssetDatabase.Refresh();EditorSceneManager.OpenScene("Assets/Scenes/RPG1.unity");
    }
}
