#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
internal static class WaterSurface2DScenePreview
{
    static WaterSurface2DScenePreview()
    {
        EditorApplication.delayCall += InstallInLoadedScenes;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
            EditorApplication.delayCall += InstallInLoadedScenes;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += InstallInLoadedScenes;
    }

    private static void InstallInLoadedScenes()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
        {
            var scene = SceneManager.GetSceneAt(sceneIndex);
            if (!scene.isLoaded) continue;

            bool changed = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var child in root.GetComponentsInChildren<Transform>(true))
                {
                    var water = child.gameObject;
                    if (water.name != "Water" || water.GetComponent<SpriteRenderer>() == null ||
                        water.GetComponent<WaterSurface2D>() != null)
                        continue;

                    Undo.AddComponent<WaterSurface2D>(water);
                    changed = true;
                }
            }

            if (changed) EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
