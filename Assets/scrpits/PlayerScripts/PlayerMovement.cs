using UnityEngine;
using System.Collections.Generic;

public class PlayerMovement : MonoBehaviour
{
    
    public int facingDirection = 1;
    /// <summary>最后一次有效移动方向（含上下），钓鱼抛竿方向用它，而不是只有左右的 facingDirection。</summary>
    public Vector2 lastMoveDir = Vector2.right;
    public Rigidbody2D rb;
    public Animator anim;

    [SerializeField] private float knockbackForce = 6f;
    [SerializeField] private float knockbackDuration = 0.15f;

    private float knockbackTimer;
    private readonly Dictionary<WaterSurface2D, float> waterSurfaces = new Dictionary<WaterSurface2D, float>();
    private float waterSpeedMultiplier = 1f;
    public bool IsWading => waterSurfaces.Count > 0;
    public bool isShooting;
    public player_combat player_Combat;
    private void Update()
    {
        var moveInput = GlobalSettings.Movement();
        if (moveInput.sqrMagnitude > 0.01f) lastMoveDir = moveInput.normalized;
        if (Time.timeScale > 0f && player_Combat != null && player_Combat.isActiveAndEnabled && Input.GetButtonDown("Slash")) 
        {
            player_Combat.Attack();
        }
    }

    private void FixedUpdate()
    {
        // 钓鱼时角色站定：不移动、不播行走动画，只显示持竿姿势
        if (FishingRig2D.Busy)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            return;
        }
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

        // 冲刺消耗体力，停下来回复；背包超重会拖慢移动
        bool wantsSprint = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                           && Mathf.Abs(horizontal) + Mathf.Abs(vertical) > .01f;
        float sprintMul = 1f;
        if (wantsSprint && PlayerStats.CanSprint)
        {
            sprintMul = 1.55f;
            PlayerStats.DrainStamina(26f * Time.fixedDeltaTime);
        }
        else
        {
            PlayerStats.RegenStamina(14f * Time.fixedDeltaTime);
        }
        if (PlayerStats.Overloaded) sprintMul *= .85f;

        rb.velocity = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f) * StatsManager.Instance.speed * waterSpeedMultiplier * sprintMul;
    }

    // 让角色转向某个世界坐标（钓鱼时面向水面）
    public void FaceTowards(float worldX)
    {
        int dir = worldX < transform.position.x ? -1 : 1;
        if (dir == facingDirection) return;
        facingDirection = dir;
        transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * dir, transform.localScale.y, transform.localScale.z);
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
