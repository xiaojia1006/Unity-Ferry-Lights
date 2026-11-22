using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(160)]
public sealed class RPG2Guard : MonoBehaviour
{
    public Vector2 home,end;
    public Rect territory;
    public RPG2Adventure world;
    public float speed=1.7f;
    public int Health=>health!=null?health.currenetHealth:0;
    private Rigidbody2D body;
    private Enemy_Health health;
    private Enemy_Movement legacy;
    private Animator animator;
    private bool outward=true,hit;
    private float attack=-1,cooldown;
    private Vector2 targetAt;
    private SpriteRenderer warning;
    private Vector2 lastSafe;
    private void Awake()
    {
        body=GetComponent<Rigidbody2D>();health=GetComponent<Enemy_Health>();legacy=GetComponent<Enemy_Movement>();animator=GetComponent<Animator>();
        legacy.enabled=false;GetComponent<Enemy_Combat>().enabled=false;
        body.gravityScale=0;body.freezeRotation=true;body.constraints=RigidbodyConstraints2D.FreezeRotation;body.bodyType=RigidbodyType2D.Dynamic;body.simulated=true;
        var go=new GameObject("Guard attack telegraph");go.transform.SetParent(world.transform);warning=go.AddComponent<SpriteRenderer>();warning.sprite=world.button;warning.sortingOrder=-60;go.SetActive(false);
        lastSafe=home;
    }
    private bool Clear(Vector2 from,Vector2 to)=>!Physics2D.LinecastAll(from,to,1<<7).Any(h=>h.collider!=null&&!h.collider.isTrigger&&h.rigidbody!=body);
    private bool Free(Vector2 p)=>territory.Contains(p)&&!Physics2D.OverlapCircleAll(p,.3f,1<<7).Any(c=>!c.isTrigger&&c.attachedRigidbody!=body);
    private void Update()
    {
        if(Health<=0||world.Player==null||Time.timeScale<=0){body.velocity=Vector2.zero;return;}
        if(legacy.enemyState==EnemyState.Knocback){attack=-1;warning.gameObject.SetActive(false);return;}
        Vector2 at=transform.position,target=world.Player.transform.position;
        if(!territory.Contains(at)){body.position=lastSafe;transform.position=lastSafe;body.velocity=Vector2.zero;at=lastSafe;}
        else if(Free(at))lastSafe=at;
        if(attack>=0)
        {
            body.velocity=Vector2.zero;float elapsed=Time.time-attack;
            if(elapsed<.65f)return;
            warning.gameObject.SetActive(false);
            if(!hit)
            {
                hit=true;animator.SetBool("isAttacking",true);
                if(Vector2.Distance(target,targetAt)<1&&Vector2.Distance(at,target)<1.9f&&Clear(at,target))
                {world.Player.GetComponent<PlayerHealth>().ChangeHealth(-2);world.Player.Knockback(transform);}
            }
            if(elapsed>1.05f){attack=-1;cooldown=Time.time+.65f;animator.SetBool("isAttacking",false);}return;
        }
        bool chase=territory.Contains(target)&&Vector2.Distance(at,target)<4.6f&&Clear(at,target);
        if(chase&&Vector2.Distance(at,target)<1.4f&&Time.time>=cooldown)
        {
            targetAt=target;attack=Time.time;hit=false;body.velocity=Vector2.zero;
            var attackDirection=target-at;animator.SetInteger("attackDirection",Mathf.Abs(attackDirection.x)>Mathf.Abs(attackDirection.y)?(attackDirection.x>0?2:3):(attackDirection.y>0?0:1));
            animator.SetBool("isChasing",false);animator.SetBool("isIdle",true);
            warning.transform.position=targetAt;warning.transform.localScale=new Vector3(1.8f/warning.sprite.bounds.size.x,1.0f/warning.sprite.bounds.size.y,1);warning.color=new Color(1,.22f,.12f,.45f);warning.gameObject.SetActive(true);return;
        }
        Vector2 goal=chase?target:outward?end:home;
        if(!chase&&Vector2.Distance(at,goal)<.35f){outward=!outward;goal=outward?end:home;}
        Vector2 direction=(goal-at).normalized;
        if(!Free(at+direction*.55f))
        {var side=new Vector2(-direction.y,direction.x);direction=Free(at+side*.55f)?side:Free(at-side*.55f)?-side:Vector2.zero;}
        body.velocity=direction*(chase?speed:speed*.65f);
        animator.SetBool("isIdle",direction.sqrMagnitude<.1f);animator.SetBool("isChasing",direction.sqrMagnitude>.1f);
        if(Mathf.Abs(direction.x)>.1f){var s=transform.localScale;s.x=Mathf.Abs(s.x)*(direction.x>0?-1:1);transform.localScale=s;}
    }
    private void OnDisable(){if(warning!=null)warning.gameObject.SetActive(false);if(body!=null)body.velocity=Vector2.zero;}
}
