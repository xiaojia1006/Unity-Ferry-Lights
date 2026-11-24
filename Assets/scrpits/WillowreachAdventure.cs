using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Scene-local optional activities. No item assets or original-scene progression are changed.
public sealed class WillowreachAdventure : MonoBehaviour
{
    public WillowreachWorld world;
    public WillowreachNode dispatch;
    public WillowreachNode[] destinations, runes;
    public ItemSO gold, mushroom;
    public int deliveries, target=-1, integrity;
    public int cargoSeals;
    public float deadline, retryAt;
    public bool carrying, ready, puzzleSolved;
    public int[] sequence;
    public int entered;
    public float showingUntil;
    private float wetTime;
    private int lastHealth, previousTarget=-1;
    private int revealed=-1;
    public string Journal => carrying ? "运货："+destinations[target].title+" · "+Mathf.CeilToInt(Mathf.Max(0,deadline-Time.time))+"秒 · 完好"+integrity+"/3" : ready ? "运货：奖励待领，回委托牌按 E" : "村庄委托 "+deliveries+"/3 · 符石 "+(puzzleSolved?"已解开":"待解谜");

    public bool Interact(WillowreachNode node)
    {
        if(node==dispatch)return Dispatch();
        int index=System.Array.IndexOf(destinations,node);
        if(index>=0)return Deliver(index);
        index=System.Array.IndexOf(runes,node);
        return index>=0&&TouchRune(index);
    }
    public bool Dispatch()
    {
        if(ready)return PayDelivery();
        if(carrying){world.Notify("运往"+destinations[target].title+"；桥上安全，涉水和受伤会损坏货物。Home 会取消本单。");return false;}
        if(deliveries>=3){world.Notify("三单委托已完成，商队补给全部送达！");return false;}
        if(Time.time<retryAt){world.Notify("货物正在重新打包，稍候再接单。");return false;}
        var choices=Enumerable.Range(0,destinations.Length).Where(i=>i!=previousTarget).ToArray();
        target=choices[Random.Range(0,choices.Length)];previousTarget=target;integrity=3;wetTime=0;
        carrying=true;deadline=Time.time+150;lastHealth=StatsManager.Instance.currentHealth;
        world.Notify("接单：150秒内送往"+destinations[target].title+"。完好度3；涉水每2秒、每次受伤各减1。无押金。");return true;
    }
    public void Tick(float dt)
    {
        if(!carrying)return;
        int health=StatsManager.Instance.currentHealth;
        if(health<lastHealth)DamageCargo();lastHealth=health;
        Vector2 p=world.player.position;
        bool bridge=world.bridges.Any(b=>b.Contains(p))||(world.shortcutBridge!=null&&world.shortcutBridge.activeInHierarchy&&world.shortcutBridgeArea.Contains(p));
        bool wet=!bridge&&world.river.GetComponent<PolygonCollider2D>().OverlapPoint(p);
        wetTime=wet?wetTime+dt:0;
        while(wetTime>=2){wetTime-=2;DamageCargo();}
        // Home rescue must not become a free delivery teleport.
        if(Input.GetKeyDown(KeyCode.Home)){Cancel("救援返乡，当前委托已取消，可重新接单。");return;}
        if(health<=0||integrity<=0||Time.time>=deadline)Cancel("本单失败：货物损坏或超时。没有扣除物品，稍候可重试。");
    }
    private void DamageCargo()
    {
        if(cargoSeals>0){cargoSeals--;world.Notify("护货封蜡抵挡了一次损坏！剩余"+cargoSeals+"层。");}
        else integrity--;
    }
    public void Cancel(string message) { carrying=false;retryAt=Time.time+10;world.Notify(message); }
    public bool Deliver(int index)
    {
        if(!carrying||index!=target){world.Notify("这里是收货点。先到村口委托牌接单，再送往指定目的地。");return false;}
        Tick(0);if(!carrying)return false;
        carrying=false;ready=true;world.Notify("货物已送达！回村委托牌领取金币与蘑菇；完好货物有额外奖金。");return true;
    }
    public bool PayDelivery()
    {
        if(!ready)return false;
        var inv=InventortyManager.Instance;
        var reward=new Dictionary<ItemSO,int>{{gold,integrity==3?24:16},{mushroom,2}};
        if(!inv.CanAddItems(reward)){world.Notify("背包不足，委托奖励保留。腾出空间后再来领取。");return false;}
        foreach(var pair in reward)inv.AddItem(pair.Key,pair.Value);PlayerStats.AddQuest("Willowreach.delivery."+deliveries);
        ready=false;deliveries++;world.Notify("完成委托 "+deliveries+"/3：金币"+(integrity==3?24:16)+"、蘑菇2。");return true;
    }
    public void BeginPuzzle()
    {
        if(sequence==null||sequence.Length!=4)
        {sequence=new int[4];sequence[0]=Random.Range(0,3);for(int i=1;i<4;i++)sequence[i]=(sequence[i-1]+Random.Range(1,3))%3;}
        entered=0;showingUntil=Time.time+6;revealed=-1;
        world.Notify("观察四次闪光，记住符石顺序。随后按相同顺序靠近符石按 E；F4 可重新观察。");
    }
    public bool TouchRune(int index)
    {
        if(puzzleSolved){world.Notify("符石已经共鸣，宝藏已领取。");return false;}
        if(sequence==null||sequence.Length!=4){BeginPuzzle();return false;}
        if(Time.time<showingUntil){world.Notify("先观察闪光顺序，等演示结束后再触碰。");return false;}
        if(entered<sequence.Length)
        {
            if(index!=sequence[entered]){entered=0;world.Notify("共鸣中断。进度重置，F4 可重新观察；没有消耗物品。");return false;}
            entered++;world.Notify("符石共鸣 "+entered+"/4。");
        }
        if(entered<sequence.Length)return true;
        var inv=InventortyManager.Instance;
        if(!inv.CanAddItems(new Dictionary<ItemSO,int>{{gold,25},{mushroom,3}}))
        {world.Notify("谜题已解开，背包满。腾出空间后触碰任意符石领奖。");return false;}
        inv.AddItem(gold,25);inv.AddItem(mushroom,3);puzzleSolved=true;PlayerStats.AddQuest("Willowreach.runes");
        world.Notify("四音共鸣！获得金币25、蘑菇3。此宝藏只能领取一次。");return true;
    }
    private void Update()
    {
        if(Time.timeScale<=0||BackpackUI.BlocksOtherInput||ShopKeeper.currentShopKeeper!=null)return;
        Tick(Time.deltaTime);
        bool near=runes.Any(n=>Vector2.Distance(n.transform.position,world.player.position)<4);
        if(!puzzleSolved&&near&&Input.GetKeyDown(KeyCode.F4))BeginPuzzle();
        int flash=sequence!=null&&sequence.Length==4&&Time.time<showingUntil?Mathf.FloorToInt((Time.time-(showingUntil-6))/1.5f):-1;
        bool lit=flash>=0&&flash<4&&(Time.time-(showingUntil-6))%1.5f<.9f;
        for(int i=0;i<runes.Length;i++)runes[i].indicator.color=puzzleSolved?new Color(.55f,1,.75f):lit&&sequence[flash]==i?new Color(1,.88f,.32f):Color.white;
        if(near&&flash!=revealed&&flash>=0&&flash<4){revealed=flash;world.Notify("第"+(flash+1)+"声：符石"+(sequence[flash]+1));}
    }
}
