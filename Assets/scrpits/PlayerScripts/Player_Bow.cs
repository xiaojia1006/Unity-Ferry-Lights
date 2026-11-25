using UnityEngine;
using UnityEngine.EventSystems;

[DefaultExecutionOrder(2000)]
public class Player_Bow : MonoBehaviour
{
    public Transform launchPoint;
    public GameObject arrowPrefab;
    public Vector2 aimDirection=Vector2.right;
    public float shootCooldown=.5f;
    float shootTimer,pendingUntil,noticeUntil;
    bool pending;bool released;float requestTime;
    bool aimInitialized;
    public float aimResponse=24f;
    public Animator anim;
    public PlayerMovement playerMovement;
    public int Ammo => ArrowCrafting.Count;
    bool Blocked => Time.timeScale<=0||GlobalSettings.BlocksInput||BackpackUI.BlocksOtherInput||FishingRig2D.Busy||ShopKeeper.currentShopKeeper!=null||DialogueManager.Instance!=null&&DialogueManager.Instance.isDialogueActive;
    void Update()
    {
        shootTimer=Mathf.Max(0,shootTimer-Time.deltaTime);
        if(Blocked){CancelShot();return;}
        // Aim is updated after the following camera has settled in LateUpdate.
        // 按下 = 拉弓，松开 = 放箭
        // 按住时把动画冻在"拉满"那一帧，别让 Shoot 片段循环重播
        if(pending&&!released&&anim!=null&&anim.speed>0f&&Time.time-requestTime>.45f)anim.speed=0f;
        if(Input.GetMouseButtonDown(0)&&!PointerOnUI())RequestShot();
        if(Input.GetMouseButtonUp(0)&&pending)ReleaseShot();
    }
    static bool PointerOnUI()=>EventSystem.current!=null&&EventSystem.current.IsPointerOverGameObject();
    public static bool TryMouseAim(Camera camera,Vector3 screen,Vector3 origin,out Vector2 direction)
    {
        direction=Vector2.zero;if(camera==null||!camera.pixelRect.Contains(screen))return false;
        var ray=camera.ScreenPointToRay(screen);float distance;
        if(!new Plane(Vector3.forward,new Vector3(0,0,origin.z)).Raycast(ray,out distance))return false;
        direction=(Vector2)(ray.GetPoint(distance)-origin);if(direction.sqrMagnitude<.001f)return false;direction.Normalize();return true;
    }
    void LateUpdate(){if(!Blocked)HandleAiming();}
    public static Vector2 SmoothDirection(Vector2 current,Vector2 target,float response,float dt)
    {
        if(target.sqrMagnitude<.001f)return current;
        if(current.sqrMagnitude<.001f)return target.normalized;
        float from=Mathf.Atan2(current.y,current.x)*Mathf.Rad2Deg,to=Mathf.Atan2(target.y,target.x)*Mathf.Rad2Deg;
        float angle=from+Mathf.DeltaAngle(from,to)*(1-Mathf.Exp(-Mathf.Max(1,response)*Mathf.Max(0,dt)));
        return new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad));
    }
    public void HandleAiming()
    {
        Vector2 direction;if(!TryMouseAim(Camera.main,Input.mousePosition,transform.position,out direction))return;
        // Ignore a tiny cursor circle around the actor to avoid direction flipping at zero distance.
        var camera=Camera.main;
        if(camera!=null&&Vector2.Distance(Input.mousePosition,camera.WorldToScreenPoint(transform.position))<12)return;
        aimDirection=aimInitialized?SmoothDirection(aimDirection,direction,aimResponse,Time.deltaTime):direction;
        aimInitialized=true;direction=aimDirection;
        if(anim!=null){anim.SetFloat("aimX",direction.x);anim.SetFloat("aimY",direction.y);}
        if(anim!=null&&anim.speed==0f)anim.Update(0f);
        if(playerMovement!=null&&Mathf.Abs(direction.x)>.16f)playerMovement.FaceTowards(transform.position.x+direction.x);
    }
    public bool RequestShot()
    {
        if(!isActiveAndEnabled||Blocked||pending||shootTimer>0||arrowPrefab==null||arrowPrefab.GetComponent<Arrow>()==null||launchPoint==null||anim==null)return false;
        if(Ammo<=0){if(Time.unscaledTime>=noticeUntil){ArrowCrafting.Notify("箭矢不足：背包选中骨头，制作5支骨箭。");noticeUntil=Time.unscaledTime+2;}return false;}
        pending=true;released=false;requestTime=Time.time;pendingUntil=Time.time+2.5f;if(anim!=null)anim.speed=1f;anim.SetBool("isShooting",true);if(playerMovement!=null)playerMovement.isShooting=true;return true;
    }
    // 松开鼠标才真正放箭
    public void ReleaseShot()
    {
        if(!pending)return;
        HandleAiming();
        released=true;
        if(anim!=null)anim.speed=1f;
        Shoot();
    }

    // 动画事件也会调这里：只有已经松开鼠标才允许出箭
    public void Shoot()
    {
        if(!pending)return;
        // 动画事件先到、鼠标还没松：冻在拉满的姿势上，等松手
        if(!released){if(anim!=null)anim.speed=0f;return;}
        pending=false;
        if(!isActiveAndEnabled||Blocked||shootTimer>0||arrowPrefab==null||arrowPrefab.GetComponent<Arrow>()==null||launchPoint==null){CancelShot();return;}
        if(!ArrowCrafting.SpendOne()){CancelShot();return;}
        var go=Instantiate(arrowPrefab,launchPoint.position,Quaternion.identity);var arrow=go.GetComponent<Arrow>();
        arrow.direction=aimDirection;var body=go.GetComponent<Collider2D>();if(body!=null&&playerMovement!=null)foreach(var c in playerMovement.GetComponentsInChildren<Collider2D>())Physics2D.IgnoreCollision(body,c);
        shootTimer=shootCooldown;Sfx.Bow();SaveSystem.Checkpoint();CancelShot();
    }
    void CancelShot(){pending=false;released=false;if(anim!=null)anim.speed=1f;if(anim!=null)anim.SetBool("isShooting",false);if(playerMovement!=null)playerMovement.isShooting=false;}
    void OnEnable(){aimInitialized=false;if(anim!=null){anim.SetLayerWeight(0,0);anim.SetLayerWeight(1,1);}}
    void OnDisable(){CancelShot();if(anim!=null){anim.SetLayerWeight(0,1);anim.SetLayerWeight(1,0);}}
}
