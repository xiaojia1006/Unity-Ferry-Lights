using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New Item")]
public class ItemSO : ScriptableObject
{
    public string itemName;

    [TextArea]
    public string itemDescription;

    public Sprite icon;
    public bool isGold;

    public bool isEXP;
    public int stackSize = 2;
    [Tooltip("Legacy variants can share one inventory material without losing existing drops.")]
    public ItemSO inventoryMaterial;
    public ItemSO InventoryMaterial => inventoryMaterial != null ? inventoryMaterial : this;


    [Header("Stats")]
    [FormerlySerializedAs("currenHealth")]
    public int currentHealth;

    public int maxHealth;
    public int speed;
    public int damage;

    [Header("For Temporary Items")]
    public float duration;
}
