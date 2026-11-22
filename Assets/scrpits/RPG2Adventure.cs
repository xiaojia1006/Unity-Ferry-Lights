using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

// Scene-local adventure state; original terrain, NPCs and exits remain authored objects.
public sealed partial class RPG2Adventure : MonoBehaviour
{
    public RPG2Point[] points;
    public RPG2Guard[] guards;
    public GameObject bridge;
    public Rect bridgeArea=new Rect(-1,-4,2,10);
    public Sprite parchment,ribbon,button,chart;
    public Sprite questScroll,questPaper,questRibbon,rewardSlot;
    public Sprite[] rewardDisplayIcons;
    public Sprite RewardIcon(ItemSO item)
    {
        int index=item==gold?0:item==experience?1:item==meat?2:item==pumpkin?3:4;
        return rewardDisplayIcons!=null&&index<rewardDisplayIcons.Length&&rewardDisplayIcons[index]!=null?rewardDisplayIcons[index]:item.icon;
    }
    public TMPro.TMP_FontAsset font;
    public ItemSO mushroom,pumpkin,gold,experience,meat;
    public Sprite[] sheepIdle,sheepWalk;
    public Transform sheep;
    public Vector2 spawn=new Vector2(-2,-9);
    public Rect bounds=new Rect(-34,-19,64,39);
    public PlayerMovement Player {get; private set;}
    public bool SuppliesDelivered {get; private set;}
    public bool BridgeRepaired {get; private set;}
    public int Timber {get; private set;}
    public int BeaconCount {get; private set;}
    public bool TreasureClaimed {get; private set;}
    public bool SheepRescued {get; private set;}
    public int Defeated => guards.Count(g=>g==null || g.Health<=0);
    public bool MenuOpen => menu!=null&&menu.activeSelf;
    public bool Busy => MenuOpen||BackpackUI.BlocksOtherInput||ShopKeeper.currentShopKeeper!=null||Time.timeScale<=0;
    public int NearestIndex {get; private set;}=-1;
    private bool sheepFollows;
    private float nextRest,fishReady,fishEnd,fishCooldown;
    private RPG2Point fishing;
    private readonly bool[] accepted=new bool[4];
    private readonly HashSet<RPG2Point> collected=new HashSet<RPG2Point>();
    private int beaconSequence;
    private readonly int[] beaconOrder={0,2,1};
    private readonly List<WaterSurface2D> waters=new List<WaterSurface2D>();
    private bool wasOnBridge;

    private void Start()
    {
        Player=FindObjectOfType<PlayerMovement>();
        if(Player==null){enabled=false;return;}
        Player.rb.velocity=Vector2.zero;Player.rb.position=spawn;Player.transform.position=spawn;
        waters.AddRange(FindObjectsOfType<WaterSurface2D>());
        bridge.SetActive(false);
        BuildUI();
        var boundary=gameObject.scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Confiner");
        foreach(var camera in FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())
        {
            camera.Follow=Player.transform;camera.m_Lens.OrthographicSize=6.7f;
            var confiner=camera.GetComponent<Cinemachine.CinemachineConfiner>();
            if(confiner!=null&&boundary!=null){confiner.m_BoundingShape2D=boundary.GetComponent<Collider2D>();confiner.InvalidatePathCache();}
        }
        Notify("欢迎来到苇岸渡口。E 互动 · R 委托 · Tab 地图。先去营火旁的告示牌看看。");
    }

    private void Update()
    {
        if (GlobalSettings.BlocksInput) return;
        if(Player==null)return;
        if(!Player.gameObject.activeInHierarchy || StatsManager.Instance!=null&&StatsManager.Instance.currentHealth<=0)
        {
            CloseMenu();Player.gameObject.SetActive(true);Player.transform.position=spawn;Player.rb.position=spawn;Player.rb.velocity=Vector2.zero;
            if(StatsManager.Instance!=null){StatsManager.Instance.currentHealth=Mathf.Max(1,StatsManager.Instance.maxHealth/2);StatsManager.Instance.UpdateHealthText();}
            fishing=null;sheepFollows=false;Notify("在渡口营火旁醒来，保留已完成的委托和机关进度。");
        }
        bool blocked=BackpackUI.BlocksOtherInput||ShopKeeper.currentShopKeeper!=null;
        if(blocked&&MenuOpen)CloseMenu();
        if(!blocked&&(Time.timeScale>0||MenuOpen))
        {
            if(Input.GetKeyDown(KeyCode.R))ToggleMenu(false);
            if(Input.GetKeyDown(KeyCode.Tab))ToggleMenu(true);
            if(Input.GetKeyDown(KeyCode.E)&&MenuOpen){CloseMenu();return;}
        }
        NearestIndex=Busy?-1:Nearest(Player.transform.position);
        RefreshUI(blocked);
        bool onBridge=BridgeRepaired&&bridgeArea.Contains(Player.transform.position);
        foreach(var water in waters)
        {
            if(water==null)continue;
            if(onBridge)Player.SetWaterState(water,false,1);
            else if(wasOnBridge&&water.GetComponent<PolygonCollider2D>().OverlapPoint(Player.transform.position))Player.SetWaterState(water,true,water.swimSpeedMultiplier);
        }
        wasOnBridge=onBridge;
        UpdateSheep();
        if(Busy)return;
        NearestIndex=Nearest(Player.transform.position);
        if(fishing!=null)
        {
            if(Vector2.Distance(Player.transform.position,fishing.transform.position)>2.1f){fishing=null;Notify("离开钓点，鱼线收回；没有消耗物品。");}
            else if(Time.time>fishEnd){fishing=null;Notify("鱼儿游走了。等一会儿再试。");}
            else if(Input.GetKeyDown(KeyCode.E))ResolveFishing();
            return;
        }
        if(NearestIndex>=0&&Input.GetKeyDown(KeyCode.E))Interact(points[NearestIndex]);
    }

    public int Nearest(Vector2 at)
    {
        int index=-1;float best=1.85f;
        for(int i=0;i<points.Length;i++)
        {
            var p=points[i];if(p==null||!p.gameObject.activeInHierarchy||collected.Contains(p))continue;
            float d=Vector2.Distance(at,p.transform.position);
            if(d<best&&!Physics2D.LinecastAll(at,p.transform.position,1<<7).Any(h=>h.collider!=null&&!h.collider.isTrigger&&h.collider.GetComponentInParent<RPG2Point>()!=p)){best=d;index=i;}
        }
        return index;
    }
    public void Accept(int quest)
    {
        if(quest<0||quest>=accepted.Length)return;
        accepted[quest]=true;Notify("已记录委托："+QuestTitle(quest));RefreshQuest();
    }
    public bool Accepted(int quest)=>accepted[quest];
    public bool QuestDone(int quest)=>quest==0?SuppliesDelivered:quest==1?BridgeRepaired:quest==2?BeaconCount==3:TreasureClaimed;
    public string QuestTitle(int quest)=>new[]{"01  渡口的热汤","02  重连两岸","03  苇岸的三盏灯","04  遗迹守卫的宝藏"}[quest];
    public string QuestDescription(int quest)
    {
        if(quest==0)return "营地厨师想给修桥工人熬一锅热汤。到南岸林地采集蘑菇和南瓜，再带回营火交付。\n交付会消耗材料。奖励只领取一次。";
        if(quest==1)return "洪水冲散了渡桥。南岸遗落三捆可用木料，寻找木料后在断桥南端修复。\n渡桥修好后可以直接过河，不再受涉水减速。";
        if(quest==2)return "北岸三座信标需要按旧渡口暗号点亮：\n水边 → 遗迹 → 林地。\n顺序错误会重置灯光，可以立即重试。渡桥修复后才能启动信标。";
        return "四名守卫正在北岸巡逻，并会在各自区域追击。躲开红色攻击预警，逐个击败他们。\n点亮三座信标并清除四名守卫后，在西北遗迹开启宝藏。";
    }
    public string QuestProgress(int quest)
    {
        var inv=InventortyManager.Instance;
        if(quest==0)return SuppliesDelivered?"已交付热汤材料":"蘑菇  "+(inv!=null?inv.GetItemQuantity(mushroom):0)+" / 3\n南瓜  "+(inv!=null?inv.GetItemQuantity(pumpkin):0)+" / 2\n返回营地营火按 E 交付";
        if(quest==1)return "收集木料  "+Timber+" / 3\n"+(BridgeRepaired?"渡桥已修复":"在断桥南端按 E 修复");
        if(quest==2)return "点亮信标  "+BeaconCount+" / 3\n"+(BridgeRepaired?"暗号：水边 → 遗迹 → 林地":"先修复渡桥");
        return "击败守卫  "+Defeated+" / 4\n点亮信标  "+BeaconCount+" / 3\n"+(TreasureClaimed?"宝藏已领取":"到西北遗迹按 E 开启");
    }
    public Dictionary<ItemSO,int> QuestRewards(int quest)
    {
        if(quest==0)return new Dictionary<ItemSO,int>{{gold,18},{experience,4},{meat,1}};
        if(quest==1)return new Dictionary<ItemSO,int>{{gold,12},{pumpkin,2}};
        if(quest==2)return new Dictionary<ItemSO,int>{{gold,16},{experience,4},{mushroom,2}};
        return new Dictionary<ItemSO,int>{{gold,32},{experience,8},{meat,2}};
    }
    public string QuestReward(int quest)=>string.Join(" · ",QuestRewards(quest).Select(r=>(r.Key.isGold?"金币":r.Key.isEXP?"经验":r.Key==meat?"肉":r.Key==pumpkin?"南瓜":"蘑菇")+" ×"+r.Value));
    private bool CanRewardQuest(int quest)
    {
        var inv=InventortyManager.Instance;
        if(inv!=null&&inv.CanAddItems(QuestRewards(quest)))return true;
        Notify("背包空间不足，请整理后再领取；任务进度已保留。");return false;
    }
    private void RewardQuest(int quest){foreach(var r in QuestRewards(quest))InventortyManager.Instance.AddItem(r.Key,r.Value);}
    public string Hint(RPG2Point p)
    {
        if(p.kind==RPG2Point.Kind.Camp)return !SuppliesDelivered&&accepted[0]?"E 交付热汤材料 / 休息":"E 营火休息 · 恢复12生命";
        if(p.kind==RPG2Point.Kind.Beacon)return "E 点亮"+new[]{"水边","林地","遗迹"}[p.number]+"信标";
        return "E "+p.title;
    }
    public void Interact(RPG2Point p)
    {
        if(p==null||Busy)return;
        if(p.kind==RPG2Point.Kind.Board){OpenMenu(false);return;}
        if(p.kind==RPG2Point.Kind.Camp)
        {
            if(accepted[0]&&!SuppliesDelivered)
            {
                if(CanRewardQuest(0)&&Consume(new Dictionary<ItemSO,int>{{mushroom,3},{pumpkin,2}}))
                {SuppliesDelivered=true;RewardQuest(0);Notify("热汤材料已交付！"+QuestReward(0));return;}
                Notify("交付需要3蘑菇、2南瓜，并留出奖励空间。R 查看委托。请凑齐材料后再来。");return;
            }
            Rest();return;
        }
        if(p.kind==RPG2Point.Kind.Mushroom||p.kind==RPG2Point.Kind.Pumpkin)
        {
            if(collected.Contains(p)){Notify("这里已经采过了，去别处找找。");return;}
            ItemSO item=p.kind==RPG2Point.Kind.Mushroom?mushroom:pumpkin;
            var inventory=InventortyManager.Instance;
            if(inventory==null||!inventory.CanAddItems(new Dictionary<ItemSO,int>{{item,1}})){Notify("背包已满，整理后再采集。");return;}
            inventory.AddItem(item,1);collected.Add(p);p.GetComponent<SpriteRenderer>().color=new Color(.6f,.6f,.6f,.22f);Notify("采集成功，已放入背包。");return;
        }
        if(p.kind==RPG2Point.Kind.Timber)
        {
            if(!collected.Add(p))return;Timber++;accepted[1]=true;p.GetComponent<SpriteRenderer>().enabled=false;Notify("找到修桥木料 "+Timber+" / 3");return;
        }
        if(p.kind==RPG2Point.Kind.Bridge)
        {
            if(BridgeRepaired){Notify("渡桥已经修好，两岸畅通。");return;}
            if(Timber<3){Notify("修复渡桥需要找到南岸三捆木料，当前 "+Timber+" / 3。");return;}
            if(!CanRewardQuest(1))return;
            BridgeRepaired=true;accepted[1]=true;bridge.SetActive(true);RewardQuest(1);Notify("渡桥修好了！"+QuestReward(1));return;
        }
        if(p.kind==RPG2Point.Kind.Beacon)
        {
            if(!BridgeRepaired){Notify("先修复渡桥，再启动北岸信标。");return;}
            if(BeaconCount==3){Notify("三座信标已经全部点亮。");return;}
            accepted[2]=true;
            if(p.number!=beaconOrder[beaconSequence])
            {
                beaconSequence=BeaconCount=0;
                foreach(var beacon in points.Where(n=>n.kind==RPG2Point.Kind.Beacon))beacon.SetLit(false);
                Notify("顺序不对，灯光熄灭。暗号是：水边 → 遗迹 → 林地。");return;
            }
            if(beaconSequence==2&&!CanRewardQuest(2))return;
            beaconSequence++;BeaconCount++;p.SetLit(true);
            if(BeaconCount==3){RewardQuest(2);Notify("三灯齐明！"+QuestReward(2));}
            else Notify("信标已点亮 "+BeaconCount+" / 3。继续寻找下一座。");
            return;
        }
        if(p.kind==RPG2Point.Kind.Treasure)
        {
            accepted[3]=true;
            if(TreasureClaimed){Notify("宝藏已经领取，去帮帮渡口的居民吧。");return;}
            if(BeaconCount<3||Defeated<4){Notify("遗迹仍被封锁：需要三座信标及清除四名巡逻守卫。");return;}
            if(!CanRewardQuest(3))return;
            TreasureClaimed=true;RewardQuest(3);p.GetComponent<SpriteRenderer>().color=new Color(.65f,.7f,.6f);Notify("找到渡口宝藏！"+QuestReward(3));return;
        }
        if(p.kind==RPG2Point.Kind.Fishing)
        {
            if(Time.time<fishCooldown){Notify("水面还没平静，稍等片刻再抛竿。");return;}
            fishing=p;fishReady=Time.time+Random.Range(1.5f,2.8f);fishEnd=fishReady+1.0f;fishCooldown=Time.time+5;Notify("已抛竿，等提示变为「鱼儿咬钩」再按 E。");return;
        }
        if(p.kind==RPG2Point.Kind.Sheep)
        {
            if(SheepRescued){Notify("小羊已经安全回家。");return;}
            sheepFollows=!sheepFollows;Notify(sheepFollows?"小羊跟着你走了，沿南岸带回营火；靠近再按 E 可让它停下。":"小羊停下来吃草了。");return;
        }
        if(p.kind==RPG2Point.Kind.Inn){Rest();return;}
        Notify(p.description);
    }
    private void Rest()
    {
        if(Time.time<nextRest){Notify("休息冷却中："+Mathf.CeilToInt(nextRest-Time.time)+"秒。");return;}
        if(guards.Any(g=>g!=null&&g.Health>0&&Vector2.Distance(g.transform.position,Player.transform.position)<5)){Notify("附近有守卫，先到安全处休息。");return;}
        if(StatsManager.Instance!=null){StatsManager.Instance.UpdateHealth(12);nextRest=Time.time+30;Notify("恢复最多12生命。营火与旅舍共用30秒冷却。");}
    }
    private bool Consume(Dictionary<ItemSO,int> cost)
    {
        var inv=InventortyManager.Instance;if(inv==null||cost.Any(e=>inv.GetItemQuantity(e.Key)<e.Value))return false;
        foreach(var entry in cost)
        {int left=entry.Value;foreach(var slot in inv.itemSlots){if(slot==null||slot.itemSO!=entry.Key.InventoryMaterial)continue;int take=Mathf.Min(left,slot.quantity);slot.quantity-=take;left-=take;if(slot.quantity==0)slot.itemSO=null;slot.UpdateUI();if(left==0)break;}}
        return true;
    }
    private void Reward(int coins,int xp)
    {var inv=InventortyManager.Instance;if(inv!=null){if(coins>0)inv.AddItem(gold,coins);if(xp>0)inv.AddItem(experience,xp);}}
    private void ResolveFishing()
    {
        bool success=Time.time>=fishReady&&Time.time<=fishEnd;fishing=null;
        if(!success){Notify("收竿太早了，下次等鱼儿咬钩。");return;}
        var inv=InventortyManager.Instance;
        if(inv==null||!inv.CanAddItems(new Dictionary<ItemSO,int>{{meat,1}})){Notify("背包已满，这次把鱼儿放回河中。");return;}
        inv.AddItem(meat,1);Notify("钓到了食材！获得1份肉，可以用于恢复。");
    }
    private void UpdateSheep()
    {
        if(sheep==null)return;bool moving=false;
        if(sheepFollows&&!Busy)
        {
            Vector2 at=sheep.position,target=Player.transform.position;
            if(Vector2.Distance(at,spawn)<2.8f){SheepRescued=true;sheepFollows=false;Reward(14,0);Notify("小羊回到了营地！获得14金币。");}
            else if(Vector2.Distance(at,target)>1.45f)
            {
                Vector2 next=Vector2.MoveTowards(at,target,Time.deltaTime*2.5f);
                bool wet=waters.Any(w=>w!=null&&w.GetComponent<PolygonCollider2D>().OverlapPoint(next));
                bool obstacle=Physics2D.OverlapCircleAll(next,.26f,1<<7).Any(c=>!c.isTrigger);
                if(!wet&&!obstacle){sheep.position=next;moving=true;}
            }
        }
        var frames=moving?sheepWalk:sheepIdle;if(frames!=null&&frames.Length>0)sheep.GetComponent<SpriteRenderer>().sprite=frames[(int)(Time.time*7)%frames.Length];
    }
    private void OnDisable(){CloseMenu();}
}

