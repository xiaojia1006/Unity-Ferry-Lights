using UnityEngine;

public class Enemy_Movement : MonoBehaviour
{
    public float speed = 2f;
    public float attackrange = 2f;
    public float detectionRange = 3f;
    public float loseTargetRange = 4.5f;
    public float loseTargetDelay = 1.25f;
    public float attackHitDelay = 0.3f;
    public float attackDuration = 0.6f;
    public float minimumAttackRange = 1.5f;
    public float facingDeadZone = 0.2f;

    private Rigidbody2D rb;
    private Transform player;
    private int facingDirection = -1;
    private Animator anim;
    private Enemy_Combat combat;
    private float loseTargetTimer;
    private float attackTimer;
    private bool attackDamageSent;

    public EnemyState enemyState;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        combat = GetComponent<Enemy_Combat>();

        PlayerMovement playerMovement =
            FindObjectOfType<PlayerMovement>();

        if (playerMovement != null)
            player = playerMovement.transform;

        enemyState = EnemyState.Idle;
        anim.SetBool("isIdle", true);
        anim.SetBool("isChasing", false);
        anim.SetBool("isAttacking", false);
    }

    private void Update()
    {
        if (enemyState != EnemyState.Knocback)
        {


            if (player == null)
                return;

            float distance = Vector2.Distance(
                GetFeetPosition(transform),
                GetFeetPosition(player)
            );

            bool playerDetected = distance <= detectionRange;

            if (playerDetected)
            {
                loseTargetTimer = loseTargetDelay;
            }
            else if (enemyState != EnemyState.Idle)
            {
                loseTargetTimer -= Time.deltaTime;
            }

            if (enemyState == EnemyState.Idle && !playerDetected)
                return;

            if (loseTargetTimer <= 0f || distance > loseTargetRange)
            {
                rb.velocity = Vector2.zero;
                ChangeState(EnemyState.Idle);
                return;
            }

            if (enemyState == EnemyState.Attacking)
            {
                HandleAttack(distance);
                return;
            }

            float effectiveAttackRange =
                Mathf.Max(attackrange, minimumAttackRange);

            if (distance <= effectiveAttackRange)
            {
                StartAttack();
            }
            else
            {
                ChangeState(EnemyState.Chasing);
                Chase();
            }
        }
    }

    private void Chase()
    {
        float dirX = player.position.x - transform.position.x;

        if (dirX > facingDeadZone && facingDirection == -1)
        {
            Flip();
        }
        else if (dirX < -facingDeadZone && facingDirection == 1)
        {
            Flip();
        }

        Vector2 direction =
            (player.position - transform.position).normalized;

        rb.velocity = direction * speed;
    }

    private void StartAttack()
    {
        SetAttackDirection(player.position - transform.position);
        ChangeState(EnemyState.Attacking);
        rb.velocity = Vector2.zero;
        attackTimer = attackDuration;
        attackDamageSent = false;
    }

    private void HandleAttack(float distance)
    {
        rb.velocity = Vector2.zero;
        attackTimer -= Time.deltaTime;

        float elapsedTime = attackDuration - attackTimer;

        if (!attackDamageSent && elapsedTime >= attackHitDelay)
        {
            attackDamageSent = true;

            if (combat != null)
                combat.Attack();
        }

        if (attackTimer > 0f)
            return;

        ChangeState(EnemyState.Idle);
    }

    private void Flip()
    {
        facingDirection *= -1;

        transform.localScale = new Vector3(
            transform.localScale.x * -1f,
            transform.localScale.y,
            transform.localScale.z
        );
    }

    private void SetAttackDirection(Vector2 direction)
    {
        int attackDirection;

        if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y))
            attackDirection = direction.x >= 0f ? 2 : 3;
        else
            attackDirection = direction.y >= 0f ? 0 : 1;

        anim.SetInteger("attackDirection", attackDirection);
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

    public void ChangeState(EnemyState newState)
    {
        if (anim == null) anim = GetComponent<Animator>();
        if (enemyState == newState)
            return;

        anim.SetBool("isIdle", newState == EnemyState.Idle);
        anim.SetBool("isChasing", newState == EnemyState.Chasing);
        anim.SetBool("isAttacking", newState == EnemyState.Attacking);

        enemyState = newState;
    }
}

public enum EnemyState
{
    Idle,
    Chasing,
    Attacking,
    Knocback
}
