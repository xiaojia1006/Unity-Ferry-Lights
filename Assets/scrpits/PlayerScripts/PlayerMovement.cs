using UnityEngine;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    
    public int facingDirection = 1;
    public Rigidbody2D rb;
    public Animator anim;

    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDuration = 0.15f;

    private float knockbackTimer;
    private readonly Dictionary<WaterSurface2D, float> waterSurfaces = new Dictionary<WaterSurface2D, float>();
    private float waterSpeedMultiplier = 1f;
    public bool isShooting;
    public player_combat player_Combat;
    private void Update()
    {
        if (Time.timeScale > 0f && player_Combat != null && player_Combat.isActiveAndEnabled && Input.GetButtonDown("Slash")) 
        {
            player_Combat.Attack();
        }
    }

    private void FixedUpdate()
    {
        if (isShooting == true)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        else if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            return;
        }

        Vector2 movement = GlobalSettings.Movement();
        float horizontal = movement.x;
        float vertical = movement.y;

        anim.SetFloat("horizontal", Mathf.Abs(horizontal));
        anim.SetFloat("vertical", Mathf.Abs(vertical));

        if (
            horizontal > 0f && transform.localScale.x < 0f ||
            horizontal < 0f && transform.localScale.x > 0f
        )
        {
            Flip();
        }

        rb.velocity = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f) * StatsManager.Instance.speed * waterSpeedMultiplier;
    }

    public void SetWaterState(WaterSurface2D source, bool entering, float multiplier)
    {
        if (source == null) return;
        if (entering)
            waterSurfaces[source] = Mathf.Clamp(multiplier, 0.1f, 1f);
        else
            waterSurfaces.Remove(source);

        waterSpeedMultiplier = 1f;
        foreach (var speed in waterSurfaces.Values)
            waterSpeedMultiplier = Mathf.Min(waterSpeedMultiplier, speed);
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

    public void Knockback(Transform enemy)
    {
        Vector2 direction =
            ((Vector2)transform.position - (Vector2)enemy.position).normalized;

        rb.velocity = direction * knockbackForce;
        knockbackTimer = knockbackDuration;
    }
}
