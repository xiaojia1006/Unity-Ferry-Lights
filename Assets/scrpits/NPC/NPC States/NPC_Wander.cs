using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class NPC_Wander : MonoBehaviour
{
    [Header("Wander Area")]
    public float wanderWidth = 5f;
    public float wanderHeight = 5f;
    public Vector2 staringPosition;

    public float speed = 2f;
    public Vector2 target;
    public float pauseDuration = 1f;

    private bool isPaused;
    private Rigidbody2D rb;
    private Animator anim;
    private Coroutine pauseRoutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        Transform sprite = transform.Find("Sprite");

        if (sprite != null)
            anim = sprite.GetComponent<Animator>();
        if (anim == null)
            anim = GetComponent<Animator>();

        if (staringPosition == Vector2.zero)
            staringPosition = transform.position;
    }

    private void OnEnable()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody2D>();

        if (rb == null)
        {
            Debug.LogWarning(
                "NPC_Wander requires a Rigidbody2D.",
                this
            );

            enabled = false;
            return;
        }

        target = GetRandomTarget();
        BeginPause();
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (isPaused)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Move();

        if (Vector2.Distance(rb.position, target) < 0.1f)
            BeginPause();
    }

    private void Move()
    {
        Vector2 direction =
            (target - rb.position).normalized;

        if (direction.sqrMagnitude < 0.001f)
        {
            rb.velocity = Vector2.zero;
            BeginPause();
            return;
        }

        if ((direction.x > 0f && transform.localScale.x < 0f) ||
            (direction.x < 0f && transform.localScale.x > 0f))
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }

        rb.velocity = direction * speed;
    }

    private void BeginPause()
    {
        if (isPaused || pauseRoutine != null || !isActiveAndEnabled)
            return;

        isPaused = true;

        if (rb != null)
            rb.velocity = Vector2.zero;

        pauseRoutine = StartCoroutine(
            PauseAndPickNewDestination()
        );
    }

    private IEnumerator PauseAndPickNewDestination()
    {
        PlayState("BluePawn_Idle");

        yield return new WaitForSeconds(pauseDuration);

        target = GetRandomTarget();
        isPaused = false;

        PlayState("BluePawn_Walk");
        pauseRoutine = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Do not keep walking into a wall or an unreachable edge.
        BeginPause();
    }

    private void PlayState(string stateName)
    {
        if (anim == null ||
            anim.runtimeAnimatorController == null || !anim.gameObject.activeInHierarchy ||
            !anim.HasState(0, Animator.StringToHash(stateName)))
        {
            return;
        }

        anim.enabled = true;
        anim.Play(stateName, 0);
    }

    private Vector2 GetRandomTarget()
    {
        float halfWidth = wanderWidth * 0.5f;
        float halfHeight = wanderHeight * 0.5f;
        int edge = Random.Range(0, 4);

        switch (edge)
        {
            case 0:
                return new Vector2(
                    staringPosition.x - halfWidth,
                    Random.Range(
                        staringPosition.y - halfHeight,
                        staringPosition.y + halfHeight
                    )
                );

            case 1:
                return new Vector2(
                    staringPosition.x + halfWidth,
                    Random.Range(
                        staringPosition.y - halfHeight,
                        staringPosition.y + halfHeight
                    )
                );

            case 2:
                return new Vector2(
                    Random.Range(
                        staringPosition.x - halfWidth,
                        staringPosition.x + halfWidth
                    ),
                    staringPosition.y - halfHeight
                );

            default:
                return new Vector2(
                    Random.Range(
                        staringPosition.x - halfWidth,
                        staringPosition.x + halfWidth
                    ),
                    staringPosition.y + halfHeight
                );
        }
    }

    private void OnDisable()
    {
        if (rb != null)
            rb.velocity = Vector2.zero;

        if (pauseRoutine != null)
        {
            StopCoroutine(pauseRoutine);
            pauseRoutine = null;
        }

        isPaused = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            staringPosition,
            new Vector3(wanderWidth, wanderHeight, 0f)
        );
    }
}
