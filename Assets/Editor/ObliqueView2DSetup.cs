using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class ObliqueView2DSetup
{
    static ObliqueView2DSetup()
    {
        EditorApplication.delayCall += ConfigureLoadedScenes;
        EditorSceneManager.sceneOpened += (scene, mode) => EditorApplication.delayCall += ConfigureLoadedScenes;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
                EditorApplication.delayCall += ConfigureLoadedScenes;
        };
    }

    [MenuItem("Tools/RPG/Apply 3-4 View")]
    public static void ConfigureLoadedScenes()
    {
        // Repair the live Play Mode copy too, without stopping or saving the user's game.
        for (int i = 0; i < SceneManager.sceneCount; i++)
            ObliqueView2D.RepairWorldPrompts(SceneManager.GetSceneAt(i));
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            var scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded || (scene.name != "RPG1" && scene.name != "RPG2")) continue;
            foreach (var root in scene.GetRootGameObjects())
                Undo.RegisterFullObjectHierarchyUndo(root, "Apply 3/4 view");
            ObliqueView2D.Configure(scene);
            EditorSceneManager.MarkSceneDirty(scene);
        }
        SceneView.RepaintAll();
    }
}
