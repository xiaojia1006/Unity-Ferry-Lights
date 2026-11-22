using UnityEngine;

// New-map-only leash: telegraphed local encounters never pursue a player into the village.
[DefaultExecutionOrder(100)]
public sealed class WillowreachGuard : MonoBehaviour
{
    public Vector2 home;
    public float leash = 5;
    private Enemy_Movement movement;
    private Enemy_Combat combat;
    private Rigidbody2D body;
    private bool returning;
    private void Start() { movement=GetComponent<Enemy_Movement>();combat=GetComponent<Enemy_Combat>();body=GetComponent<Rigidbody2D>(); }
    private void LateUpdate()
    {
        if(movement==null || body==null || Time.timeScale<=0) return;
        if(Vector2.Distance(transform.position,home)>leash) returning=true;
        if(!returning) return;
        movement.enabled=false;combat.enabled=false;
        movement.ChangeState(EnemyState.Chasing);
        body.velocity=(home-(Vector2)transform.position).normalized*movement.speed;
        if(Vector2.Distance(transform.position,home)<.25f)
        { body.velocity=Vector2.zero;movement.enabled=true;combat.enabled=true;movement.ChangeState(EnemyState.Idle);returning=false; }
    }
}
