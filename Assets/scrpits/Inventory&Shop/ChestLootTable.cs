using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "RPG/Chest Loot Table", fileName = "Chest Loot Table")]
public class ChestLootTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public ItemSO item;
        [Min(0)] public int weight = 1;
        [Min(1)] public int minQuantity = 1;
        [Min(1)] public int maxQuantity = 3;
    }

    public struct Drop
    {
        public ItemSO item;
        public int quantity;
    }

    [Range(1, 10)] public int minRolls = 1;
    [Range(1, 10)] public int maxRolls = 2;
    public Entry[] entries;

    // Draws may select the same item; merge those quantities into one world drop.
    public List<Drop> Roll(System.Random random)
    {
        var result = new List<Drop>();
        long totalWeight = 0;
        if (entries == null) return result;
        foreach (var entry in entries)
            if (entry != null && entry.item != null && entry.weight > 0)
                totalWeight += entry.weight;
        if (totalWeight == 0) return result;

        int minimum = Mathf.Clamp(minRolls, 1, 10);
        int count = random.Next(minimum, Mathf.Clamp(maxRolls, minimum, 10) + 1);
        for (int i = 0; i < count; i++)
        {
            double ticket = random.NextDouble() * totalWeight;
            foreach (var entry in entries)
            {
                if (entry == null || entry.item == null || entry.weight <= 0) continue;
                ticket -= entry.weight;
                if (ticket >= 0) continue;
                int min = Mathf.Clamp(entry.minQuantity, 1, int.MaxValue / 10);
                int max = Mathf.Clamp(entry.maxQuantity, min, int.MaxValue / 10);
                int quantity = random.Next(min, max + 1);
                int index = result.FindIndex(drop => drop.item == entry.item);
                if (index < 0) result.Add(new Drop { item = entry.item, quantity = quantity });
                else
                {
                    var drop = result[index];
                    drop.quantity += quantity;
                    result[index] = drop;
                }
                break;
            }
        }
        return result;
    }
}
