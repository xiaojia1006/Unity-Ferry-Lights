using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class NPC_Patorl : MonoBehaviour
{
    public Vector2[] patroPints;
    public float speed = 2f;
    public int currentPatrolIndex;
    public Vector2 direction;
    public float pauseDuration = 1.5f;

    private Vector2 target;
    private bool isPaused;
    private Rigidbody2D rb;
    private Animator anim;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        Transform sprite = transform.Find("Sprite");

        if (sprite != null)
            anim = sprite.GetComponent<Animator>();
        if (anim == null)
            anim = GetComponent<Animator>();

        if (rb == null ||
            patroPints == null ||
            patroPints.Length == 0)
        {
            Debug.LogWarning(
                "NPC patrol requires a Rigidbody2D and at least one patrol point.",
                this
            );

            enabled = false;
            return;
        }

        currentPatrolIndex = Mathf.Clamp(
            currentPatrolIndex,
            0,
            patroPints.Length - 1
        );

        target = patroPints[currentPatrolIndex];
        PlayState("BluePawn_Walk");
    }

    private void FixedUpdate()
    {
        if (rb == null ||
            patroPints == null ||
            patroPints.Length == 0)
        {
            return;
        }

        if (isPaused)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        direction = (target - rb.position).normalized;

        if (direction.sqrMagnitude < 0.001f)
        {
            rb.velocity = Vector2.zero;
            BeginPause();
            return;
        }

        rb.velocity = direction * speed;

        if ((direction.x < 0f && transform.localScale.x > 0f) ||
            (direction.x > 0f && transform.localScale.x < 0f))
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1f;
            transform.localScale = scale;
        }

        if (Vector2.Distance(rb.position, target) < 0.1f)
            BeginPause();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // A wall or other obstacle means the selected point is unreachable.
        BeginPause();
    }

    private void BeginPause()
    {
        if (isPaused || !isActiveAndEnabled)
            return;

        isPaused = true;

        if (rb != null)
            rb.velocity = Vector2.zero;

        StartCoroutine(PauseAndChooseNextPoint());
    }

    private IEnumerator PauseAndChooseNextPoint()
    {
        PlayState("BluePawn_Idle");

        yield return new WaitForSeconds(pauseDuration);

        if (patroPints != null && patroPints.Length > 0)
        {
            currentPatrolIndex =
                (currentPatrolIndex + 1) % patroPints.Length;

            target = patroPints[currentPatrolIndex];
        }

        isPaused = false;
        PlayState("BluePawn_Walk");
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

    private void OnDisable()
    {
        StopAllCoroutines();
        isPaused = false;

        if (rb != null)
            rb.velocity = Vector2.zero;
    }
}
