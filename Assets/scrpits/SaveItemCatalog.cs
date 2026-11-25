using UnityEngine;
public sealed class SaveItemCatalog : ScriptableObject
{
    public Loot lootPrefab;
    public QuestSo[] quests;
    public string[] questIds;
    public string[] ids;
    public ItemSO[] items;
    public string Id(ItemSO item){if(item==null)return "";for(int i=0;i<items.Length;i++)if(items[i]==item.InventoryMaterial)return ids[i];return "";}
    public ItemSO Find(string id){for(int i=0;i<ids.Length;i++)if(ids[i]==id)return items[i];return null;}
}
