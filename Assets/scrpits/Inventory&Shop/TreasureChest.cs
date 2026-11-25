using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Animator), typeof(BoxCollider2D))]
public class TreasureChest : MonoBehaviour
{
    public bool isOpen;
    public bool CanOpen;
    [SerializeField] private Animator anim;
    [SerializeField] private string openStateName = "Chest_Open";
    [SerializeField] private Loot lootPrefab;
    [SerializeField] private ChestLootTable lootTable;
    [SerializeField, Min(0.1f)] private float dropDistance = 0.8f;

    private readonly HashSet<Collider2D> playersInRange = new HashSet<Collider2D>();

    private void Awake()
    {
        ResolveMissingLootConfiguration();
        if (anim == null) anim = GetComponent<Animator>();
        ConfigureColliders(true);
        isOpen = false;
        CanOpen = false;
        if (anim != null && anim.runtimeAnimatorController != null &&
            anim.HasState(0, Animator.StringToHash("Chest_Closed")))
        {
            anim.Play("Chest_Closed", 0, 0f);
            anim.Update(0f);
        }
    }

    private void Update()
    {
        if (!BackpackUI.BlocksOtherInput && Input.GetKeyDown(KeyCode.F)) TryOpen();   // 开箱键：F
    }

    public bool TryOpen()
    {
        if (!CanOpen || isOpen) return false;
        ResolveMissingLootConfiguration();
        if (anim == null || anim.runtimeAnimatorController == null ||
            !anim.HasState(0, Animator.StringToHash(openStateName)))
        {
            Debug.LogWarning("Chest opening animation is not configured.", this);
            return false;
        }
        if (lootPrefab == null || lootTable == null)
        {
            Debug.LogWarning($"Chest configuration missing: {(lootPrefab == null ? "Loot Prefab " : "")}{(lootTable == null ? "Loot Table" : "")}", this);
            return false;
        }
        var rewards = lootTable.Roll(new System.Random(Random.Range(0, int.MaxValue)));
        if (rewards.Count == 0)
        {
            Debug.LogWarning("Chest loot table has no eligible items with positive weight.", this);
            return false;
        }

        // Mark first so repeated input cannot grant the rewards twice.
        isOpen = true;
        anim.Play(openStateName, 0, 0f);
        for (int i = 0; i < rewards.Count; i++)
        {
            float offset = (i - (rewards.Count - 1) * 0.5f) * 0.65f;
            Vector3 position = transform.position + new Vector3(offset, -dropDistance, 0f);
            Loot drop = Instantiate(lootPrefab, position, Quaternion.identity);
            drop.Initialize(rewards[i].item, rewards[i].quantity);
        }
        return true;
    }

    private void ResolveMissingLootConfiguration()
    {
        if (lootPrefab != null && lootTable != null) return;
        var defaults = Resources.Load<ChestDefaults>("ChestDefaults");
        if (defaults == null) return;
        if (lootPrefab == null) lootPrefab = defaults.lootPrefab;
        if (lootTable == null) lootTable = defaults.lootTable;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playersInRange.Add(other);
        CanOpen = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        playersInRange.Remove(other);
        CanOpen = playersInRange.Count > 0;
    }

    private void OnDisable()
    {
        playersInRange.Clear();
        CanOpen = false;
    }

    private void ConfigureColliders(bool createInteraction)
    {
        var body = GetComponent<BoxCollider2D>();
        if (body != null)
        {
            // Migrate the previous oversized trigger to a solid footprint.
            if (body.isTrigger)
            {
                var renderer = GetComponent<SpriteRenderer>();
                if (renderer != null && renderer.sprite != null)
                {
                    var bounds = renderer.sprite.bounds;
                    body.size = new Vector2(bounds.size.x * 0.85f, bounds.size.y * 0.45f);
                    body.offset = new Vector2(bounds.center.x, bounds.min.y + body.size.y * 0.5f);
                }
            }
            body.isTrigger = false;
        }

        var interaction = GetComponent<CircleCollider2D>();
        if (interaction == null && createInteraction)
        {
            interaction = gameObject.AddComponent<CircleCollider2D>();
            interaction.radius = 0.75f;
        }
        if (interaction != null) interaction.isTrigger = true;
    }

#if UNITY_EDITOR
    private void Reset() { ConfigureEditorDefaults(); }
    private void OnValidate() { if (!Application.isPlaying) ConfigureEditorDefaults(); }

    public void ConfigureEditorDefaults()
    {
        if (anim == null) anim = GetComponent<Animator>();
        var controller = UnityEditor.AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Sprites/Animations/chest-Sheet_0.controller");
        if (anim != null && controller != null &&
            (anim.runtimeAnimatorController == null || anim.runtimeAnimatorController == controller))
            anim.runtimeAnimatorController = controller;
        if (lootPrefab == null)
            lootPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<Loot>("Assets/prefabs/Loot Prefab.prefab");
        if (lootTable == null)
            lootTable = UnityEditor.AssetDatabase.LoadAssetAtPath<ChestLootTable>(
                "Assets/scrpits/Inventory&Shop/ItemSOs/Chest Loot Table.asset");
        ConfigureColliders(false);
    }
#endif
}
