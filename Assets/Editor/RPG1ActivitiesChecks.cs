using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class RPG1ActivitiesChecks
{
    [MenuItem("Tools/RPG/RPG1 Activities/Verify In Play")]
    public static void Verify()
    {
        RPG1FrontierChecks.VerifyPlay();
    }
    [MenuItem("Tools/RPG/RPG1 Activities/Open Willowreach For Title Check")]
    public static void OpenWillow(){if(!Application.isPlaying)EditorSceneManager.OpenScene("Assets/Scenes/Willowreach.unity");}
}
