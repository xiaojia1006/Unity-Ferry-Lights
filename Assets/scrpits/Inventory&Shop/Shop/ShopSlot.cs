using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShopSlot : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerMoveHandler
{
    public ItemSO itemSO;
    public TMP_Text itemNameText;
    public TMP_Text priceText;
    public Image itemImage;

    [SerializeField] private ShopManager shopManager;
    [SerializeField] private ShopInfo shopInfo;

    public int price;

    private void Awake()
    {
        Transform icon = transform.Find("ItemIcon");

        if (icon != null)
            itemImage = icon.GetComponent<Image>();

        if (shopInfo == null)
            shopInfo = FindObjectOfType<ShopInfo>();

        if (shopManager == null)
            shopManager = FindObjectOfType<ShopManager>();
    }

    public void Initialize(ItemSO newItemSO, int newPrice)
    {
        itemSO = newItemSO;
        price = newPrice;

        if (itemSO == null)
        {
            itemImage.gameObject.SetActive(false);
            itemNameText.text = "";
            priceText.text = "";
            return;
        }

        itemImage.sprite = itemSO.icon;
        itemImage.gameObject.SetActive(true);
        itemNameText.text = itemSO.itemName;
        priceText.text = price.ToString();
    }

    public void OnBuyButtonClicked()
    {
        shopManager.TryBuyItem(itemSO, price);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemSO != null && shopInfo != null)
        {
            shopInfo.ShowItemInfo(itemSO);
            shopInfo.FollowMouse(itemSO);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (shopInfo != null)
            shopInfo.HideItemInfo();
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (itemSO != null && shopInfo != null)
            shopInfo.FollowMouse(itemSO);
    }
}