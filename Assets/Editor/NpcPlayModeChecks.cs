using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class NpcPlayModeChecks
{
    private const string Pending = "Rpg.NpcChecks";
    private static readonly List<string> report = new List<string>();
    private static double deadline;
    private static bool failed;

    static NpcPlayModeChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                Application.logMessageReceived += Log;
                deadline = EditorApplication.timeSinceStartup + 4;
                EditorApplication.update += Check;
            }
        };
    }

    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RPG2.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Log(string message, string stack, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error ||
            message.Contains("Animator") || message.Contains("Layer Index") || message.Contains("NPC patrol"))
        {
            failed = true;
            report.Add("FAIL: " + message);
        }
    }

    private static void Assert(bool value, string message)
    {
        failed |= !value;
        report.Add((value ? "PASS: " : "FAIL: ") + message);
    }

    private static void Check()
    {
        if (EditorApplication.timeSinceStartup < deadline) return;
        EditorApplication.update -= Check;
        try
        {
            var npcs = UnityEngine.Object.FindObjectsOfType<NPC>();
            Assert(npcs.Length == 2, "RPG2 contains both NPCs");
            foreach (var npc in npcs)
            {
                var sprite = npc.transform.Find("Sprite");
                var anim = sprite == null ? null : sprite.GetComponent<Animator>();
                if (anim == null) anim = npc.GetComponent<Animator>();
                Assert(anim != null && anim.HasState(0, Animator.StringToHash("BluePawn_Idle")) &&
                    anim.HasState(0, Animator.StringToHash("BluePawn_Walk")), npc.name + " idle/walk states resolve");
                if (npc.name == "NPC1")
                    Assert(npc.currentState == NPC.NPCState.Wander && npc.wander.enabled && !npc.patorl.enabled,
                        "NPC1 wanders instead of selecting an empty patrol route");
                Assert(!npc.CompareTag("Player"), npc.name + " is not mistaken for player");
                npc.SwitchState(NPC.NPCState.Talk);
                Assert(npc.talk.enabled && npc.GetComponent<Rigidbody2D>().velocity == Vector2.zero,
                    npc.name + " talk stops movement");
                npc.SwitchState(NPC.NPCState.Wander);
                Assert(npc.wander.enabled && !npc.talk.enabled, npc.name + " returns from talk to movement");
                npc.gameObject.SetActive(false);
            }
        }
        catch (Exception error) { failed = true; report.Add("FAIL: " + error); }
        SessionState.SetBool(Pending, false);
        var path = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/NpcPlayModeChecks.txt"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllLines(path, report);
        Application.logMessageReceived -= Log;
        EditorApplication.ExitPlaymode();
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }
}
