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
    private Rect bridgeWalk;   // 运行时按桥贴图算出的通行范围
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
    private ItemSO timberItem;
    private ItemSO TimberItem => timberItem!=null?timberItem:(timberItem=Resources.Load<ItemSO>("Items/timber"));
    /// <summary>修桥进度直接读背包里的木料数量，材料有了实体就不能只记计数器。</summary>
    public int Timber => InventortyManager.Instance!=null&&TimberItem!=null?InventortyManager.Instance.GetItemQuantity(TimberItem):0;
    public int BeaconCount {get; private set;}
    public bool TreasureClaimed {get; private set;}
    public bool SheepRescued {get; private set;}
    public int Defeated => guards.Count(g=>g==null || g.Health<=0);
    public bool MenuOpen => menu!=null&&menu.activeSelf;
    public bool Busy => MenuOpen||BackpackUI.BlocksOtherInput||ShopKeeper.currentShopKeeper!=null||Time.timeScale<=0;
    public int NearestIndex {get; private set;}=-1;
    private bool sheepFollows;
    private float nextRest,fishReady,fishEnd,fishCooldown;
    private bool biteCue;
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
        RebuildBridgeWalkArea();
        if(bridge!=null&&bridge.GetComponent<BridgeRails2D>()==null)bridge.AddComponent<BridgeRails2D>();
        BuildUI();
        var boundary=gameObject.scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Confiner");
        foreach(var camera in FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())
        {
            camera.Follow=Player.transform;camera.m_Lens.OrthographicSize=6.7f;
            var confiner=camera.GetComponent<Cinemachine.CinemachineConfiner>();
            if(confiner!=null&&boundary!=null){confiner.m_BoundingShape2D=boundary.GetComponent<Collider2D>();confiner.InvalidatePathCache();}
        }
        RestoreOrSnapFishingSpot();
        Notify("欢迎来到苇岸渡口。E 互动 · R 委托 · Tab 地图。先去营火旁的告示牌看看。");
    }

    private void Update()
    {
        TrackDay();
        if (GlobalSettings.BlocksInput) return;
        if(Debug.isDebugBuild&&Input.GetKeyDown(KeyCode.F11))PlaceFishingHere();
        if(Debug.isDebugBuild&&Input.GetKeyDown(KeyCode.F12))DiagnoseFishing();
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
        bool onBridge=BridgeRepaired&&bridgeWalk.Contains(Player.transform.position);
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
        if(fishing==null)FishingRig2D.End();
        if(fishing!=null)
        {
            if(Vector2.Distance(Player.transform.position,fishing.transform.position)>4.0f){fishing=null;Notify("离开钓点，鱼线收回；没有消耗物品。");}
            else if(Time.time>fishEnd){fishing=null;Notify(deepCast&&reelTaps>0?"线一松，鱼就脱钩了。下次连按别停。":"鱼儿游走了。等一会儿再试。");}
            else if(Time.time>=fishReady&&!biteCue){biteCue=true;Sfx.Bite();toastUntil=0f;}
            FishingRig2D.SetBite(Time.time>=fishReady&&Time.time<=fishEnd);
            if(fishing!=null&&Input.GetKeyDown(KeyCode.E))ResolveFishing();
            return;
        }
        if(NearestIndex>=0&&Input.GetKeyDown(KeyCode.E))Interact(points[NearestIndex]);
        else if(NearestIndex<0&&Input.GetKeyDown(KeyCode.E))HintNearestFishing();
    }

    private Vector3 fishingCast;
    private bool deepCast;
    private int reelTaps;
    private ItemSO silverFish;
    private ItemSO SilverFish => silverFish!=null?silverFish:(silverFish=Resources.Load<ItemSO>("Items/silver_fish"));

    // 每日记录
    private const string JournalKey="Rpg.Journal";
    private int dayIndex, fishToday, questsToday, goldAtDayStart;
    private DayNight2D.Phase lastPhase=DayNight2D.Phase.Dawn;

    // 浮标落点：以"最后一次移动方向"为基准甩出去，再按需微调角度找水面
    public bool TryFindFishingCast(Vector2 from,Vector2 facing,out Vector3 cast)
    {
        cast=Vector3.zero;
        if(waters.Count==0)waters.AddRange(FindObjectsOfType<WaterSurface2D>());
        if(facing.sqrMagnitude<.01f)facing=Vector2.right;
        float angle=Mathf.Atan2(facing.y,facing.x)*Mathf.Rad2Deg;
        // Prefer the facing direction, then search both sides, including the river behind the player.
        // Failure has no fallback on land.
        for(int step=0;step<=36;step++)
        {
            float offset=step==0?0:((step+1)/2)*10*(step%2==1?1:-1);
            float rad=(angle+offset)*Mathf.Deg2Rad;Vector2 direction=new Vector2(Mathf.Cos(rad),Mathf.Sin(rad));
            for(float distance=4.8f;distance>=1.4f;distance-=.2f)
            {
                Vector2 candidate=from+direction*distance;
                if(!ValidFishingWater(candidate))continue;
                cast=new Vector3(candidate.x,candidate.y,0);return true;
            }
        }
        return false;
    }
    // 场景里桥被移动/放大后，序列化的 bridgeArea 就对不上了（人在桥上仍被判成在水里，
    // 于是被水减速、走不动）。这里直接用桥贴图的包围盒，再留一点余量。
    private void RebuildBridgeWalkArea()
    {
        bridgeWalk = bridgeArea;
        if (bridge == null) return;
        // 桥可能由多段贴图拼成，取所有子渲染器的合并包围盒
        var renderers = bridge.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers == null || renderers.Length == 0) return;
        bool found = false;
        Bounds merged = new Bounds();
        foreach (var r in renderers)
        {
            if (r == null || r.sprite == null) continue;
            if (!found) { merged = r.bounds; found = true; }
            else merged.Encapsulate(r.bounds);
        }
        if (!found || merged.size.x < .1f || merged.size.y < .1f) return;
        bridgeWalk = new Rect(merged.min.x - .15f, merged.min.y - .15f, merged.size.x + .3f, merged.size.y + .3f);
        Debug.Log("[桥] 通行范围 " + bridgeWalk);
    }

    public bool ValidFishingWater(Vector2 point)
    {
        if(bridge!=null&&bridge.activeInHierarchy&&bridgeWalk.Contains(point))return false;
        // A small margin keeps the bobber's idle motion away from the shore.
        return InWater(point)&&InWater(point+Vector2.up*.16f)&&InWater(point+Vector2.down*.16f)
            &&InWater(point+Vector2.left*.16f)&&InWater(point+Vector2.right*.16f);
    }

    private bool InWater(Vector2 p)
    {
        foreach(var w in waters)
        {
            if(w==null||!w.isActiveAndEnabled)continue;
            var poly=w.GetComponent<PolygonCollider2D>();
            if(poly!=null&&poly.enabled&&poly.OverlapPoint(p))return true;
        }
        return false;
    }

    // 优先用碰撞体求最近的水面点，再往里推进一点；找不到才退回环形搜索
    private Vector3 NearestWater(Vector3 from)
    {
        var waters=FindObjectsOfType<WaterSurface2D>();
        Vector2 best=from;float bestDist=float.MaxValue;
        if(waters!=null)
            foreach(var w in waters)
            {
                if(w==null)continue;
                var col=w.GetComponent<Collider2D>();
                if(col==null)continue;
                Vector2 closest=col.ClosestPoint(from);
                float d=Vector2.Distance(from,closest);
                if(d<bestDist){bestDist=d;best=closest;}
            }

        if(bestDist<float.MaxValue)
        {
            if(InWater(best))return new Vector3(best.x,best.y,from.z);
            // 边界点往往正好压在岸线上，向水面内侧推 0.9 米
            for(int k=0;k<12;k++)
            {
                float ang=k*Mathf.PI*2f/12f;
                Vector2 cand=best+new Vector2(Mathf.Cos(ang),Mathf.Sin(ang))*.9f;
                if(InWater(cand))return new Vector3(cand.x,cand.y,from.z);
            }
            return new Vector3(best.x,best.y,from.z);
        }

        for(float r=.6f;r<=7f;r+=.4f)
            for(int a=0;a<24;a++)
            {
                float ang=a*Mathf.PI*2f/24f;
                Vector2 cand=new Vector2(from.x+Mathf.Cos(ang)*r,from.y+Mathf.Sin(ang)*r);
                if(InWater(cand))return new Vector3(cand.x,cand.y,from.z);
            }
        return from;
    }

    // F12：把钓点的真实状态打出来，方便定位"浮标为什么不在水里"
    private void DiagnoseFishing()
    {
        var spot=FishingPoint();
        if(spot==null){Notify("场景里没有钓鱼点");return;}
        Vector3 p=spot.transform.position;
        var waters=FindObjectsOfType<WaterSurface2D>();
        float nearest=float.MaxValue;
        foreach(var w in waters)
        {
            if(w==null)continue;
            var col=w.GetComponent<Collider2D>();
            if(col!=null)nearest=Mathf.Min(nearest,Vector2.Distance(p,col.ClosestPoint(p)));
        }
        Notify("钓点 "+p.x.ToString("0.0")+","+p.y.ToString("0.0")+" · 在水里="+(InWater(p)?"是":"否")
              +" · 水面组件="+((waters!=null)?waters.Length:0)
              +" · 到水边="+(nearest==float.MaxValue?"无":nearest.ToString("0.0")+"m"));
    }

    private const string FishingSpotKey="Rpg.FishingSpot";

    private RPG2Point FishingPoint()
    {
        foreach(var p in points) if(p!=null&&p.kind==RPG2Point.Kind.Fishing) return p;
        return null;
    }

    // A saved spot wins, then the nearest waterline, so the point never starts in the river.
    private void RestoreOrSnapFishingSpot()
    {
        var spot=FishingPoint();if(spot==null)return;
        string saved=PlayerPrefs.GetString(FishingSpotKey,string.Empty);
        if(!string.IsNullOrEmpty(saved))
        {
            var parts=saved.Split(',');
            if(parts.Length==2&&float.TryParse(parts[0],out float x)&&float.TryParse(parts[1],out float y))
            {
                // 钓点就是"站的位置"，浮标由抛竿方向决定，所以这里原样恢复即可
                spot.transform.position=new Vector3(x,y,spot.transform.position.z);
                return;
            }
        }
        SnapFishingToBank();
    }

    /// <summary>Stand where the rod should point and press F11: the spot moves to your feet and
    /// remembers itself, so nobody has to hunt for coordinates in the scene.</summary>
    public void PlaceFishingHere()
    {
        var spot=FishingPoint();if(spot==null||Player==null)return;
        Vector3 feet=Player.transform.position;
        // 玩家站在岸上，钓点必须落到水里，否则鱼线、浮标全挤在脚下
        Vector3 at=NearestWater(feet);
        spot.transform.position=new Vector3(at.x,at.y,spot.transform.position.z);
        PlayerPrefs.SetString(FishingSpotKey,at.x.ToString("0.###")+","+at.y.ToString("0.###"));
        Debug.Log("[钓鱼] 钓点设于 "+at);
        PlayerPrefs.Save();
        Notify("钓点已移到你脚下（"+at.x.ToString("0.0")+" , "+at.y.ToString("0.0")+"），已保存。");
    }

    // The authored fishing point sits out in the river. Walk outwards until land is found, then
    // step back to the waterline, so the spot ends up on the bank where the player can stand.
    private void SnapFishingToBank()
    {
        var waters = FindObjectsOfType<WaterSurface2D>();
        if (waters == null || waters.Length == 0) return;

        bool Wet(Vector2 p)
        {
            foreach (var w in waters)
            {
                if (w == null) continue;
                var poly = w.GetComponent<PolygonCollider2D>();
                if (poly != null && poly.OverlapPoint(p)) return true;
            }
            return false;
        }

        foreach (var point in points)
        {
            if (point == null || point.kind != RPG2Point.Kind.Fishing) continue;
            Vector2 origin = point.transform.position;
            if (!Wet(origin)) continue;                       // already on dry land

            Vector2 best = origin;
            float bestGap = float.MaxValue;
            for (float radius = .6f; radius <= 9f; radius += .8f)
            {
                for (int a = 0; a < 24; a++)
                {
                    float angle = a * Mathf.PI * 2f / 24f;
                    Vector2 candidate = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    if (Wet(candidate)) continue;

                    // How far is the water from here? Aim for the waterline, not the middle of a field.
                    float gap = float.MaxValue;
                    for (int k = 0; k < 8; k++)
                    {
                        float ka = k * Mathf.PI * 2f / 8f;
                        for (float step = .25f; step <= 1.5f; step += .35f)
                        {
                            Vector2 probe = candidate + new Vector2(Mathf.Cos(ka), Mathf.Sin(ka)) * step;
                            if (Wet(probe)) { gap = Mathf.Min(gap, step); break; }
                        }
                    }
                    if (gap < bestGap) { bestGap = gap; best = candidate; }
                }
            }
            if (bestGap < float.MaxValue) point.transform.position = best;
        }
    }

    // Ripples and a float on every fishing spot, so the player can see where to cast.
    private void SpawnFishingMarkers()
    {
        // World sprites are depth-sorted by foot Y (orders in the thousands), so the marker has to
        // sit above that band the same way the rain particles do, or it hides under the path tiles.
        int layer = 0, order = 24000;
        var water = FindObjectOfType<WaterSurface2D>();
        if (water != null)
        {
            var sr = water.GetComponent<SpriteRenderer>();
            if (sr != null) layer = sr.sortingLayerID;
        }
        foreach (var p in points)
            if (p != null && p.kind == RPG2Point.Kind.Fishing)
                FishingMarker2D.Create(p.transform, layer, order);
    }

    public int Nearest(Vector2 at)
    {
        int index=-1;float best=float.MaxValue;
        for(int i=0;i<points.Length;i++)
        {
            var p=points[i];if(p==null||!p.gameObject.activeInHierarchy||collected.Contains(p))continue;
            // The fishing spot sits out in the water, so it needs a longer reach from the bank.
            float reach=p.kind==RPG2Point.Kind.Fishing?3.4f:1.85f;
            float d=Vector2.Distance(at,p.transform.position);
            if(d>reach)continue;
            // Ground pickups and bank-side spots can never hide behind a wall at arm's length: a
            // pumpkin sitting on the grass was blocked by its own terrain collider, and a fishing
            // spot on the bank was blocked by the water surface collider in front of it.
            bool openSpot=p.kind==RPG2Point.Kind.Mushroom||p.kind==RPG2Point.Kind.Pumpkin||p.kind==RPG2Point.Kind.Timber
                       ||p.kind==RPG2Point.Kind.Fishing||p.kind==RPG2Point.Kind.Bridge;
            bool clear=openSpot||!Physics2D.LinecastAll(at,p.transform.position,1<<7).Any(h=>h.collider!=null&&!h.collider.isTrigger&&h.collider.GetComponentInParent<RPG2Point>()!=p);
            if(d<best&&clear){best=d;index=i;}
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
        if(quest==1&&BridgeRepaired)return "木料已交付 · 渡桥已修复";
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
    private void RewardQuest(int quest){foreach(var r in QuestRewards(quest))InventortyManager.Instance.AddItem(r.Key,r.Value);questsToday++;PlayerStats.AddQuest("RPG2.quest."+quest);}
    public string Hint(RPG2Point p)
    {
        if(p.kind==RPG2Point.Kind.Camp)return !SuppliesDelivered&&accepted[0]?"E 交付热汤材料 / 休息":"E 营火休息 / 熬汤 · 恢复12生命";
        if(p.kind==RPG2Point.Kind.Beacon)return "E 点亮"+new[]{"水边","林地","遗迹"}[p.number]+"信标";
        return "E "+p.title;
    }
    public void Interact(RPG2Point p)
    {
        if(p==null||Busy)return;
        if(p.kind==RPG2Point.Kind.Board){OpenMenu(false);return;}
        if(p.kind==RPG2Point.Kind.Camp)
        {
            bool pending=accepted[0]&&!SuppliesDelivered;
            if(pending&&CanRewardQuest(0)&&Consume(new Dictionary<ItemSO,int>{{mushroom,3},{pumpkin,2}}))
            {SuppliesDelivered=true;RewardQuest(0);Notify("热汤材料已交付！"+QuestReward(0));return;}
            // 委托未交付时不动食材，避免把任务材料熬成汤
            if(TryCook(pending))return;
            if(pending)Notify("交付需要3蘑菇、2南瓜，并留出奖励空间。R 查看委托。先休息一下也行。");
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
            if(!collected.Add(p))return;
            accepted[1]=true;
            var tinv=InventortyManager.Instance;
            if(tinv!=null&&TimberItem!=null)
            {
                if(!tinv.CanAddItems(new Dictionary<ItemSO,int>{{TimberItem,1}})){collected.Remove(p);Notify("背包已满，先整理再来搬木料。");return;}
                tinv.AddItem(TimberItem,1);
            }
            p.GetComponent<SpriteRenderer>().enabled=false;
            Notify("收进背包：一捆木料（"+Timber+" / 3）");return;
        }
        if(p.kind==RPG2Point.Kind.Bridge)
        {
            if(BridgeRepaired){Notify("渡桥已经修好，两岸畅通。");return;}
            if(Timber<3){Notify("修复渡桥需要三捆木料，当前 "+Timber+" / 3。");return;}
            if(!CanRewardQuest(1))return;
            if(TimberItem!=null&&!Consume(new Dictionary<ItemSO,int>{{TimberItem,3}})){Notify("木料不足。");return;}
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
            TreasureClaimed=true;RewardQuest(3);p.GetComponent<SpriteRenderer>().color=new Color(.65f,.7f,.6f);Notify("本章完成！找到渡口宝藏！"+QuestReward(3)+" · 可继续探索或前往柳湾");return;
        }
        if(p.kind==RPG2Point.Kind.Fishing)
        {
            if(Time.time<fishCooldown){Notify("水面还没平静，稍等片刻再抛竿。");return;}
            if(!TryFindFishingCast(Player.transform.position,Player.lastMoveDir,out fishingCast))
            {Notify("附近没有可以抛竿的河面，请靠近河岸再试。");return;}
            if(Mathf.Abs(fishingCast.x-Player.transform.position.x)>.4f)Player.FaceTowards(fishingCast.x);
            fishing=p;fishReady=Time.time+Random.Range(1.5f,2.8f);fishEnd=fishReady+1.6f+PlayerStats.FishingWindowBonus;fishCooldown=Time.time+2.5f;biteCue=false;toastUntil=0f;
            deepCast=Vector2.Distance(Player.transform.position,fishingCast)>3.2f;
            reelTaps=0;
            FishingRig2D.Begin(Player.transform,fishingCast);
            Notify(deepCast?"抛向深水：咬钩后要连按 3 次 E 收线，慢一次就断线。":"已抛竿：等底部提示变为「鱼儿咬钩」再按 E。");
            toastUntil=Time.unscaledTime+(deepCast?2.4f:1.4f);return;
        }
        if(p.kind==RPG2Point.Kind.Sheep)
        {
            if(SheepRescued){Notify("小羊已经安全回家。");return;}
            sheepFollows=!sheepFollows;Notify(sheepFollows?"小羊跟着你走了，沿南岸带回营火；靠近再按 E 可让它停下。":"小羊停下来吃草了。");return;
        }
        if(p.kind==RPG2Point.Kind.Inn){Rest();return;}
        Notify(p.description);
    }
    private void TrackDay()
    {
        var dn=DayNight2D.Instance;if(dn==null)return;
        var phase=dn.Current;
        if(phase==lastPhase)return;
        if(phase==DayNight2D.Phase.Dawn)   // 天亮即新的一天
        {
            dayIndex++;
            WriteJournal();
            fishToday=0;questsToday=0;
            goldAtDayStart=InventortyManager.Instance!=null?InventortyManager.Instance.gold:0;
        }
        lastPhase=phase;
    }

    private void WriteJournal()
    {
        var weather=FindObjectOfType<WeatherCycle2D>();
        string sky=weather==null?"晴":weather.currentWeather==WeatherCycle2D.Weather.Rain?"雨"
                                 :weather.currentWeather==WeatherCycle2D.Weather.Snow?"雪":"晴";
        int gold=InventortyManager.Instance!=null?InventortyManager.Instance.gold:0;
        int diff=gold-goldAtDayStart;
        string line="第 "+dayIndex+" 天 · "+sky+" · 钓到 "+fishToday+" 条鱼 · 完成 "+questsToday+" 份委托 · 金币 "+(diff>=0?"+":"")+diff;
        string raw=PlayerPrefs.GetString(JournalKey,string.Empty);
        var list=new List<string>(raw.Length>0?raw.Split('\n'):new string[0]);
        list.Add(line);while(list.Count>7)list.RemoveAt(0);
        PlayerPrefs.SetString(JournalKey,string.Join("\n",list));PlayerPrefs.Save();
    }

    private string LastJournal()
    {
        string raw=PlayerPrefs.GetString(JournalKey,string.Empty);
        if(raw.Length==0)return string.Empty;
        var parts=raw.Split('\n');
        return parts[parts.Length-1];
    }

    private void Rest()
    {
        if(Time.time<nextRest){Notify("休息冷却中："+Mathf.CeilToInt(nextRest-Time.time)+"秒。");return;}
        if(guards.Any(g=>g!=null&&g.Health>0&&Vector2.Distance(g.transform.position,Player.transform.position)<5)){Notify("附近有守卫，先到安全处休息。");return;}
        if(StatsManager.Instance!=null){StatsManager.Instance.UpdateHealth(12);nextRest=Time.time+30;
            string entry=LastJournal();
            Notify(entry.Length>0?"恢复最多12生命。｜"+entry:"恢复最多12生命。营火与旅舍共用30秒冷却。");}
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
    private ItemSO fish, mushroomSoup, pumpkinSoup;
    private ItemSO Fish => fish!=null?fish:(fish=Resources.Load<ItemSO>("Items/fish"));

    // Two of the same ingredient become one stronger meal. Only offered outside a pending delivery
    // so quest materials are never cooked by accident.
    private bool TryCook(bool pendingQuest)
    {
        var inv=InventortyManager.Instance;if(inv==null)return false;
        if(mushroomSoup==null)mushroomSoup=Resources.Load<ItemSO>("Items/mushroom_soup");
        if(pumpkinSoup==null)pumpkinSoup=Resources.Load<ItemSO>("Items/pumpkin_soup");
        var cost=new Dictionary<ItemSO,int>();

        // 委托还没交付时，先给厨师留够材料（3 蘑菇 / 2 南瓜），剩下的才能拿来熬汤
        int reserveMush=pendingQuest?3:0;
        int reservePump=pendingQuest?2:0;
        int mushQty=inv.GetItemQuantity(mushroom);
        int pumpQty=inv.GetItemQuantity(pumpkin);

        if(mushroomSoup!=null&&mushQty-2>=reserveMush&&inv.CanAddItems(new Dictionary<ItemSO,int>{{mushroomSoup,1}}))
        {cost[mushroom]=2;if(Consume(cost)){inv.AddItem(mushroomSoup,1);PlayerStats.AddCook();Notify("熬了一碗蘑菇汤：回血 12。");return true;}}
        if(pumpkinSoup!=null&&pumpQty-2>=reservePump&&inv.CanAddItems(new Dictionary<ItemSO,int>{{pumpkinSoup,1}}))
        {cost.Clear();cost[pumpkin]=2;if(Consume(cost)){inv.AddItem(pumpkinSoup,1);PlayerStats.AddCook();Notify("熬了一碗南瓜汤：回血 14。");return true;}}

        // 材料够两份但被委托预留了，给一句明确解释
        if(pendingQuest&&(mushQty>=2||pumpQty>=2))
            Notify("这些食材要先留给厨师的热汤（3 蘑菇 2 南瓜）。交付后再来熬汤。");
        return false;
    }

    // Standing just outside the fishing radius: say so instead of staying silent.
    private void HintNearestFishing()
    {
        RPG2Point best=null;float bestD=3.2f;
        foreach(var p in points)
        {
            if(p==null||p.kind!=RPG2Point.Kind.Fishing||!p.gameObject.activeInHierarchy)continue;
            float d=Vector2.Distance(Player.transform.position,p.transform.position);
            if(d<bestD){bestD=d;best=p;}
        }
        if(best!=null)Notify("再靠近钓点一点（"+(bestD).ToString("0.0")+" 米）就能抛竿。");
    }

    private void ResolveFishing()
    {
        if(fishing==null)return;
        if(!ValidFishingWater(fishingCast)){fishing=null;FishingRig2D.End();Notify("浮标已离开河面，本次收竿，不消耗物品。");return;}
        Vector3 spot=fishingCast;
        bool inWindow=Time.time>=fishReady&&Time.time<=fishEnd;
        if(!inWindow){fishing=null;Notify("收竿太早了，下次等鱼儿咬钩。");return;}

        // 深水区：咬钩后要连按三次，每次都必须赶在上一拍的一秒内
        if(deepCast)
        {
            reelTaps++;
            if(reelTaps<3){fishEnd=Time.time+.9f;biteCue=true;Sfx.Bite();Notify("收线 "+reelTaps+" / 3 —— 继续按 E！");return;}
        }

        fishing=null;
        FishingRig2D.Splash(spot);
        var inv=InventortyManager.Instance;
        var catchItem=deepCast&&SilverFish!=null?SilverFish:(Fish!=null?Fish:meat);
        if(inv==null||!inv.CanAddItems(new Dictionary<ItemSO,int>{{catchItem,1}})){Notify("背包已满，这次把鱼儿放回河中。");return;}
        inv.AddItem(catchItem,1);
        fishToday++;PlayerStats.AddFish();
        Notify(catchItem==SilverFish?"收线上岸：一条银鳞鱼！鳞片上还挂着水。"
              :catchItem==meat?"钓到了食材！获得 1 份肉，可以用于恢复。"
              :"钓到一条鱼！可以生吃，也可以之后熬汤。");
    }
    private void UpdateSheep()
    {
        if(sheep==null)return;bool moving=false;
        if(sheepFollows&&!Busy)
        {
            Vector2 at=sheep.position,target=Player.transform.position;
            if(Vector2.Distance(at,spawn)<2.8f){SheepRescued=true;sheepFollows=false;Reward(14,0);PlayerStats.AddQuest("RPG2.sheep");Notify("小羊回到了营地！获得14金币。");}
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
    private void OnDisable(){FishingRig2D.End();CloseMenu();}
}

