using UnityEngine;

public class ShopButtonToggles : MonoBehaviour
{
    public void OpneItemShop()
    {
        if (ShopKeeper.currentShopKeeper != null)
            ShopKeeper.currentShopKeeper.OpenItemShop();
    }

    public void OpneWeaponsShop()
    {
        if (ShopKeeper.currentShopKeeper != null)
            ShopKeeper.currentShopKeeper.OpenWeaponShop();
    }

    public void OpneArmourShop()
    {
        if (ShopKeeper.currentShopKeeper != null)
            ShopKeeper.currentShopKeeper.OpenArmourShop();
    }

    // Correctly spelled aliases for future button bindings.
    public void OpenItemShop()
    {
        OpneItemShop();
    }

    public void OpenWeaponsShop()
    {
        OpneWeaponsShop();
    }

    public void OpenArmourShop()
    {
        OpneArmourShop();
    }
}
