using UnityEngine;

// Shared fallback for existing scene instances that predate loot-table fields.
public class ChestDefaults : ScriptableObject
{
    public Loot lootPrefab;
    public ChestLootTable lootTable;
}
