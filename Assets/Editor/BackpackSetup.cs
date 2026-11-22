using UnityEditor;
using UnityEngine;
using TMPro;

[InitializeOnLoad]
public static class BackpackSetup
{
    static BackpackSetup() { EditorApplication.delayCall += EnsureTheme; }

    [MenuItem("Tools/RPG/Setup Backpack Theme")]
    public static void EnsureTheme()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        const string path="Assets/Resources/BackpackTheme.asset";
        if(AssetDatabase.LoadAssetAtPath<BackpackTheme>(path) != null) return;
        var theme=ScriptableObject.CreateInstance<BackpackTheme>();
        theme.parchment=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Banners/Banner_Vertical.png");
        theme.slot=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buttons/Button_Blue_9Slides.png");
        theme.ribbon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Ribbons/Ribbon_Yellow_3Slides.png");
        theme.button=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/Buttons/Button_Blue_3Slides.png");
        theme.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath("125cb55b44b24c4393181402bc6200e6"));
        AssetDatabase.CreateAsset(theme,path); AssetDatabase.SaveAssets();
    }
}
