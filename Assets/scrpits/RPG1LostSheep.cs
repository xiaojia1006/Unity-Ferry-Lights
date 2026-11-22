using UnityEngine;

[RequireComponent(typeof(Rigidbody2D),typeof(CapsuleCollider2D))]
public sealed class RPG1LostSheep : MonoBehaviour
{
    public RPG1Frontier world;
    public Sprite[] idle,walk;
    public Vector2 home;
    public Rect meadow;
    private Rigidbody2D body;
    private SpriteRenderer art;
    private PlayerMovement player;
    private Vector2 target;
    private float chooseAt;
    private bool following;
    private static bool rescued;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession(){rescued=false;}
    private void Start(){body=GetComponent<Rigidbody2D>();art=GetComponent<SpriteRenderer>();player=FindObjectOfType<PlayerMovement>();body.gravityScale=0;body.freezeRotation=true;if(rescued){home=world.spawn+Vector2.right*3;transform.position=home;meadow=new Rect(home-Vector2.one*2,Vector2.one*4);}target=home;}
    private void Update()
    {
        if(player==null||Time.timeScale<=0){body.velocity=Vector2.zero;return;}
        float distance=Vector2.Distance(player.transform.position,transform.position);
        if(!rescued&&distance<1.5f&&!BackpackUI.BlocksOtherInput&&ShopKeeper.currentShopKeeper==null&&!RPG1Activities.BlocksMenuInput)
        {
            world.hud.Notify(following?"E 让羊等候 · 带回西南营地获得一次救援奖励":"E 呼唤迷路的羊 · 带回西南营地，注意绕开守卫");
            if(Input.GetKeyDown(KeyCode.E))following=!following;
        }
        if(following&&world.safeCamp.Contains(transform.position))
        {
            if(InventortyManager.Instance==null)return;
            rescued=true;following=false;home=transform.position;meadow=new Rect(home-Vector2.one*2,Vector2.one*4);
            InventortyManager.Instance.AddItem(world.hud.gold,12);world.hud.Notify("小羊已安全回营地！获得12金币；它会留在营地散步。");
        }
        Vector2 at=body.position,delta;
        if(following){delta=(Vector2)player.transform.position-at;if(distance<1.1f||distance>9)delta=Vector2.zero;}
        else{if(Time.time>chooseAt){target=new Vector2(Random.Range(meadow.xMin,meadow.xMax),Random.Range(meadow.yMin,meadow.yMax));chooseAt=Time.time+Random.Range(3,6);}delta=target-at;if(delta.magnitude<.3f)delta=Vector2.zero;}
        var direction=delta.normalized;bool blocked=false;
        foreach(var hit in Physics2D.CircleCastAll(at,.3f,direction,.6f,1<<7))if(!hit.collider.isTrigger)blocked=true;
        if(blocked&&following&&direction.sqrMagnitude>0)
        {
            var side=new Vector2(-direction.y,direction.x);
            if(Clear(at,side)){direction=side;blocked=false;}else if(Clear(at,-side)){direction=-side;blocked=false;}
        }
        body.velocity=blocked?Vector2.zero:direction*(following?2.8f:.65f);
        bool moving=body.velocity.sqrMagnitude>.01f;var frames=moving?walk:idle;
        if(frames!=null&&frames.Length>0)art.sprite=frames[Mathf.FloorToInt(Time.time*(moving?8:5))%frames.Length];
        if(Mathf.Abs(body.velocity.x)>.1f)art.flipX=body.velocity.x<0;
    }
    private bool Clear(Vector2 at,Vector2 direction){foreach(var hit in Physics2D.CircleCastAll(at,.3f,direction,.7f,1<<7))if(!hit.collider.isTrigger)return false;return true;}
}
