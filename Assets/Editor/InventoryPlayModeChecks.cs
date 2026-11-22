using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.UI;

// Runs against the actual scene UI and assets. Changes exist only in Play Mode.
[InitializeOnLoad]
public static class InventoryPlayModeChecks
{
    private const string Pending = "Rpg.InventoryChecks.Pending";
    private static readonly List<string> report = new List<string>();

    static InventoryPlayModeChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Pending, false))
            {
                SessionState.SetBool(Pending, false);
                readyAt = EditorApplication.timeSinceStartup + 1.0;
                EditorApplication.update += WaitForScene;
            }
        };
    }

    private static double readyAt;
    public static void RunBatch()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/RPG1.unity");
        SessionState.SetBool(Pending, true);
        EditorApplication.EnterPlaymode();
    }

    private static void WaitForScene()
    {
        if (EditorApplication.timeSinceStartup < readyAt)
            return;
        EditorApplication.update -= WaitForScene;
        Run();
    }

    [MenuItem("Tools/RPG/Check Inventory In Play Mode %#F9")]
    public static void Start()
    {
        if (EditorApplication.isPlaying)
            Run();
        else
        {
            var scene = SceneManager.GetActiveScene();
            if (scene.isDirty)
            {
                Debug.LogWarning("Save scene changes before running inventory checks.");
                return;
            }
            EditorSceneManager.OpenScene(scene.path);
            SessionState.SetBool(Pending, true);
            EditorApplication.EnterPlaymode();
        }
    }

    private static void Check(bool condition, string message)
    {
        report.Add((condition ? "PASS: " : "FAIL: ") + message);
    }

    private static void Run()
    {
        report.Add("SCENE: " + SceneManager.GetActiveScene().name);
        try
        {
            var inventory = UnityEngine.Object.FindObjectOfType<InventortyManager>();
            var stats = StatsManager.Instance;
            Check(inventory != null && stats != null, "Scene inventory and stats exist");
            if (inventory == null || stats == null)
                return;

            var pumpkin = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/pumpkin.asset");
            var mushroom = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Mushroom.asset");
            foreach (var entry in inventory.itemSlots)
            {
                entry.quantity = 0;
                entry.UpdateUI();
            }
            inventory.AddItem(mushroom, 2);
            inventory.AddItem(mushroom, 1);
            Check(inventory.itemSlots[0].itemSO == mushroom && inventory.itemSlots[0].quantity == 3 && inventory.itemSlots[1].itemSO == null,
                "Mushroom pickups 2+1 stack together as 3");
            inventory.AddItem(mushroom, 62);
            Check(inventory.itemSlots[0].quantity == 64 && inventory.itemSlots[1].quantity == 1,
                "Mushroom stack caps at 64 and preserves overflow");
            var slot = inventory.itemSlots[2];
            slot.itemSO = pumpkin;
            slot.quantity = 3;
            slot.UpdateUI();
            Canvas.ForceUpdateCanvases();
            report.Add("Slot hierarchy: " + Hierarchy(slot.transform));
            report.Add("Slot parent inventory: " + slot.GetComponentInParent<InventortyManager>());
            report.Add("Stats hierarchy: " + Hierarchy(stats.transform));
            report.Add("Use handler: " + inventory.useItem);

            var system = EventSystem.current;
            Check(system != null, "Scene has an EventSystem");
            if (system == null)
                return;
            var mouse = InputSystem.AddDevice<Mouse>();
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            var module = system.GetComponent<InputSystemUIInputModule>();
            if (module == null)
            {
                report.Add("Legacy input scene: using raycast checks and explicit click dispatch.");
            }
            foreach (var candidate in inventory.itemSlots)
            {
                candidate.itemSO = pumpkin;
                candidate.quantity = 10;
                candidate.UpdateUI();
            }
            Canvas.ForceUpdateCanvases();
            foreach (var candidate in inventory.itemSlots)
            {
                // Backpack storage slots hold data; their visible buttons belong to BackpackUI.
                var candidateRect = candidate.GetComponent<RectTransform>();
                if (candidateRect == null || !candidate.gameObject.activeInHierarchy) continue;
                var candidateCanvas = candidate.GetComponentInParent<Canvas>();
                if (candidateCanvas == null) continue;
                var candidateCamera = candidateCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : candidateCanvas.worldCamera;
                foreach (float x in new[] { -0.3f, 0f, 0.3f })
                {
                    var position = RectTransformUtility.WorldToScreenPoint(candidateCamera, candidateRect.TransformPoint(candidateRect.rect.center + new Vector2(candidateRect.rect.width * x, 0)));
                    var probe = new PointerEventData(system) { position = position };
                    var probeHits = new List<RaycastResult>();
                    system.RaycastAll(probe, probeHits);
                    Check(probeHits.Count > 0 && probeHits[0].gameObject.GetComponentInParent<InventorySlot>() == candidate,
                        candidate.name + " click region x=" + x + ", hit=" + (probeHits.Count > 0 ? Hierarchy(probeHits[0].gameObject.transform) : "none"));
                    int previous = candidate.quantity;
                    Click(mouse, module, position, false);
                    Check(candidate.quantity == previous - 1, candidate.name + " mouse press/release consumes pumpkin x=" + x);
                }
            }
            slot.quantity = 3;
            var canvas = slot.GetComponentInParent<Canvas>();
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var rect = slot.GetComponent<RectTransform>();
            report.Add("Canvas: " + canvas.renderMode + ", camera=" + camera + ", scale=" + canvas.transform.lossyScale + ", slot=" + rect.rect);
            var pointer = new PointerEventData(system)
            {
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center)),
                button = PointerEventData.InputButton.Left
            };
            var hits = new List<RaycastResult>();
            system.RaycastAll(pointer, hits);
            foreach (var hit in hits)
                report.Add("Raycast: " + Hierarchy(hit.gameObject.transform));
            Check(hits.Count > 0 && hits[0].gameObject.GetComponentInParent<InventorySlot>() == slot,
                "Visible inventory slot receives mouse input");

            stats.currentHealth = Mathf.Max(1, stats.maxHealth - 10);
            int health = stats.currentHealth, speed = stats.speed, damage = stats.damage;
            if (hits.Count > 0)
                Click(mouse, module, pointer.position, false);
            Check(slot.quantity == 2 && stats.currentHealth > health && stats.speed == speed + pumpkin.speed && stats.damage == damage + pumpkin.damage,
                "Left click consumes pumpkin and applies its configured effects");

            var before = UnityEngine.Object.FindObjectsOfType<Loot>().Length;
            int quantity = slot.quantity;
            pointer.button = PointerEventData.InputButton.Right;
            if (hits.Count > 0)
                Click(mouse, module, pointer.position, true);
            Check(slot.quantity == quantity - 1 && UnityEngine.Object.FindObjectsOfType<Loot>().Length == before + 1,
                "Right click drops one physical item");

            typeof(InventortyManager).GetField("dropPoint", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(inventory, null);
            quantity = slot.quantity;
            inventory.DropLoot(slot);
            Check(slot.quantity == quantity - 1, "Dropping automatically finds player when reference is missing");

            var material = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/bone.asset");
            slot.itemSO = material;
            slot.quantity = 2;
            inventory.UseItem(slot);
            Check(slot.quantity == 2, "Material without an effect is not consumed");
            InputSystem.RemoveDevice(mouse);
        }
        catch (Exception error)
        {
            report.Add("ERROR: " + error);
        }
        finally
        {
            string logDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
            Directory.CreateDirectory(logDirectory);
            File.WriteAllLines(Path.Combine(logDirectory, "InventoryPlayModeChecks.txt"), report);
            Debug.Log(string.Join("\n", report));
            if (SceneManager.GetActiveScene().name == "RPG1")
            {
                SceneManager.LoadScene("RPG2");
                readyAt = EditorApplication.timeSinceStartup + 1.0;
                EditorApplication.update += WaitForScene;
            }
            else
            {
                EditorApplication.ExitPlaymode();
                if (Application.isBatchMode)
                    EditorApplication.Exit(report.Exists(line => line.StartsWith("FAIL:") || line.StartsWith("ERROR:")) ? 1 : 0);
            }
        }
    }

    private static void Click(Mouse mouse, InputSystemUIInputModule module, Vector2 position, bool right)
    {
        if (module == null)
        {
            var pointer = new PointerEventData(EventSystem.current) { position = position, button = right ? PointerEventData.InputButton.Right : PointerEventData.InputButton.Left };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (hits.Count > 0) ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerClickHandler);
            return;
        }
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        typeof(InputSystem).GetMethod("Update", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(InputUpdateType) }, null).Invoke(null, new object[] { InputUpdateType.Dynamic });
        module.Process();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position }.WithButton(right ? MouseButton.Right : MouseButton.Left));
        typeof(InputSystem).GetMethod("Update", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(InputUpdateType) }, null).Invoke(null, new object[] { InputUpdateType.Dynamic });
        module.Process();
        InputSystem.QueueStateEvent(mouse, new MouseState { position = position });
        typeof(InputSystem).GetMethod("Update", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic, null, new[] { typeof(InputUpdateType) }, null).Invoke(null, new object[] { InputUpdateType.Dynamic });
        module.Process();
    }

    private static string Hierarchy(Transform transform)
    {
        return transform.parent == null ? transform.name : Hierarchy(transform.parent) + "/" + transform.name;
    }
}
