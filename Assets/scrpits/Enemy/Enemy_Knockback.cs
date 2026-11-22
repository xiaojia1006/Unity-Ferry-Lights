using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy_Knockback : MonoBehaviour
{
    private Rigidbody2D rb;
    private Enemy_Movement enemy_Movement;
    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        enemy_Movement = GetComponent<Enemy_Movement>();
    }
    // Start is called before the first frame update
    public void Knockback(Transform forceTransform,float knockbackForce,float stunTime,float knockbackTime) 
    {
        Vector2 direction = (transform.position - forceTransform.position).normalized;
        rb.velocity = direction * knockbackForce;
        Debug.Log("knockback applied");
        enemy_Movement.ChangeState(EnemyState.Knocback);
        StopAllCoroutines();
        StartCoroutine(StunTime(stunTime,knockbackTime));
    }
    IEnumerator StunTime(float stuTime,float knockbackTime) 
    {
        yield return new WaitForSeconds(knockbackTime);
        rb.velocity = Vector2.zero;
        yield return new WaitForSeconds(stuTime);
        enemy_Movement.ChangeState(EnemyState.Idle);
    }
}
