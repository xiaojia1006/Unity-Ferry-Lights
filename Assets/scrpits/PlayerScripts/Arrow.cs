using UnityEngine;

public class Arrow : MonoBehaviour
{
    public Rigidbody2D rb;
    public Vector2 direction=Vector2.right;
    public float speed,lifeSpawn=2;
    public LayerMask enemyLayer,obstacleLayer;
    public SpriteRenderer sr;
    public Sprite buriedSprite;
    public int damage;
    public float knockbackForce,knockbackTime,stunTime;
    bool hitTarget;
    Transform shooter;
    public bool HasHit => hitTarget;
    void Awake()
    {
        if(rb==null)rb=GetComponent<Rigidbody2D>();if(sr==null)sr=GetComponent<SpriteRenderer>();
        if(rb!=null){rb.gravityScale=0;rb.collisionDetectionMode=CollisionDetectionMode2D.Continuous;rb.interpolation=RigidbodyInterpolation2D.Interpolate;}
    }
    void Start()
    {
        ObliqueView2D.ConfigureActor(GetComponent<SpriteRenderer>());
        var player=FindObjectOfType<PlayerMovement>();shooter=player!=null?player.transform:null;
        var depth=GetComponent<ObliqueDepth2D>();var source=player!=null?player.GetComponent<ObliqueDepth2D>():null;
        if(depth!=null&&source!=null)depth.elevated=source.elevated;
        direction=direction.sqrMagnitude>.001f?direction.normalized:Vector2.right;
        if(rb!=null)rb.velocity=direction*speed;
        transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
        Destroy(gameObject,lifeSpawn);
    }
    void FixedUpdate()
    {
        if(hitTarget||rb==null||Time.timeScale<=0)return;
        // Sweep the next physics step so a thin enemy collider cannot be skipped by a fast arrow.
        int mask=Physics2D.AllLayers; // Child hurtboxes may use a different layer from their enemy root.
        var hits=Physics2D.CircleCastAll(rb.position,.06f,direction,rb.velocity.magnitude*Time.fixedDeltaTime,mask);
        foreach(var hit in hits)
        {
            if(hit.collider==null||hit.collider.transform==transform||hit.collider.transform.IsChildOf(transform))continue;
            if(shooter!=null&&hit.collider.transform.IsChildOf(shooter))continue;
            if(TryHit(hit.collider))break;
        }
    }
    public void OnCollisionEnter2D(Collision2D collision){TryHit(collision.collider);}
    void OnTriggerEnter2D(Collider2D other){TryHit(other);}
    public bool TryHit(Collider2D other)
    {
        if(hitTarget||other==null)return false;
        if(shooter!=null&&other.transform.IsChildOf(shooter))return false;
        var enemy=other.GetComponentInParent<Enemy_Health>();
        if(enemy!=null&&((enemyLayer.value&(1<<other.gameObject.layer))!=0||(enemyLayer.value&(1<<enemy.gameObject.layer))!=0))
        {
            hitTarget=true;enemy.ChangeHealth(-NightCombat2D.PlayerDamage(damage));
            var knock=enemy.GetComponent<Enemy_Knockback>();if(knock!=null&&enemy.currenetHealth>0&&enemy.gameObject.activeInHierarchy)knock.Knockback(transform,knockbackForce,stunTime,knockbackTime);
            if(rb!=null)rb.velocity=Vector2.zero;Sfx.Impact(.8f);Destroy(gameObject);return true;
        }
        if(!other.isTrigger&&(obstacleLayer.value&(1<<other.gameObject.layer))!=0)
        {
            hitTarget=true;Sfx.Impact(.55f);if(sr!=null&&buriedSprite!=null)sr.sprite=buriedSprite;
            if(rb!=null){rb.velocity=Vector2.zero;rb.simulated=false;}transform.SetParent(other.transform,true);return true;
        }
        return false;
    }
}
