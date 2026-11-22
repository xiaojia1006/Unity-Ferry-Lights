using System;
using System.Collections.Generic;
using UnityEngine;

public partial class InventortyManager
{
    // The original five HUD slots are shared with the backpack, not copied.
    public void EnsureBackpackCapacity(int capacity)
    {
        if (itemSlots == null) itemSlots = new InventorySlot[0];
        int previous = itemSlots.Length;
        if (previous >= capacity) return;
        Array.Resize(ref itemSlots, capacity);
        for (int i = previous; i < capacity; i++)
        {
            var storage = new GameObject("Backpack Storage " + (i + 1));
            storage.transform.SetParent(transform, false);
            itemSlots[i] = storage.AddComponent<InventorySlot>();
        }
    }

    public bool MoveItem(int source, int target, bool split)
    {
        if (source == target || source < 0 || target < 0 ||
            source >= itemSlots.Length || target >= itemSlots.Length) return false;
        var from = itemSlots[source]; var to = itemSlots[target];
        if (from == null || to == null || from.itemSO == null || from.quantity <= 0) return false;
        int amount = split ? (from.quantity + 1) / 2 : from.quantity;
        if (to.itemSO == null || to.itemSO == from.itemSO)
        {
            amount = Mathf.Min(amount, Mathf.Max(1, from.itemSO.stackSize) - to.quantity);
            if (amount <= 0) return false;
            to.itemSO = from.itemSO; to.quantity += amount; from.quantity -= amount;
        }
        else
        {
            if (split) return false;
            ItemSO item = to.itemSO; int quantity = to.quantity;
            to.itemSO = from.itemSO; to.quantity = from.quantity;
            from.itemSO = item; from.quantity = quantity;
        }
        from.UpdateUI(); to.UpdateUI();
        return true;
    }

    public void SortBackpack()
    {
        // Keep quick-access assignments intact while consolidating storage.
        var items = new List<KeyValuePair<ItemSO, int>>();
        for (int i = 5; i < itemSlots.Length; i++)
        {
            var slot = itemSlots[i];
            if (slot != null && slot.itemSO != null && slot.quantity > 0)
                items.Add(new KeyValuePair<ItemSO, int>(slot.itemSO, slot.quantity));
        }
        items.Sort((a,b) => string.CompareOrdinal(a.Key.itemName,b.Key.itemName));
        for (int i = 5; i < itemSlots.Length; i++)
            if (itemSlots[i] != null) { itemSlots[i].itemSO = null; itemSlots[i].quantity = 0; }
        foreach (var entry in items)
        {
            int remaining = entry.Value;
            for (int i = 5; i < itemSlots.Length && remaining > 0; i++)
            {
                var slot = itemSlots[i];
                if (slot == null || (slot.itemSO != null && slot.itemSO != entry.Key)) continue;
                int amount = Mathf.Min(remaining, Mathf.Max(1, entry.Key.stackSize) - slot.quantity);
                if (amount <= 0) continue;
                slot.itemSO = entry.Key; slot.quantity += amount; remaining -= amount;
            }
        }
        foreach (var slot in itemSlots) if (slot != null) slot.UpdateUI();
    }
}
