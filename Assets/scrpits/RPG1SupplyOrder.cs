using UnityEngine;
public sealed class RPG1SupplyOrder : MonoBehaviour
{
    public RPG1Frontier world;
    public ItemSO bone;
    private static bool paid;
    private PlayerMovement player;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]private static void ResetSession(){paid=false;}
    private void Start(){player=FindObjectOfType<PlayerMovement>();}
    public bool Complete()
    {
        var inv=InventortyManager.Instance;if(paid||inv==null||bone==null||inv.GetItemQuantity(bone)<3)return false;
        int remaining=3;foreach(var slot in inv.itemSlots)if(slot!=null&&slot.itemSO!=null&&slot.itemSO.InventoryMaterial==bone.InventoryMaterial&&remaining>0){int n=Mathf.Min(remaining,slot.quantity);slot.quantity-=n;remaining-=n;if(slot.quantity==0)slot.itemSO=null;slot.UpdateUI();}
        paid=true;inv.AddItem(world.hud.gold,10);world.hud.Notify("骨料交付完成！获得10金币。每次游戏仅结算一次。");return true;
    }
    private void Update()
    {
        if(paid||player==null||Time.timeScale<=0||BackpackUI.BlocksOtherInput||ShopKeeper.currentShopKeeper!=null||RPG1Activities.BlocksMenuInput||Vector2.Distance(player.transform.position,transform.position)>1.4f)return;
        world.hud.Notify("F 交付3份骨料 → 10金币 · 骨料可在林间搜集");if(Input.GetKeyDown(KeyCode.F)&&!Complete())world.hud.Notify("骨料不足3份，没有扣除物品。");
    }
}
