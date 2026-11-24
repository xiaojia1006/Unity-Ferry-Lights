using System.Linq;
using UnityEngine;
public sealed class RPG1Frontier : MonoBehaviour
{
    public RPG1Activities hud;
    public RPG1PushCrate crate;
    public Transform plate;
    public GameObject gate;
    public RPG1Sentinel[] arena;
    public TreasureChest finalChest;
    public Vector2 spawn=new Vector2(-23,-14);
    public Rect safeCamp=new Rect(-30,-19,15,12),arenaRect=new Rect(-11,10,22,10);
    private static bool west,east,won,paid;
    private float nextRest,charge;
    private int charging=-1;
    private PlayerMovement player;
    public bool Powered => west&&east;
    public bool Completed => won;
    public bool PlatePressed => crate!=null&&plate!=null&&Vector2.Distance(crate.transform.position,plate.position)<.55f;
    public string Journal => (west?"西供能 ✓":"西供能：清理守卫并启动")+"\n"+(east?"东供能 ✓":"东供能：推箱压板后启动")+"\n"+(won?(paid?"工事已解放 · 自由探索":"挑战完成 · 返回营地结算"):Powered?"北部大门已开 · 启动工事挑战":"双供能开启北部工事");
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset(){west=east=won=paid=false;}
    private void Start()
    {
        player=FindObjectOfType<PlayerMovement>();ApplyState();
        if(player!=null)
        {
            // Re-entering from RPG2 uses the persistent player/camera, not the scene duplicates.
            player.transform.position=spawn;player.rb.position=spawn;player.rb.velocity=Vector2.zero;
            var boundary=gameObject.scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Confiner");
            foreach(var camera in FindObjectsOfType<Cinemachine.CinemachineVirtualCamera>())
            {camera.Follow=player.transform;camera.m_Lens.OrthographicSize=6;var confiner=camera.GetComponent<Cinemachine.CinemachineConfiner>();if(confiner!=null&&boundary!=null){confiner.m_BoundingShape2D=boundary.GetComponent<Collider2D>();confiner.InvalidatePathCache();}}
        }
        if(finalChest!=null)finalChest.gameObject.SetActive(won);
        if(won)foreach(var guard in arena)if(guard!=null)guard.gameObject.SetActive(false);
        hud.Notify("欢迎来到风鸣伐木场！E互动，推动木箱供能；Tab地图。营地可休息。");
    }
    private void Update()
    {
        if(player==null)return;
        // Recover the player before evaluating victory, including deaths outside the arena.
        if(!player.gameObject.activeInHierarchy||(StatsManager.Instance!=null&&StatsManager.Instance.currentHealth<=0))
        { ResetArena();charging=-1;charge=0;return; }
        if(charging>=0&&Time.timeScale>0)
        {
            if(!Input.GetKey(KeyCode.E)||Vector2.Distance(player.transform.position,hud.posts[charging].position)>1.8f||Threat(hud.posts[charging].position,4)||(charging==2&&!PlatePressed))
            {charging=-1;charge=0;hud.Notify("启动中断：保持靠近并按住 E，先清理附近守卫。");}
            else
            {
                charge+=Time.deltaTime;hud.Notify("正在恢复供能 "+Mathf.CeilToInt(Mathf.Clamp01(charge/2)*100)+"% · 按住 E");
                if(charge>=2){if(charging==1)west=true;else east=true;charging=-1;charge=0;ApplyState();hud.Notify(Powered?"两处供能恢复！北部工事大门已开启。":"一处供能恢复！前往另一处装置继续解锁。");}
            }
        }
        if(arenaStarted&&!won&&arena.All(e=>e==null||!e.gameObject.activeInHierarchy||e.Health<=0))
        {won=true;arenaStarted=false;ApplyState();if(finalChest!=null)finalChest.gameObject.SetActive(true);hud.Notify("工事守卫已清除！K 打开宝箱，回营地领取通关奖励。");}
        if(plate!=null)plate.GetComponent<SpriteRenderer>().color=PlatePressed?new Color(.5f,1,.65f):new Color(.94f,.76f,.34f);
    }
    private bool arenaStarted;
    public bool ArenaStarted => arenaStarted;
    public bool Threat(Vector2 p,float radius)=>GetComponentsInChildren<RPG1Sentinel>().Any(e=>e.isActiveAndEnabled&&e.Health>0&&Vector2.Distance(e.Feet,p)<radius);
    public string Caption(int i)=>new[]{"营地补给","西侧供能","东侧供能","工事大门","木箱复位","工事挑战","物资宝箱"}[Mathf.Clamp(i,0,6)];
    public string Hint(int i)
    {
        if(i==0)return "E 休息恢复12生命（30秒冷却）；完成北部挑战后在此结算。";
        if(i==1)return west?"供能已经恢复。":"先清理附近敌人，靠近后按住 E 两秒恢复供能。";
        if(i==2)return east?"供能已经锁定，木箱可以移开。":"将木箱推到金色压板，再靠近东侧装置按住 E 两秒。";
        if(i==3)return Powered?"大门已开启，向北进入工事，在战旗处开始挑战。":"同时启动东西供能装置后开启。";
        if(i==4)return "E 将机关木箱恢复到起点；不消耗物品。起点被占用时不会强行复位。";
        if(i==5)return won?"挑战已经完成。":arenaStarted?"击败两名工事守卫；南侧出口允许撤退。":"E 唤醒两名守卫；1.5秒准备后开战，失败无扣款可重试。";
        return won?"K 打开宝箱。":"完成北部工事挑战后出现奖励宝箱。";
    }
    public bool Done(int i)=>i==1?west:i==2?east:i==3?Powered:i==5?won:i==6?won:i==0&&paid;
    public void Interact(int i)
    {
        if(i==0)
        {
            if(won&&!paid){var inv=InventortyManager.Instance;if(inv==null)return;paid=true;inv.AddItem(hud.gold,28);inv.AddItem(hud.experience,8);PlayerStats.AddQuest("RPG1.frontier");hud.Notify("伐木场重启！获得28金币、8经验；本次运行仅结算一次。");return;}
            if(Time.time<nextRest){hud.Notify("营火冷却中，剩余 "+Mathf.CeilToInt(nextRest-Time.time)+" 秒。");return;}
            if(Threat(player.transform.position,5)){hud.Notify("附近有敌人，暂时无法休息。");return;}
            if(StatsManager.Instance!=null){StatsManager.Instance.UpdateHealth(12);nextRest=Time.time+30;hud.Notify("休息恢复最多12生命。接下来去东西供能点，重启工事。");}return;
        }
        if(i==1||i==2)
        {
            if(Done(i)){hud.Notify("此处供能已锁定，无需重复操作。");return;}
            if(Threat(hud.posts[i].position,4)){hud.Notify("附近守卫会打断操作，先清理他们。");return;}
            if(i==2&&!PlatePressed){hud.Notify("东侧装置缺少配重：先将木箱推到金色压板。");return;}
            charging=i;charge=0;return;
        }
        if(i==4){hud.Notify(crate.ResetCrate()?"木箱已复位，可以重新推动。":"木箱起点被占用，先让开起点。");return;}
        if(i==5)
        {
            if(!Powered){hud.Notify("需要恢复东西两处供能。");return;}
            if(won||arenaStarted){hud.Notify(Hint(i));return;}
            arenaStarted=true;foreach(var e in arena)if(e!=null)e.BeginEncounter(1.5f);hud.Notify("工事守卫苏醒！1.5秒后开战，红色区域是攻击预警，可向南撤退。");return;
        }
        hud.Notify(Hint(i));
    }
    public void ApplyState(){if(gate!=null)gate.SetActive(!Powered);}
    public void ResetArena()
    {
        arenaStarted=false;if(!won)foreach(var e in arena)if(e!=null)e.PrepareAgain();
        if(player==null)return;player.gameObject.SetActive(true);
        if(StatsManager.Instance!=null){StatsManager.Instance.currentHealth=Mathf.Max(1,StatsManager.Instance.maxHealth/2);StatsManager.Instance.UpdateHealthText();}
        player.rb.velocity=Vector2.zero;player.rb.position=spawn;player.transform.position=spawn;hud.Notify("已在营地恢复（半血）。供能进度保留；没有扣除金币，可以重新出发。");
    }
}
