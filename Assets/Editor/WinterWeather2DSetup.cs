using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class WinterWeather2DSetup
{
    private static double nextFrame;
    static WinterWeather2DSetup()
    {
        EditorApplication.delayCall += Install;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += Install;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += Install;
        };
        EditorApplication.update += TickPreview;
    }

    [MenuItem("Tools/RPG/Set Up Winter Weather")]
    public static void Install()
    {
        EnsureMaterial("WinterSurface", "RPG/Winter Surface");
        EnsureMaterial("PixelSnow", "RPG/Pixel Snowflakes");
        EnsureMaterial("PixelRain", "RPG/Pixel Rain");
        EnsureMaterial("SnowFootprint", "RPG/Snow Footprint");
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (scene.name != "RPG1" && scene.name != "RPG2") continue;
            WinterWeather2D.Install(scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        SceneView.RepaintAll();
    }

    private static void EnsureMaterial(string name, string shaderName)
    {
        string path = "Assets/Resources/" + name + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null) return;
        var shader = Shader.Find(shaderName);
        if (shader == null) return;
        AssetDatabase.CreateAsset(new Material(shader) { name = name }, path);
        AssetDatabase.SaveAssets();
    }

    private static void TickPreview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling ||
            EditorApplication.timeSinceStartup < nextFrame) return;
        nextFrame = EditorApplication.timeSinceStartup + 0.05;
        foreach (var weather in Object.FindObjectsOfType<WinterWeather2D>())
            if (weather.previewInEditor && weather.enabled)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                SceneView.RepaintAll();
                break;
            }
    }
}
