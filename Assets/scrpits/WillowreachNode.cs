using UnityEngine;

// All state belongs to the new scene, never to shared item assets.
public sealed class WillowreachNode : MonoBehaviour
{
    public enum Kind { Supply, Survey, Camp, Shortcut, Shop, Guide, Craft, Pasture, Adventure }
    public Kind kind;
    public string title;
    [TextArea] public string hint;
    public ItemSO item;
    public int quantity = 1;
    public WeatherCycle2D.Weather requiredWeather;
    public bool completed;
    public GameObject unlock;
    public GameObject activate;
    public SpriteRenderer indicator;
    public float range = 2.1f;
    public ShopKeeper shop;
    public int requiredObservations;
    public bool Reusable => kind == Kind.Camp || kind == Kind.Shop || kind == Kind.Guide || kind == Kind.Craft || kind == Kind.Adventure;

    public bool TryInteract(WillowreachWorld world)
    {
        if (completed && !Reusable) { world.Notify("这里已经完成了，继续探索其他区域吧。"); return false; }
        if (kind == Kind.Craft) return world.GetComponent<WillowreachPastoral>().Craft();
        if (kind == Kind.Adventure) return world.GetComponent<WillowreachAdventure>().Interact(this);
        if (kind == Kind.Pasture) return world.GetComponent<WillowreachPastoral>().Claim(this);
        if (world.SurveyCount < requiredObservations) { world.Notify("需要先完成 " + requiredObservations + " 处天气观测，再来领取此处的补给。"); return false; }
        if (kind == Kind.Guide) { world.Notify(hint); world.OpenFieldMap(); return true; }
        if (kind == Kind.Shop) return shop != null && shop.TryOpenFromBuilding();
        var inv = InventortyManager.Instance;
        if (inv == null) return false;
        if (kind == Kind.Survey && world.weather.currentWeather != requiredWeather)
        { world.Notify("需要" + WillowreachWorld.WeatherName(requiredWeather) + "。靠近观测站按 F1晴 / F2雨 / F3雪 调谐。"); return false; }
        if (kind == Kind.Shortcut && world.SurveyCount < 3)
        { world.Notify("完成三处天气观测后，才能打开返乡捷径。"); return false; }
        int amount = quantity;
        if (kind == Kind.Supply && world.weather.currentWeather == WeatherCycle2D.Weather.Rain && item != null && item.name == "Mushroom") amount++;
        if (item != null && !inv.CanAddItems(new System.Collections.Generic.Dictionary<ItemSO,int>{{item,amount}}))
        { world.Notify("背包空间不足，奖励仍保留在这里。"); return false; }
        // Capacity is checked before any state change. Full backpacks cannot consume a node.
        if (item != null) inv.AddItem(item, amount);
        if (kind == Kind.Camp)
        {
            var stats = StatsManager.Instance;
            if (Time.time < world.nextRest) { world.Notify("营火尚未恢复，稍后再来（30秒）。"); return false; }
            if (stats != null) stats.UpdateHealth(Mathf.Min(10, stats.maxHealth - stats.currentHealth));
            world.nextRest = Time.time + 30;
            world.Notify("营火休息：恢复最多 10 点生命。"); return true;
        }
        completed = true;
        if (unlock != null) unlock.SetActive(false);
        if (activate != null) activate.SetActive(true);
        if (indicator != null)
        {
            if (kind == Kind.Supply) indicator.enabled = false;
            else indicator.color = new Color(.55f,.76f,.66f);
        }
        world.Notify(kind == Kind.Survey ? "观测完成！获得补给，三处观测后可开启遗迹返乡捷径。" : kind == Kind.Shortcut ? "返乡捷径已开启！" : "获得 " + (item != null ? item.itemName : "补给") + " ×" + amount);
        return true;
    }
}
