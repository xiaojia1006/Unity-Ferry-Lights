using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
[InitializeOnLoad]
public static class SaveWorkshop
{
    static SaveWorkshop(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/Save.request"))return;
        string command;try{command=File.ReadAllText("Temp/Save.request").Trim();File.Delete("Temp/Save.request");}catch(IOException){return;}
        try{if(command=="catalog"){Catalog();Identities();}else if(command=="save")SaveSystem.SaveNow();File.WriteAllText("Logs/SaveChecks.txt",command+" PASS");}catch(Exception e){File.WriteAllText("Logs/SaveChecks.txt",command+" FAIL "+e);Debug.LogException(e);}
    }
    static void Identities()
    {
        if(EditorApplication.isPlaying)throw new Exception("Stop Play first");
        string backup="Backups/SaveSetup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss");Directory.CreateDirectory(backup);
        foreach(string name in new[]{"RPG1","RPG2","Willowreach"})
        {
            string path="Assets/Scenes/"+name+".unity";File.Copy(path,backup+"/"+name+"-disk.unity");
            var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
            if(opened)scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(path,UnityEditor.SceneManagement.OpenSceneMode.Additive);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,backup+"/"+name+"-live.unity",true);
            foreach(var root in scene.GetRootGameObjects())foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                if(t.GetComponent<TreasureChest>()==null&&t.GetComponent<QuestManager>()==null&&t.GetComponent<RPG2Point>()==null&&t.GetComponent<RPG2Adventure>()==null&&t.GetComponent<RPG1Frontier>()==null&&t.GetComponent<Enemy_Health>()==null&&t.GetComponent<WillowreachNode>()==null&&t.GetComponent<WillowreachAdventure>()==null&&t.GetComponent<WillowreachPastoral>()==null&&t.GetComponent<WillowreachKitchen>()==null)continue;
                var identity=t.GetComponent<SaveIdentity>();if(identity==null)identity=t.gameObject.AddComponent<SaveIdentity>();
                if(string.IsNullOrEmpty(identity.id))identity.id=Guid.NewGuid().ToString("N");EditorUtility.SetDirty(identity);
            }
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
        }
        File.WriteAllText("Backups/LatestSaveSetup.txt",backup);
    }
    [MenuItem("Tools/RPG/Save/Refresh Item Catalog")]
    public static void Catalog()
    {
        const string path="Assets/Resources/SaveItemCatalog.asset";
        var bank=AssetDatabase.LoadAssetAtPath<SaveItemCatalog>(path);if(bank==null){bank=ScriptableObject.CreateInstance<SaveItemCatalog>();AssetDatabase.CreateAsset(bank,path);}
        var ids=AssetDatabase.FindAssets("t:ItemSO").OrderBy(x=>x).ToArray();bank.lootPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Loot Prefab.prefab").GetComponent<Loot>();bank.questIds=AssetDatabase.FindAssets("t:QuestSo").OrderBy(x=>x).ToArray();bank.quests=bank.questIds.Select(id=>AssetDatabase.LoadAssetAtPath<QuestSo>(AssetDatabase.GUIDToAssetPath(id))).ToArray();bank.ids=ids;bank.items=ids.Select(id=>AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(id))).ToArray();EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();
    }
}
