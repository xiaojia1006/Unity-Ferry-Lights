using UnityEngine;

/// <summary>Rod, line and float shown while the player is fishing. Rod bend, float dip and the
/// pull-out splash are authored sprite frames; only the aim angle is calculated.</summary>
public sealed class FishingRig2D : MonoBehaviour
{
    static FishingRig2D active;
    static Sprite[] splashFrames;

    Transform bobber;
    SpriteRenderer body;
    Sprite bodySprite;
    Animator animator;
    bool hadAnimator;
    LineRenderer line;
    SpriteRenderer floatRenderer;
    Sprite[] floatFrames;
    static Sprite holdPose, liftPose, holdUp, liftUp;
    Vector3 target;
    Transform owner;
    float clock;
    bool biting;

    public static bool Busy { get { return active != null; } }

    public static void Begin(Transform player, Vector3 waterPoint)
    {
        End();
        if (player == null) return;
        // Kept at scene root: the player flips localScale.x when turning, and a child rig would
        // inherit that mirror and swing the rod the wrong way.
        var go = new GameObject("Fishing rig");
        active = go.AddComponent<FishingRig2D>();
        active.owner = player;
        active.transform.position = player.position;
        active.target = waterPoint;
        active.Build();
        active.WearPose(player);
    }

    public static void SetBite(bool value)
    {
        if (active != null) active.biting = value;
    }

    public static void End()
    {
        if (active == null) return;
        active.RestorePose();
        Destroy(active.gameObject);
        active = null;
    }

    void OnDisable(){RestorePose();if(active==this)active=null;}
    void OnDestroy(){RestorePose();if(active==this)active=null;}

    static Sprite PoseSprite(bool lift, bool up)
    {
        // 朝上 / 朝左右 是两套帧：角色只有水平朝向的贴图，不能让竿斜着指向错误方向
        if (up)
        {
            if (!lift && holdUp != null) return holdUp;
            if (lift && liftUp != null) return liftUp;
            var texUp = Resources.Load<Texture2D>(lift ? "Archer_Fishing_Up_Lift" : "Archer_Fishing_Up_Hold");
            if (texUp == null) return holdPose;
            var sUp = Sprite.Create(texUp, new Rect(0, 0, texUp.width, texUp.height), new Vector2(.5f, .5f), 64f);
            if (lift) liftUp = sUp; else holdUp = sUp;
            return sUp;
        }
        if (!lift && holdPose != null) return holdPose;
        if (lift && liftPose != null) return liftPose;
        var tex = Resources.Load<Texture2D>(lift ? "Archer_Fishing_Lift" : "Archer_Fishing_Hold");
        if (tex == null) return holdPose;
        var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 64f);
        if (lift) liftPose = sprite; else holdPose = sprite;
        return sprite;
    }

    // Swap the body sprite for the authored fishing frame and freeze the animator, so the bow does
    // not stay in the hands while the rod is out.
    void WearPose(Transform player)
    {
        body = player.GetComponentInChildren<SpriteRenderer>();
        animator = player.GetComponent<Animator>();
        if (animator == null) animator = player.GetComponentInChildren<Animator>();
        if (animator != null) { hadAnimator = animator.enabled; animator.enabled = false; }
        var pose = PoseSprite(false, false);
        if (body != null && pose != null)
        {
            bodySprite = body.sprite;
            body.sprite = pose;
            // The player already mirrors with localScale.x, so the sprite needs no flip here.
        }
    }

    void RestorePose()
    {
        if (body != null && bodySprite != null) body.sprite = bodySprite;
        if (animator != null) animator.enabled = hadAnimator;
    }

    /// <summary>One-shot droplets where the float broke the surface.</summary>
    public static void Splash(Vector3 at)
    {
        if (splashFrames == null || splashFrames.Length == 0) splashFrames = FishingArt.Sheet("splash_sheet", 5, 64f);
        if (splashFrames.Length == 0) return;
        var go = new GameObject("Fishing splash");
        go.transform.position = at;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 24004;
        go.AddComponent<FishingSplash>().Frames = splashFrames;
    }

    void Build()
    {
        floatFrames = FishingArt.Sheet("float_sheet", 4, 64f);
        splashFrames = FishingArt.Sheet("splash_sheet", 5, 64f);

        var lineGo = new GameObject("Line");
        lineGo.transform.SetParent(transform, false);
        line = lineGo.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startWidth = .022f;
        line.endWidth = .016f;
        line.startColor = new Color(1f, 1f, 1f, .8f);
        line.endColor = new Color(1f, 1f, 1f, .45f);
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.sortingOrder = 24001;

        if (floatFrames.Length > 0)
        {
            var bobGo = new GameObject("Float");
            bobGo.transform.SetParent(transform, false);
            floatRenderer = bobGo.AddComponent<SpriteRenderer>();
            floatRenderer.sprite = floatFrames[0];
            floatRenderer.sortingOrder = 24003;
            bobber = bobGo.transform;
            bobber.position = target;
        }
    }

    void Update()
    {
        clock += Time.deltaTime;
        if (owner == null) { End(); return; }
        transform.position = owner.position;

        var player = owner.GetComponent<PlayerMovement>();
        float facing = player != null && player.facingDirection != 0 ? player.facingDirection : 1f;

        // 抛竿瞬间和咬钩时抬竿，其余时间持竿等待；竿尖位置跟着两帧的手位走
        bool liftNow = clock < .35f || biting;
        // 抛竿方向决定用哪套姿势：竖直向上还是水平
        Vector2 toTarget2 = (Vector2)(target - transform.position);
        bool facingUp = toTarget2.y > Mathf.Abs(toTarget2.x) * .9f;
        if (body != null)
        {
            var pose = PoseSprite(liftNow, facingUp);
            if (pose != null) body.sprite = pose;
        }

        Vector3 tip = facingUp
            ? transform.position + new Vector3(facing * .18f, liftNow ? 1.45f : 1.30f, 0f)
            : transform.position + new Vector3(facing * (liftNow ? 1.02f : 1.16f), liftNow ? .78f : .53f, 0f);
        if (biting) tip += new Vector3(facing * .05f, -.03f, 0f);
        Vector3 floatPos = target + new Vector3(0f, Mathf.Sin(clock * 2.6f) * .03f, 0f);
        if (line != null)
        {
            line.SetPosition(0, tip);
            line.SetPosition(1, floatPos);
        }
        if (bobber != null)
        {
            bobber.position = floatPos;
            if (floatFrames.Length >= 4)
                floatRenderer.sprite = biting ? floatFrames[3] : (Mathf.Repeat(clock, 1.4f) < .7f ? floatFrames[0] : floatFrames[1]);
        }
    }
}

/// <summary>Plays the splash sheet once and removes itself.</summary>
public sealed class FishingSplash : MonoBehaviour
{
    public Sprite[] Frames;
    float clock;
    SpriteRenderer renderer2D;

    void Awake()
    {
        renderer2D = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (Frames == null || Frames.Length == 0 || renderer2D == null) { Destroy(gameObject); return; }
        clock += Time.deltaTime;
        int index = Mathf.FloorToInt(clock * 14f);
        if (index >= Frames.Length) { Destroy(gameObject); return; }
        renderer2D.sprite = Frames[index];
    }
}
