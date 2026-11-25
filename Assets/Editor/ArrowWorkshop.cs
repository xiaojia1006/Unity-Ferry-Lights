using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
[InitializeOnLoad]
public static class ArrowWorkshop
{
    static ArrowWorkshop(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/ArrowSetup.request"))return;
        try{File.Delete("Temp/ArrowSetup.request");}catch(IOException){return;}
        try
        {
            var bone=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/bone.asset");
            const string aliasPath="Assets/Resources/Items/arrow_bone.asset";
            var alias=AssetDatabase.LoadAssetAtPath<ItemSO>(aliasPath);if(alias==null){alias=ScriptableObject.CreateInstance<ItemSO>();AssetDatabase.CreateAsset(alias,aliasPath);}
            alias.inventoryMaterial=bone;alias.itemName=bone.itemName;alias.icon=bone.icon;alias.stackSize=64;
            const string path="Assets/Resources/Items/bone_arrow.asset";var ammo=AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if(ammo==null){ammo=ScriptableObject.CreateInstance<ItemSO>();AssetDatabase.CreateAsset(ammo,path);}
            ammo.itemName="骨箭";ammo.itemDescription="弓箭消耗品，每次射击消耗1支。背包选中骨头后，使用按钮制作：1骨头→5骨箭。";ammo.stackSize=64;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/prefabs/Arrow_0.prefab");ammo.icon=prefab.GetComponent<SpriteRenderer>().sprite;
            EditorUtility.SetDirty(alias);EditorUtility.SetDirty(ammo);AssetDatabase.SaveAssets();SaveWorkshop.Catalog();
            File.WriteAllText("Logs/ArrowSetup.txt","PASS bone recipe, arrow item/icon and GUID save catalog configured");
        }catch(Exception e){File.WriteAllText("Logs/ArrowSetup.txt","FAIL "+e);Debug.LogException(e);}
    }
}
