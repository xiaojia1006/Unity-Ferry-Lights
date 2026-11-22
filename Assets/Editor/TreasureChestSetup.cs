using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class TreasureChestSetup
{
    private const string ControllerPath = "Assets/Sprites/Animations/chest-Sheet_0.controller";
    private const string PrefabPath = "Assets/prefabs/Treasure Chest.prefab";
    private const string Pending = "Rpg.ChestChecks";
    private static double deadline;
    private static int stage;
    private static bool failed;
    private static int collected;
    private static int expectedCollected;
    private static readonly List<string> report = new List<string>();

    static TreasureChestSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(PrefabPath))
                BuildAssets();
            if (!EditorApplication.isPlayingOrWillChangePlaymode) RepairLoadedChests();
        };
        EditorSceneManager.sceneOpened += (scene, mode) => RepairLoadedChests();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) RepairLoadedChests();
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending, false)) return;
            deadline = EditorApplication.timeSinceStartup + 2;
            EditorApplication.update += CheckPlayMode;
        };
    }

    [MenuItem("Tools/RPG/Set Up Treasure Chest")]
    public static void BuildAssets()
    {
        const string tablePath = "Assets/scrpits/Inventory&Shop/ItemSOs/Chest Loot Table.asset";
        if (AssetDatabase.LoadAssetAtPath<ChestLootTable>(tablePath) == null)
        {
            var table = ScriptableObject.CreateInstance<ChestLootTable>();
            table.entries = AssetDatabase.FindAssets("t:ItemSO", new[] { "Assets/scrpits/Inventory&Shop/ItemSOs" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(item => item != null && !item.isGold && !item.isEXP)
                .OrderBy(item => item.name)
                .Select(item => new ChestLootTable.Entry { item = item }).ToArray();
            AssetDatabase.CreateAsset(table, tablePath);
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
        const string defaultsPath = "Assets/Resources/ChestDefaults.asset";
        var defaults = AssetDatabase.LoadAssetAtPath<ChestDefaults>(defaultsPath);
        if (defaults == null)
        {
            defaults = ScriptableObject.CreateInstance<ChestDefaults>();
            AssetDatabase.CreateAsset(defaults, defaultsPath);
        }
        if (defaults.lootPrefab == null)
            defaults.lootPrefab = AssetDatabase.LoadAssetAtPath<Loot>("Assets/prefabs/Loot Prefab.prefab");
        if (defaults.lootTable == null)
            defaults.lootTable = AssetDatabase.LoadAssetAtPath<ChestLootTable>(tablePath);
        EditorUtility.SetDirty(defaults);
        var sprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/UI/chest-Sheet.png")
            .OfType<Sprite>().OrderBy(sprite => int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1))).ToArray();
        if (sprites.Length != 8) throw new InvalidOperationException("Expected eight chest sprite frames.");
        var closed = MakeClip("Assets/Sprites/Animations/Chest Closed.anim", new[] { sprites[0] }, 1f);
        var opened = MakeClip("Assets/Sprites/Animations/Treasure Chest.anim", sprites, 10f);
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        if (controller.layers.Length == 0) controller.AddLayer("Base Layer");
        var machine = controller.layers[0].stateMachine;
        var idle = machine.states.FirstOrDefault(entry => entry.state.name == "Chest_Closed").state;
        if (idle == null) idle = machine.AddState("Chest_Closed");
        idle.motion = closed;
        var open = machine.states.FirstOrDefault(entry => entry.state.name == "Chest_Open").state;
        if (open == null) open = machine.AddState("Chest_Open");
        open.motion = opened;
        foreach (var transition in machine.anyStateTransitions) machine.RemoveAnyStateTransition(transition);
        foreach (var transition in idle.transitions) idle.RemoveTransition(transition);
        foreach (var transition in open.transitions) open.RemoveTransition(transition);
        machine.defaultState = idle;
        EditorUtility.SetDirty(controller);
        var chest = new GameObject("Treasure Chest");
        try
        {
            chest.AddComponent<SpriteRenderer>().sprite = sprites[0];
            chest.AddComponent<Animator>().runtimeAnimatorController = controller;
            var area = chest.AddComponent<BoxCollider2D>();
            // ConfigureEditorDefaults migrates this to the sprite's solid footprint.
            area.isTrigger = true;
            var interaction = chest.AddComponent<CircleCollider2D>();
            interaction.isTrigger = true;
            interaction.radius = 0.75f;
            chest.AddComponent<TreasureChest>().ConfigureEditorDefaults();
            PrefabUtility.SaveAsPrefabAsset(chest, PrefabPath);
        }
        finally { UnityEngine.Object.DestroyImmediate(chest); }
        AssetDatabase.SaveAssets();
        RepairLoadedChests();
    }

    private static void RepairLoadedChests()
    {
        foreach (var existing in Resources.FindObjectsOfTypeAll<TreasureChest>())
        {
            if (EditorUtility.IsPersistent(existing) || !existing.gameObject.scene.IsValid() || !existing.gameObject.scene.isLoaded)
                continue;
            var before = EditorJsonUtility.ToJson(existing);
            existing.ConfigureEditorDefaults();
            if (before == EditorJsonUtility.ToJson(existing)) continue;
            EditorUtility.SetDirty(existing);
            EditorSceneManager.MarkSceneDirty(existing.gameObject.scene);
        }
    }

    private static AnimationClip MakeClip(string path, Sprite[] sprites, float rate)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.frameRate = rate;
        var keys = new ObjectReferenceKeyframe[sprites.Length + 1];
        for (int i = 0; i < keys.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / rate, value = sprites[Math.Min(i, sprites.Length - 1)] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    public static void RunBatch()
    {
        BuildAssets();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
        var player = new GameObject("Chest Test Player") { tag = "Player" };
        player.transform.position = new Vector3(5, 0, 0);
        player.AddComponent<Rigidbody2D>().gravityScale = 0;
        player.AddComponent<CircleCollider2D>().radius = 0.15f;
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void Assert(bool condition, string message)
    {
        failed |= !condition;
        report.Add((condition ? "PASS: " : "FAIL: ") + message);
    }

    private static void CheckPlayMode()
    {
        if (EditorApplication.timeSinceStartup < deadline) return;
        try
        {
            var chest = UnityEngine.Object.FindObjectOfType<TreasureChest>();
            var player = GameObject.Find("Chest Test Player");
            if (stage == 0)
            {
                CheckRandomLoot();
                Assert(!chest.isOpen && chest.GetComponent<SpriteRenderer>().sprite.name == "chest-Sheet_0", "Chest remains visibly closed after two seconds");
                Assert(!chest.TryOpen() && UnityEngine.Object.FindObjectsOfType<Loot>().Length == 0, "Distant player cannot open or spawn rewards");
                Assert(!chest.GetComponent<BoxCollider2D>().isTrigger && chest.GetComponent<CircleCollider2D>().isTrigger,
                    "Chest has a solid body and a separate interaction trigger");
                player.transform.position = new Vector3(-0.65f, -0.1f, 0);
                player.GetComponent<Rigidbody2D>().velocity = Vector2.right * 2f;
                stage = 1;
                deadline = EditorApplication.timeSinceStartup + 0.7;
                return;
            }
            if (stage == 1)
            {
                Assert(chest.CanOpen, "Player entering real 2D trigger enables interaction");
                Assert(player.GetComponent<Collider2D>().bounds.max.x <= chest.GetComponent<BoxCollider2D>().bounds.min.x + 0.02f,
                    "Physics blocks a player moving toward the chest instead of letting them cross it");
                player.GetComponent<Rigidbody2D>().velocity = Vector2.zero;
                // Simulate an old scene instance, rather than only testing the ready-made prefab.
                var fields = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
                typeof(TreasureChest).GetField("lootTable", fields).SetValue(chest, null);
                typeof(TreasureChest).GetField("lootPrefab", fields).SetValue(chest, null);
                Assert(chest.TryOpen(), "Interaction opens the chest");
                Assert(typeof(TreasureChest).GetField("lootTable", fields).GetValue(chest) != null &&
                    typeof(TreasureChest).GetField("lootPrefab", fields).GetValue(chest) != null,
                    "Old chest with both loot references missing repairs itself at runtime");
                Assert(!chest.GetComponent<BoxCollider2D>().isTrigger, "Opened chest remains a solid obstacle");
                Assert(!chest.TryOpen(), "Repeated interaction cannot duplicate rewards");
                var drops = UnityEngine.Object.FindObjectsOfType<Loot>();
                expectedCollected = drops.Sum(drop => drop.quantity);
                Assert(drops.Length >= 1 && drops.Length <= 2 && drops.All(drop => drop.itemSO != null && drop.quantity > 0),
                    "Opening spawns valid random rewards from the configured table");
                player.transform.position = new Vector3(5, 0, 0);
                stage = 2;
                deadline = EditorApplication.timeSinceStartup + 1.2;
                return;
            }
            if (stage == 2)
            {
                Assert(chest.GetComponent<SpriteRenderer>().sprite.name == "chest-Sheet_7", "Opening animation reaches and holds its last frame");
                Assert(!chest.CanOpen, "Leaving trigger clears interaction range");
                Assert(UnityEngine.Object.FindObjectsOfType<Loot>().All(drop => drop.GetComponent<Collider2D>().enabled), "Dropped rewards become collectible after delay");
                Loot.OnItemLooted += Collect;
                player.transform.position = UnityEngine.Object.FindObjectsOfType<Loot>()[0].transform.position;
                stage = 3;
                deadline = EditorApplication.timeSinceStartup + 0.8;
                return;
            }
            if (stage == 3)
            {
                var remaining = UnityEngine.Object.FindObjectsOfType<Loot>();
                if (remaining.Length > 0)
                {
                    player.transform.position = remaining[0].transform.position;
                    stage = 4;
                    deadline = EditorApplication.timeSinceStartup + 0.8;
                    return;
                }
            }
            Assert(collected == expectedCollected && collected > 0 && UnityEngine.Object.FindObjectsOfType<Loot>().Length == 0,
                "Player collects every randomly generated reward through real collision triggers");
        }
        catch (Exception error) { failed = true; report.Add("FAIL: " + error); }
        EditorApplication.update -= CheckPlayMode;
        Loot.OnItemLooted -= Collect;
        SessionState.SetBool(Pending, false);
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/TreasureChestChecks.txt", report);
        EditorApplication.ExitPlaymode();
        if (Application.isBatchMode) EditorApplication.Exit(failed ? 1 : 0);
    }

    private static int Collect(ItemSO item, int quantity)
    {
        collected += quantity;
        return 0;
    }

    private static void CheckRandomLoot()
    {
        var first = ScriptableObject.CreateInstance<ItemSO>();
        var second = ScriptableObject.CreateInstance<ItemSO>();
        var table = ScriptableObject.CreateInstance<ChestLootTable>();
        try
        {
            table.minRolls = table.maxRolls = 1;
            table.entries = new[]
            {
                new ChestLootTable.Entry { item = first, weight = 1, minQuantity = 1, maxQuantity = 3 },
                new ChestLootTable.Entry { item = second, weight = 3, minQuantity = 1, maxQuantity = 3 },
                new ChestLootTable.Entry { item = first, weight = 0, minQuantity = 99, maxQuantity = 99 }
            };
            var random = new System.Random(12345);
            int firstCount = 0;
            var quantities = new HashSet<int>();
            bool valid = true;
            for (int i = 0; i < 2000; i++)
            {
                var result = table.Roll(random);
                valid &= result.Count == 1 && result[0].quantity >= 1 && result[0].quantity <= 3;
                if (result[0].item == first) firstCount++;
                quantities.Add(result[0].quantity);
            }
            Assert(valid && quantities.Count == 3, "Random quantities cover the configured range; zero-weight entries never drop");
            Assert(firstCount > 350 && firstCount < 650, "Weighted selection produces both items near the configured 1:3 ratio over 2000 rolls");
            table.entries = new[] { new ChestLootTable.Entry { item = first, weight = 1, minQuantity = 2, maxQuantity = 2 } };
            table.minRolls = table.maxRolls = 10;
            var merged = table.Roll(random);
            Assert(merged.Count == 1 && merged[0].quantity == 20, "Repeated selections merge without losing item quantities");
            table.entries[0].weight = 0;
            Assert(table.Roll(random).Count == 0, "A table with no eligible entries does not generate a reward");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(table);
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }
    }
}
