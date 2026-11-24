using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arrow : MonoBehaviour
{
    public Rigidbody2D rb;
    public Vector2 direction = Vector2.right;
    public float speed;
    public float lifeSpawn = 2;

    public LayerMask enemyLayer;
    public LayerMask obstacleLayer;

    public SpriteRenderer sr;
    public Sprite buriedSprite;

    public int damage;
    public float knockbackForce;
    public float knockbackTime;
    public float stunTime;
    // Start is called before the first frame update
    void Start()
    {
        ObliqueView2D.ConfigureActor(GetComponent<SpriteRenderer>());
        var shooter = FindObjectOfType<PlayerMovement>();
        var depth = GetComponent<ObliqueDepth2D>();
        var shooterDepth = shooter != null ? shooter.GetComponent<ObliqueDepth2D>() : null;
        if (depth != null && shooterDepth != null) depth.elevated = shooterDepth.elevated;
        rb.velocity = direction * speed;
        RotateArrow();
        Destroy(gameObject,lifeSpawn);
    }
    private void RotateArrow()
    {
        float angle = Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(new Vector3(0,0,angle));
    }
    public void OnCollisionEnter2D(Collision2D collision)
    {
        if ((enemyLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            collision.gameObject.GetComponent<Enemy_Health>().ChangeHealth(-NightCombat2D.PlayerDamage(damage));
            collision.gameObject.GetComponent<Enemy_Knockback>().Knockback(transform, knockbackForce, stunTime, knockbackTime);
            Sfx.Impact(.8f);

        }
        else if ((obstacleLayer.value & (1 << collision.gameObject.layer)) > 0)
        {
            Sfx.Impact(.55f);
            AttachToTarget(collision.gameObject.transform);
        }
        
    }
    private void AttachToTarget(Transform target)
    {
        sr.sprite = buriedSprite;
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;

        transform.SetParent(target);

    }
    // Update is called once per frame

}
