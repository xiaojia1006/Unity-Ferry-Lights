using UnityEngine;
using System;
using System.Collections.Generic;

public class ShopKeeper : MonoBehaviour
{
    public static ShopKeeper currentShopKeeper;

    // A static keeper reference must never outlive the scene that owns it.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics(){ currentShopKeeper = null; }

    public Animator anim;
    public bool PlayerInRange;
    public ShopManager shopManager;
    public CanvasGroup shopCanvasGroup;
    public bool isShopOpen;

    [SerializeField] private List<ShopItem> shopItems;
    [SerializeField] private List<ShopItem> shopWeapons;
    [SerializeField] private List<ShopItem> shopArmour;

    [SerializeField] private Camera shopkeeperCam;
    [SerializeField] private Vector3 camerOffset = new Vector3(0,0,-1);
    public Transform portraitTarget;

    public static event Action<ShopManager, bool> OnShopStateChanged;

    private int handledFrame=-1;
    private bool nearFallback;
    private PlayerMovement nearbyPlayer;

    // Triggers are the only interaction in the project that depends on a physics overlap event;
    // after a scene round trip the player can be standing inside the ring with no Enter callback,
    // so distance is used as the same fallback every other interaction already uses.
    private bool InReachDistance()
    {
        if (nearbyPlayer == null || !nearbyPlayer.isActiveAndEnabled)
            nearbyPlayer = FindObjectOfType<PlayerMovement>();
        return nearbyPlayer != null &&
               Vector2.Distance(nearbyPlayer.transform.position, transform.position) < 1.9f;
    }

    private void Update()
    {
        nearFallback = !isShopOpen && InReachDistance();
        if (BackpackUI.BlocksOtherInput || (!PlayerInRange && !nearFallback && !isShopOpen))
            return;

        // E is the game-wide interaction key; K stays as the original shop key.
        if (!Input.GetButtonDown("Interact") && !Input.GetKeyDown(KeyCode.E))
            return;

        // Something else (a door or map node) already opened/closed it on this same E press.
        if (handledFrame == Time.frameCount)
            return;

        if (!isShopOpen)
        {
            OpenShop();
            if (isShopOpen && shopkeeperCam != null)
            {
                shopkeeperCam.transform.position = (portraitTarget!=null?portraitTarget.position:transform.position) + camerOffset;
                shopkeeperCam.gameObject.SetActive(true);
            }
        }
        else
        {
            CloseShop();
            if (shopkeeperCam != null)
                shopkeeperCam.gameObject.SetActive(false);
        }
    }

    private void OpenShop()
    {
        if (shopManager == null || shopCanvasGroup == null)
            return;

        MenuPause.SetPaused(this, true);
        isShopOpen = true;
        handledFrame = Time.frameCount;
        currentShopKeeper = this;

        OpenItemShop();

        shopCanvasGroup.alpha = 1f;
        shopCanvasGroup.blocksRaycasts = true;
        shopCanvasGroup.interactable = true;

        OnShopStateChanged?.Invoke(shopManager, true);
    }

    // Door interactions reuse the same inventory, pause ownership and portrait camera.
    public bool TryOpenFromBuilding()
    {
        if (BackpackUI.BlocksOtherInput || (currentShopKeeper != null && currentShopKeeper != this)) return false;
        // The same E press must not reopen what the keeper just closed this frame.
        if (handledFrame == Time.frameCount) return isShopOpen;
        if (!isShopOpen) OpenShop();
        if (isShopOpen && shopkeeperCam != null)
        {
            shopkeeperCam.transform.position = (portraitTarget!=null?portraitTarget.position:transform.position) + camerOffset;
            shopkeeperCam.gameObject.SetActive(true);
        }
        return isShopOpen;
    }

    private void CloseShop()
    {
        MenuPause.SetPaused(this, false);
        isShopOpen = false;
        handledFrame = Time.frameCount;

        if (currentShopKeeper == this)
            currentShopKeeper = null;

        OnShopStateChanged?.Invoke(shopManager, false);

        if (shopCanvasGroup != null)
        {
            shopCanvasGroup.alpha = 0f;
            shopCanvasGroup.blocksRaycasts = false;
            shopCanvasGroup.interactable = false;
        }
        if (shopkeeperCam != null)
            shopkeeperCam.gameObject.SetActive(false);
    }

    public void OpenItemShop()
    {
        if (shopManager != null)
            shopManager.PopulateShopItems(shopItems);
    }

    public void OpenWeaponShop()
    {
        if (shopManager != null)
            shopManager.PopulateShopItems(shopWeapons);
    }

    public void OpenArmourShop()
    {
        if (shopManager != null)
            shopManager.PopulateShopItems(shopArmour);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        PlayerInRange = true;
        if (anim != null && anim.isActiveAndEnabled && anim.runtimeAnimatorController != null)
            anim.SetBool("PlayerInRange", true);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        PlayerInRange = false;
        if (anim != null && anim.isActiveAndEnabled && anim.runtimeAnimatorController != null)
            anim.SetBool("PlayerInRange", false);
    }

    private void OnDisable()
    {
        if (isShopOpen)
            CloseShop();
        else if (currentShopKeeper == this)
            currentShopKeeper = null;
    }
}
