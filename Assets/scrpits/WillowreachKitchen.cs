using UnityEngine;

// A short skill challenge earns a single-use cargo seal. Failures never consume ingredients.
public sealed class WillowreachKitchen : MonoBehaviour
{
    public WillowreachWorld world;
    public WillowreachNode[] counters;
    public ItemSO pumpkin,mushroom;
    public int successes;
    public bool cooking;
    public float started, target, retryAt;
    private WillowreachNode active;
    public float Elapsed=>Time.time-started;
    public string Hint => cooking?(Elapsed>=target?"<color=#235932>火候正好！现在按 F5 装盒</color>":"<color=#704319>正在加热，等待绿色提示…</color>")+"\n"+Elapsed.ToString("0.0")+"秒 / 装盒 "+target.ToString("0.0")+"～"+(target+.8f).ToString("0.0")+"秒":"F5 帮厨：南瓜1+蘑菇1 → 护货封蜡（成功才扣料）";
    public bool Press(WillowreachNode node)
    {
        var inv=InventortyManager.Instance;var delivery=world.GetComponent<WillowreachAdventure>();
        if(cooking)
        {
            if(node!=active)return false;
            float elapsed=Elapsed;cooking=false;retryAt=Time.time+2;
            if(elapsed<target||elapsed>target+.8f){world.Notify("火候没掌握好，本次未消耗食材。两秒后可再试。");return false;}
            if(inv.GetItemQuantity(pumpkin)<1||inv.GetItemQuantity(mushroom)<1){world.Notify("食材不足，本次未扣除物品。");return false;}
            if(delivery.cargoSeals>=3){world.Notify("已有3层封蜡，先用于运货再来配餐。未消耗物品。");return false;}
            Consume(pumpkin);Consume(mushroom);successes++;delivery.cargoSeals++;
            world.Notify("配餐成功！消耗南瓜1、蘑菇1，获得护货封蜡1层。自动抵挡一次货物损坏。");return true;
        }
        if(successes>=3){world.Notify("今日三份配餐已完成，感谢帮厨！");return false;}
        if(delivery.cargoSeals>=3){world.Notify("封蜡已满3层，先用于运货。");return false;}
        if(Time.time<retryAt)return false;
        if(inv.GetItemQuantity(pumpkin)<1||inv.GetItemQuantity(mushroom)<1){world.Notify("需要南瓜1和蘑菇1；只有成功装盒才会消耗。");return false;}
        active=node;cooking=true;started=Time.time;target=Random.Range(14,24)*.1f;
        world.Notify("观察右下提示：在 "+target.ToString("0.0")+"～"+(target+.8f).ToString("0.0")+"秒按 F5 装盒；提前、超时都不扣食材。");return true;
    }
    private static void Consume(ItemSO item)
    {
        foreach(var slot in InventortyManager.Instance.itemSlots)if(slot.itemSO==item&&slot.quantity>0)
        {slot.quantity--;if(slot.quantity==0)slot.itemSO=null;slot.UpdateUI();return;}
    }
    private void Update()
    {
        if(Time.timeScale<=0||BackpackUI.BlocksOtherInput||ShopKeeper.currentShopKeeper!=null)return;
        var node=world.Nearest;
        if(cooking&&(node!=active||Elapsed>target+.8f))
        {cooking=false;retryAt=Time.time+2;world.Notify("离开窗口或装盒超时，本次未消耗食材。");}
        if(node!=null&&System.Array.IndexOf(counters,node)>=0&&Input.GetKeyDown(KeyCode.F5))Press(node);
    }
}
