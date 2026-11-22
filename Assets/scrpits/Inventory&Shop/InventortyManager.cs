using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public partial class InventortyManager : MonoBehaviour
{
    public static InventortyManager Instance;

    public InventorySlot[] itemSlots;
    public Useltem useItem;
    public int gold;
    public TMP_Text goldText;

    [SerializeField] private Loot lootPrefab;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private float dropDistance = 1.2f;

    public static event Action<int> OnExperienceGained;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            enabled = false;
            return;
        }
        Instance = this;
        EnsureBackpackCapacity(30);
        NormalizeMaterials();
        if (GetComponent<BackpackUI>() == null)
            gameObject.AddComponent<BackpackUI>();
        if (useItem == null)
            useItem = GetComponent<Useltem>();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void NormalizeMaterials()
    {
        var pending = new Dictionary<ItemSO, int>();
        if (itemSlots == null) return;
        foreach (var slot in itemSlots)
        {
            if (slot == null || slot.itemSO == null || slot.itemSO.InventoryMaterial == slot.itemSO) continue;
            var key = slot.itemSO.InventoryMaterial;
            pending[key] = (pending.ContainsKey(key) ? pending[key] : 0) + slot.quantity;
            slot.itemSO = null; slot.quantity = 0; slot.UpdateUI();
        }
        foreach (var entry in pending) AddItem(entry.Key, entry.Value);
    }

    // Reserve capacity for the whole reward batch before adding anything.
    public bool CanAddItems(Dictionary<ItemSO, int> items)
    {
        if (itemSlots == null)
            return false;
        int emptySlots = 0;
        foreach (var slot in itemSlots)
            if (slot != null && slot.itemSO == null)
                emptySlots++;

        var materials = new Dictionary<ItemSO, int>();
        foreach (var item in items)
        {
            if (item.Key == null || item.Value <= 0) return false;
            var key = item.Key.InventoryMaterial;
            materials[key] = (materials.ContainsKey(key) ? materials[key] : 0) + item.Value;
        }
        foreach (var item in materials)
        {
            if (item.Key == null || item.Value <= 0)
                return false;
            if (item.Key.isGold || item.Key.isEXP)
                continue;
            int stackSize = Mathf.Max(1, item.Key.stackSize);
            long remaining = item.Value;
            foreach (var slot in itemSlots)
                if (slot != null && slot.itemSO == item.Key)
                    remaining -= Mathf.Max(0, stackSize - slot.quantity);
            if (remaining > 0)
            {
                long needed = (remaining + stackSize - 1) / stackSize;
                if (needed > emptySlots)
                    return false;
                emptySlots -= (int)needed;
            }
        }
        return true;
    }

    private void Start()
    {
        foreach (InventorySlot slot in itemSlots)
        {
            if (slot != null)
                slot.UpdateUI();
        }
    }

    private void OnEnable()
    {
        if (Instance == this)
            Loot.OnItemLooted += AddItem;
    }

    private void OnDisable()
    {
        Loot.OnItemLooted -= AddItem;
    }

    public int AddItem(ItemSO itemSO, int quantity)
    {
        if (itemSO == null || quantity <= 0)
            return quantity;

        itemSO = itemSO.InventoryMaterial;

        if (itemSO.isGold)
        {
            gold += quantity;

            if (goldText != null)
                goldText.text = gold.ToString();

            return 0;
        }

        if (itemSO.isEXP)
        {
            OnExperienceGained?.Invoke(quantity);
            return 0;
        }

        int maxStack = Mathf.Max(1, itemSO.stackSize);

        // 先堆叠已有物品
        foreach (InventorySlot slot in itemSlots)
        {
            if (slot != null && slot.itemSO == itemSO &&
                slot.quantity < maxStack)
            {
                int space = maxStack - slot.quantity;
                int amountToAdd = Mathf.Min(space, quantity);

                slot.quantity += amountToAdd;
                quantity -= amountToAdd;

                slot.UpdateUI();

                if (quantity <= 0)
                    return 0;
            }
        }

        // 再放入空格
        foreach (InventorySlot slot in itemSlots)
        {
            if (slot == null || slot.itemSO != null)
                continue;

            int amountToAdd = Mathf.Min(maxStack, quantity);

            slot.itemSO = itemSO;
            slot.quantity = amountToAdd;
            quantity -= amountToAdd;

            slot.UpdateUI();

            if (quantity <= 0)
                return 0;
        }

        // 返回没有放进背包的数量
        return quantity;
    }
    public void DropLoot(InventorySlot slot)
    {
        if (slot == null ||
            slot.itemSO == null ||
            slot.quantity <= 0)
        {
            return;
        }

        if (!DropLoot(slot.itemSO, 1))
            return;

        slot.quantity--;

        if (slot.quantity <= 0)
            slot.itemSO = null;

        slot.UpdateUI();
    }

    private bool DropLoot(ItemSO itemSO, int quantity)
    {
        if (itemSO == null ||
            quantity <= 0 ||
            lootPrefab == null)
        {
            return false;
        }

        if (dropPoint == null)
        {
            PlayerMovement player = FindObjectOfType<PlayerMovement>();
            if (player == null)
                return false;
            dropPoint = player.transform;
        }

        float facing =
            dropPoint.localScale.x >= 0f ? 1f : -1f;

        Vector3 spawnPosition =
            dropPoint.position +
            new Vector3(-facing * dropDistance, 0f, 0f);

        Loot loot = Instantiate(
            lootPrefab,
            spawnPosition,
            Quaternion.identity
        );

        loot.Initialize(itemSO, quantity);
        return true;
    }
    public void UseItem(InventorySlot slot)
    {
        if (slot == null ||
            slot.itemSO == null ||
            slot.quantity <= 0)
        {
            return ;
        }

        if (useItem == null)
            useItem = GetComponent<Useltem>();
        if (useItem == null || !useItem.TryApplyItemEffects(slot.itemSO))
            return;

        slot.quantity--;

        if (slot.quantity <= 0)
            slot.itemSO = null;

        slot.UpdateUI();
    }
    public int GetItemQuantity(ItemSO itemSO) 
    {
        if (itemSO == null) return 0;
        itemSO = itemSO.InventoryMaterial;
        int total = 0;

        foreach (var slot in itemSlots)
        {
            if (slot != null && slot.itemSO == itemSO)
            {
                total += slot.quantity;
            }
        }

        return total;
    }
}
