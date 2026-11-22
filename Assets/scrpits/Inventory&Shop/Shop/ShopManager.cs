
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    private void OnEnable()
    {
        // The large portrait has transparent pixels over the first product card.
        // It is decoration, so it must never intercept shop button raycasts.
        foreach (var graphic in GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
        {
            for (Transform current = graphic.transform; current != null && current != transform; current = current.parent)
            {
                if (current.name != "ShopKeeperPortait" && current.name != "ShopKeeperPortrait") continue;
                graphic.raycastTarget = false;
                break;
            }
        }
    }
   

 

    [SerializeField]
    private ShopSlot[] shopSlots;

    [SerializeField]
    private InventortyManager inventortyManager;


    public void PopulateShopItems(List<ShopItem>shopItems)
    {
        if (shopSlots == null)
            return;

        int count = Mathf.Min(
            shopItems.Count,
            shopSlots.Length
        );

        for (int i = 0; i < count; i++)
        {
            ShopItem shopItem = shopItems[i];

            shopSlots[i].gameObject.SetActive(true);
            shopSlots[i].Initialize(
                shopItem.itemSO,
                shopItem.price
            );
        }

        for (int i = count; i < shopSlots.Length; i++)
        {
            shopSlots[i].gameObject.SetActive(false);
        }
    }

    public void TryBuyItem(ItemSO itemSO, int price)
    {
        if (itemSO == null || inventortyManager == null)
            return;

        if (inventortyManager.gold < price)
            return;

        if (!HasSpaceForItem(itemSO))
            return;

        inventortyManager.gold -= price;

        if (inventortyManager.goldText != null)
        {
            inventortyManager.goldText.text =
                inventortyManager.gold.ToString();
        }

        inventortyManager.AddItem(itemSO, 1);
    }

    private bool HasSpaceForItem(ItemSO itemSO)
    {
        if (itemSO == null || inventortyManager == null)
            return false;

        int maxStack = Mathf.Max(1, itemSO.stackSize);

        foreach (InventorySlot slot in inventortyManager.itemSlots)
        {
            if (slot.itemSO == itemSO &&
                slot.quantity < maxStack)
            {
                return true;
            }

            if (slot.itemSO == null)
                return true;
        }

        return false;
    }

    public bool SellItem(ItemSO itemSO)
    {
        if (itemSO == null ||
            inventortyManager == null ||
            shopSlots == null)
        {
            return false;
        }

        foreach (ShopSlot slot in shopSlots)
        {
            if (slot == null)
                continue;

            if (slot.itemSO == itemSO)
            {
                inventortyManager.gold += slot.price;

                if (inventortyManager.goldText != null)
                {
                    inventortyManager.goldText.text =
                        inventortyManager.gold.ToString();
                }

                return true;
            }
        }

        return false;
    }
}

[System.Serializable]
public class ShopItem
{
    public ItemSO itemSO;
    public int price;
}
