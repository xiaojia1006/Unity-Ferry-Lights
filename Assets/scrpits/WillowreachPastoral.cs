using System.Linq;
using UnityEngine;

// Optional side quest: bones -> flute -> lead living sheep over dry bridges -> weather treasure.
public sealed class WillowreachPastoral : MonoBehaviour
{
    public WillowreachWorld world;
    public WillowreachSheep[] sheep;
    public WillowreachNode[] pastures;
    public ItemSO bone,gold;
    public bool fluteCrafted;
    private float nextCall;
    public int Found => pastures.Count(n=>n.completed);
    public string Journal => !fluteCrafted?"支线：骨头×3制作骨笛（村口营火旁）": "牧羊寻宝 "+Found+"/3 · R 呼唤/放牧";
    public bool Craft()
    {
        if(fluteCrafted){world.Notify("骨笛已经制作。靠近羊按 R 呼唤，再按一次放牧。");return false;}
        var inv=InventortyManager.Instance;
        if(inv==null||inv.GetItemQuantity(bone)<3){world.Notify("需要3根骨头：遗迹地面的骨头现在可以走近拾取。");return false;}
        int remaining=3;
        foreach(var slot in inv.itemSlots)
        {
            if(slot.itemSO!=bone)continue;
            int used=Mathf.Min(remaining,slot.quantity);slot.quantity-=used;remaining-=used;
            if(slot.quantity==0)slot.itemSO=null;slot.UpdateUI();if(remaining==0)break;
        }
        fluteCrafted=true;world.Notify("骨笛制作完成！靠近羊按 R 呼唤/放牧。带羊到地图上的三处草场寻宝。");return true;
    }
    private void Update()
    {
        if(Time.timeScale<=0||BackpackUI.BlocksOtherInput||ShopKeeper.currentShopKeeper!=null||!Input.GetKeyDown(KeyCode.R))return;
        Call();
    }
    public bool Call()
    {
        if(!fluteCrafted){world.Notify("先到村口营火旁，用3根骨头制作骨笛。");return false;}
        if(Time.time<nextCall)return false;
        nextCall=Time.time+1.5f;
        var nearby=sheep.Where(s=>s!=null&&Vector2.Distance(s.transform.position,world.player.position)<12).ToArray();
        if(nearby.Length==0){world.Notify("羊群太远了，靠近到12单位内再吹笛。");return false;}
        bool follow=!nearby.Any(s=>s.following);
        foreach(var s in nearby)s.Follow(follow);
        world.Notify(follow?"羊群正在跟随。它们会寻找桥梁过河，请放慢脚步等候。":"羊群恢复自由放牧；可再次按 R 呼唤。");return true;
    }
    public bool Claim(WillowreachNode pasture)
    {
        if(pasture.completed)return false;
        if(!fluteCrafted){world.Notify("先制作骨笛，再带羊寻找草场宝藏。");return false;}
        if(world.weather.currentWeather!=pasture.requiredWeather){world.Notify("草场需要"+WillowreachWorld.WeatherName(pasture.requiredWeather)+"；在标记旁用 F1/F2/F3 调谐。");return false;}
        var companions=sheep.Where(s=>s!=null&&Vector2.Distance(s.transform.position,pasture.transform.position)<3.2f).ToArray();
        if(companions.Length==0){world.Notify("带至少一只羊靠近草场，羊会闻出埋藏的补给。");return false;}
        var inv=InventortyManager.Instance;if(inv==null)return false;
        inv.AddItem(gold,12);pasture.completed=true;PlayerStats.AddQuest("Willowreach.pasture."+pasture.name);
        if(pasture.indicator!=null)pasture.indicator.color=new Color(.65f,1,.75f);
        foreach(var s in companions)s.Graze(5);
        if(Found==pastures.Length){inv.AddItem(gold,15);world.Notify("三处草场宝藏全部找到！本处金币12，另获牧羊完成奖励15金币。");}
        else world.Notify("羊群找到了埋藏的补给！金币12 · 草场 "+Found+"/3。");
        return true;
    }
}
