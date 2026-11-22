using UnityEngine;

public class Enemy_Combat : MonoBehaviour
{
    public int damage = 1;
    public Transform attackPoinnt;
    public float weaponRange = 1f;
    public LayerMask playerLayer;
    public float minimumHitRange = 2.2f;
    public float attackCooldown = 0.25f;

    private float lastAttackTime = -999f;

    public void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        PlayerMovement movement = FindObjectOfType<PlayerMovement>();

        if (movement == null)
            return;

        float distance = Vector2.Distance(
            GetFeetPosition(transform),
            GetFeetPosition(movement.transform)
        );

        float range = Mathf.Max(weaponRange, minimumHitRange);

        if (distance > range)
            return;

        PlayerHealth health =
            movement.GetComponentInParent<PlayerHealth>();

        if (health == null)
            return;

        health.ChangeHealth(-damage);
        movement.Knockback(transform);
        Sfx.EnemySwing();
    }

    private Vector2 GetFeetPosition(Transform target)
    {
        Collider2D[] colliders = target.GetComponents<Collider2D>();

        foreach (Collider2D collider in colliders)
        {
            if (collider.isTrigger)
                continue;

            Bounds bounds = collider.bounds;
            return new Vector2(bounds.center.x, bounds.min.y);
        }

        return target.position;
    }
}
