using UnityEngine;

// Scene-specific patrol/combat using the original enemy art, Animator and damage receiver.
[DefaultExecutionOrder(150)]
public sealed class RPG1Sentinel : MonoBehaviour
{
    public RPG1Frontier world;
    public Vector2 home,patrolEnd;
    public Rect territory;
    public bool arenaOnly;
    public float speed=1.65f,detect=4.2f,reach=1.15f;
    public int damage=2;
    private Rigidbody2D body;
    private Animator animator;
    private Enemy_Movement legacy;
    private Enemy_Health health;
    private PlayerMovement player;
    private Collider2D foot;
    private Vector2 lockedTarget;
    private float attackTime=-1,cooldown,stunUntil,readyAt;
    private bool toEnd=true,awake;
    private Vector2 lastSafe;
    private SpriteRenderer telegraph;
    public int Health=>health!=null?health.currenetHealth:0;
    public Vector2 Feet => foot!=null?(Vector2)foot.bounds.center:(Vector2)transform.position;
    private void Awake()
    {
        body=GetComponent<Rigidbody2D>();animator=GetComponent<Animator>();legacy=GetComponent<Enemy_Movement>();health=GetComponent<Enemy_Health>();
        foreach(var c in GetComponents<Collider2D>())if(!c.isTrigger)foot=c;
        legacy.enabled=false;GetComponent<Enemy_Combat>().enabled=false;
        body.gravityScale=0;body.freezeRotation=true;body.collisionDetectionMode=CollisionDetectionMode2D.Continuous;
        var go=new GameObject("Attack warning");go.transform.SetParent(world.transform,false);telegraph=go.AddComponent<SpriteRenderer>();telegraph.sprite=world.hud.button;telegraph.sortingOrder=-60;go.SetActive(false);
        awake=!arenaOnly;
        lastSafe=transform.position;
        health.invulnerable=arenaOnly;
    }
    private void Start(){player=FindObjectOfType<PlayerMovement>();SetAnimation(false,false);}
    private void OnDisable(){if(telegraph!=null)telegraph.gameObject.SetActive(false);if(body!=null)body.velocity=Vector2.zero;}
    public void BeginEncounter(float delay){awake=true;health.invulnerable=false;readyAt=Time.time+delay;}
    public void PrepareAgain()
    {
        health.Revive();health.invulnerable=true;body.position=home;transform.position=home;body.velocity=Vector2.zero;awake=false;attackTime=-1;cooldown=Time.time+1;legacy.enemyState=EnemyState.Idle;SetAnimation(false,false);
    }
    public bool ClearLine(Vector2 a,Vector2 b)
    {
        foreach(var hit in Physics2D.LinecastAll(a,b,1<<7))if(hit.collider!=null&&!hit.collider.isTrigger&&hit.rigidbody!=body)return false;
        return true;
    }
    private bool Walkable(Vector2 p)
    {
        if(!territory.Contains(p)||world.safeCamp.Contains(p))return false;
        foreach(var hit in Physics2D.OverlapCircleAll(p,.25f,1<<7))if(!hit.isTrigger&&hit.attachedRigidbody!=body)return false;
        return true;
    }
    private void Update()
    {
        if(health!=null&&health.currenetHealth<=0){body.velocity=Vector2.zero;return;}
        if(legacy.enemyState!=EnemyState.Knocback&&(!territory.Contains(Feet)||world.safeCamp.Contains(Feet)))
        {body.position=lastSafe;transform.position=lastSafe;body.velocity=Vector2.zero;}
        else if(Walkable(Feet))lastSafe=transform.position;
        if(Time.timeScale<=0||player==null||!player.gameObject.activeInHierarchy||!awake||Time.time<readyAt){body.velocity=Vector2.zero;return;}
        if(legacy.enemyState==EnemyState.Knocback){stunUntil=Time.time+.2f;attackTime=-1;telegraph.gameObject.SetActive(false);return;}
        if(Time.time<stunUntil){body.velocity=Vector2.zero;return;}
        Vector2 at=Feet,target=PlayerFeet();
        if(attackTime>=0)
        {
            body.velocity=Vector2.zero;float elapsed=Time.time-attackTime;
            if(elapsed<.75f){telegraph.color=new Color(1,.25f,.1f,.25f+.18f*Mathf.Sin(elapsed*25));return;}
            telegraph.gameObject.SetActive(false);SetAnimation(false,true);
            if(elapsed<.95f)
            {
                if(!hitSent){hitSent=true;if(Vector2.Distance(target,lockedTarget)<.85f&&Vector2.Distance(at,target)<reach+.45f&&ClearLine(at,target))
                {var hp=player.GetComponent<PlayerHealth>();if(hp!=null)hp.ChangeHealth(-damage);player.Knockback(transform);}}
                return;
            }
            if(elapsed<1.35f)return;attackTime=-1;cooldown=Time.time+.6f;SetAnimation(false,false);return;
        }
        bool chase=territory.Contains(target)&&!world.safeCamp.Contains(target)&&Vector2.Distance(at,target)<detect&&ClearLine(at,target);
        if(chase&&Vector2.Distance(at,target)<reach&&Time.time>=cooldown)
        {
            body.velocity=Vector2.zero;lockedTarget=target;attackTime=Time.time;hitSent=false;Face(target-at);
            animator.SetInteger("attackDirection",Mathf.Abs(target.x-at.x)>Mathf.Abs(target.y-at.y)?(target.x>at.x?2:3):(target.y>at.y?0:1));
            telegraph.transform.position=lockedTarget;telegraph.transform.localScale=new Vector3(1.8f/telegraph.sprite.bounds.size.x,.95f/telegraph.sprite.bounds.size.y,1);telegraph.gameObject.SetActive(true);SetAnimation(false,false);return;
        }
        Vector2 goal=chase?target:toEnd?patrolEnd:home;
        if(!chase&&Vector2.Distance(transform.position,goal)<.45f){toEnd=!toEnd;goal=toEnd?patrolEnd:home;}
        Vector2 direction=(goal-(Vector2)transform.position).normalized;
        if(!Walkable(at+direction*.48f))
        {
            Vector2 side=new Vector2(-direction.y,direction.x);
            if(Walkable(at+side*.5f))direction=side;else if(Walkable(at-side*.5f))direction=-side;else direction=Vector2.zero;
        }
        body.velocity=direction*(chase?speed:speed*.6f);Face(direction);SetAnimation(direction.sqrMagnitude>.1f,false);
    }
    private bool hitSent;
    private Vector2 PlayerFeet(){foreach(var c in player.GetComponents<Collider2D>())if(!c.isTrigger)return c.bounds.center;return player.transform.position;}
    private void Face(Vector2 dir){if(Mathf.Abs(dir.x)>.15f){var s=transform.localScale;s.x=Mathf.Abs(s.x)*(dir.x>0?-1:1);transform.localScale=s;}}
    private void SetAnimation(bool moving,bool attacking){animator.SetBool("isIdle",!moving&&!attacking);animator.SetBool("isChasing",moving);animator.SetBool("isAttacking",attacking);}
}
