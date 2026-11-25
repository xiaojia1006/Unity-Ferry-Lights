using System.Collections;
using UnityEngine;

public class Enemy_Knockback : MonoBehaviour
{
    Rigidbody2D rb;
    Enemy_Movement movement;
    Enemy_Health health;
    bool stunned;
    void Awake(){Resolve();}
    void Resolve(){if(rb==null)rb=GetComponent<Rigidbody2D>();if(movement==null)movement=GetComponent<Enemy_Movement>();if(health==null)health=GetComponent<Enemy_Health>();}
    public void Knockback(Transform source,float force,float stunTime,float knockbackTime)
    {
        // A lethal hit can synchronously disable the enemy inside ChangeHealth.
        // Do not set its state or start a coroutine after that happens.
        if(!isActiveAndEnabled||!gameObject.activeInHierarchy||source==null)return;
        Resolve();if(rb==null||health!=null&&health.currenetHealth<=0)return;
        StopAllCoroutines();stunned=true;
        rb.velocity=((Vector2)(transform.position-source.position)).normalized*Mathf.Max(0,force);
        if(movement!=null)movement.ChangeState(EnemyState.Knocback);
        StartCoroutine(Recover(Mathf.Max(0,stunTime),Mathf.Max(0,knockbackTime)));
    }
    IEnumerator Recover(float stun,float knockback)
    {
        yield return new WaitForSeconds(knockback);
        if(rb!=null)rb.velocity=Vector2.zero;
        yield return new WaitForSeconds(stun);
        Restore();
    }
    void Restore()
    {
        if(!stunned)return;stunned=false;
        if(rb!=null)rb.velocity=Vector2.zero;
        if(movement!=null&&(health==null||health.currenetHealth>0))movement.ChangeState(EnemyState.Idle);
    }
    void OnDisable(){StopAllCoroutines();Restore();}
}
