using UnityEngine;

public class player_combat : MonoBehaviour
{
    public Transform attackPoint;
    public LayerMask enemyLayer;
    public StatsUI statsUI;

    public Animator anim;
    public float cooldown = 1f;

    private float timer;

    private void Update()
    {
        if (timer > 0f)
            timer -= Time.deltaTime;
    }

    public void Attack()
    {
        if (!isActiveAndEnabled || Time.timeScale <= 0f || anim == null || timer > 0f)
            return;

        anim.SetBool("isAttacking", true);
        timer = cooldown;
        Sfx.Blade();
    }

    public void DealDamage()
    {
        if (!isActiveAndEnabled || attackPoint == null || StatsManager.Instance == null)
            return;

      

        if (statsUI != null)
            statsUI.UpdateDamage();

        int layerMask = enemyLayer.value != 0
            ? enemyLayer.value
            : ~0;

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            attackPoint.position,
            StatsManager.Instance.weaponRange,
            layerMask
        );

        foreach (Collider2D hit in hits)
        {
            Enemy_Health health =
                hit.GetComponentInParent<Enemy_Health>();

            if (health == null)
                continue;

            health.ChangeHealth(-StatsManager.Instance.damage);
            Sfx.Impact();

            Enemy_Knockback knockback =
                hit.GetComponentInParent<Enemy_Knockback>();

            if (knockback != null)
            {
                knockback.Knockback(
                    transform,
                    StatsManager.Instance.knockbackForce,
                    StatsManager.Instance.stunTime,
                    StatsManager.Instance.knockbackTime
                );
            }

            break;
        }
    }

    private void OnDisable()
    {
        FinishAttacking();
    }

    public void FinishAttacking()
    {
        if (anim != null)
            anim.SetBool("isAttacking", false);
    }

    private void OnDrawGizmosSelected()
    {
        if (!isActiveAndEnabled || attackPoint == null || StatsManager.Instance == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            attackPoint.position,
            StatsManager.Instance.weaponRange
        );
    }
}
