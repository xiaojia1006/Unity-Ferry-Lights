using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class InventorySlot : MonoBehaviour, IPointerClickHandler
{
    public ItemSO itemSO;
    public int quantity;

    public Image itemImage;
    public TMP_Text quantityText;

    private InventortyManager inventortyManager;
    private static ShopManager activeShop;
    private void Start()
    {
        inventortyManager =
            GetComponentInParent<InventortyManager>();
    }

    private void OnEnable()
    {
        ShopKeeper.OnShopStateChanged += HandleShopStateChanged;
    }
    private void OnDisable()
    {
        ShopKeeper.OnShopStateChanged -= HandleShopStateChanged;
    }
    private void HandleShopStateChanged(ShopManager shopManager, bool isOpen)
    {
        activeShop = isOpen ? shopManager : null;
    }





    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventortyManager == null)
            inventortyManager = GetComponentInParent<InventortyManager>();
        if (inventortyManager == null)
            inventortyManager = InventortyManager.Instance;

        if (quantity <= 0 ||
            itemSO == null ||
            inventortyManager == null)
        {
            return;
        }

        if (eventData.button ==
            PointerEventData.InputButton.Left)
        {
            bool isHealOnlyItem =
                itemSO.currentHealth > 0 &&
                itemSO.maxHealth <= 0 &&
                itemSO.speed <= 0 &&
                itemSO.damage <= 0;
            if (activeShop != null)
            {
                if (!activeShop.SellItem(itemSO))
                    return;
                quantity--;
                UpdateUI();
            }
            else
            {




                if (isHealOnlyItem &&
                    StatsManager.Instance != null &&
                    StatsManager.Instance.currentHealth >=
                    StatsManager.Instance.maxHealth)
                {
                    return;
                }

                inventortyManager.UseItem(this);
            }
        }
        else if (eventData.button ==
                 PointerEventData.InputButton.Right)
        {
            inventortyManager.DropLoot(this);
        }
    }

    public void UpdateUI()
    {
        if (quantity <= 0)
        {
            quantity = 0;
            itemSO = null;
        }

        if (itemSO != null)
        {
            if (itemImage != null)
            {
                itemImage.sprite = itemSO.icon;
                itemImage.gameObject.SetActive(true);
            }
            if (quantityText != null) quantityText.text = quantity.ToString();
        }
        else
        {
            if (itemImage != null) itemImage.gameObject.SetActive(false);
            if (quantityText != null) quantityText.text = "";
        }
    }
}
