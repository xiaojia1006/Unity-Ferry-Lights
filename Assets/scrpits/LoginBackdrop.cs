using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Dresses the login village: a star field with twinkling points, moonlight, a fire glow that
/// breathes, drifting mist, floating motes and a foreground tree frame. Everything is built at runtime
/// and inserted just under the parchment panel, so the authored scene stays untouched.</summary>
public sealed class LoginBackdrop : MonoBehaviour
{
    public static bool Enabled = true;

    struct Mote { public RectTransform rt; public float speed, sway, phase, baseX; }
    struct Band { public RectTransform rt; public float speed, baseX; }
    struct Star { public RectTransform rt; public float hz, phase, amp, dim, flareIn, flash; }

    readonly List<Mote> motes = new List<Mote>();
    readonly List<Band> bands = new List<Band>();
    readonly List<Star> stars = new List<Star>();

    RectTransform glow;
    Image glowImage;
    RectTransform moon;
    Canvas canvas;
    float clock;
    AudioSource chime;
    AudioClip[] chimes;
    float nextChime = 1.5f, lastChime = -9f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!Enabled || scene.name != "Login") return;
        var runner = new GameObject("Login Backdrop").AddComponent<LoginBackdrop>();
        runner.StartCoroutine(BuildNextFrame(runner));
    }

    static System.Collections.IEnumerator BuildNextFrame(LoginBackdrop runner)
    {
        yield return null;
        try { runner.Build(); }
        catch (System.Exception e) { Debug.LogWarning("LoginBackdrop: " + e.Message); }
    }

    internal static Sprite LoadSprite(string name)
    {
        var s = Resources.Load<Sprite>("Login/" + name);
        if (s != null) return s;
        var tex = Resources.Load<Texture2D>("Login/" + name);
        if (tex == null) return null;
        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f), 100f);
    }

    static GameObject Find(string name)
    {
        foreach (var t in Object.FindObjectsOfType<Transform>(true))
            if (t.name == name) return t.gameObject;
        return null;
    }

    /// <summary>Top-level object that owns the parchment. Decorations are appended to the canvas and
    /// the panel is pushed to the front afterwards, so insert order can never bury them.</summary>
    Transform PanelRoot()
    {
        var panel = Find("Parchment");
        if (panel == null) return null;
        var t = panel.transform;
        while (t.parent != null && t.parent != canvas.transform) t = t.parent;
        return t;
    }

    void Build()
    {
        var canvasGo = Find("Login Canvas");
        if (canvasGo == null) { Destroy(gameObject); return; }
        canvas = canvasGo.GetComponent<Canvas>();
        if (canvas == null) { Destroy(gameObject); return; }

        int baseIndex = 0;
        int order = 0;

        // 1. Star field, then the moon and its halo on top of it.
        Make("Star field", LoadSprite("starfield"), new Vector2(1920, 540), new Vector2(0, 300), new Color(1f, 1f, 1f, .95f), ref baseIndex, ref order);
        // Real moonlight is a warm grey, not white: the disc only reflects sunlight.
        // The glow is baked into the moon sprite itself, so no second crescent can appear.
        Make("Moon haze", LoadSprite("glow"), new Vector2(380, 380), new Vector2(-560, 360), new Color(1f, .90f, .74f, .04f), ref baseIndex, ref order);
        moon = Make("Moon", LoadSprite("moon"), new Vector2(132, 132), new Vector2(-560, 360), new Color(1f, .96f, .88f, .95f), ref baseIndex, ref order);

        // 2. Warm haze at the horizon so the village sits in air, not on a flat plate.
        Make("Horizon haze", LoadSprite("horizon"), new Vector2(1700, 760), new Vector2(-140, -70), new Color(1f, .93f, .8f, .10f), ref baseIndex, ref order);

        // 3. Twinkling points above the sky band.
        BuildStars(ref baseIndex, ref order);

        // 4. Fire glow, anchored to the campfire the scene already has.
        var login = Object.FindObjectOfType<LoginScreen>();
        Vector2 fireAt = new Vector2(-330, -250);
        if (login != null && login.campfire != null)
            fireAt = (Vector2)canvas.transform.InverseTransformPoint(login.campfire.rectTransform.position) + new Vector2(0, 30);
        glow = Make("Campfire glow", LoadSprite("glow"), new Vector2(820, 820), fireAt, new Color(1f, .74f, .42f, .30f), ref baseIndex, ref order);
        glowImage = glow.GetComponent<Image>();

        // 5. Three mist bands near the ground, each drifting at its own speed.
        float[] ys = { -300f, -230f, -150f };
        float[] alphas = { .07f, .05f, .09f };
        float[] speeds = { 6f, 4f, 9f };
        for (int i = 0; i < ys.Length; i++)
        {
            var rt = Make("Mist " + i, LoadSprite("mist"), new Vector2(2200, 380), new Vector2(0, ys[i]),
                new Color(1f, 1f, 1f, alphas[i]), ref baseIndex, ref order);
            bands.Add(new Band { rt = rt, speed = speeds[i] * (i % 2 == 0 ? 1f : -1f), baseX = rt.anchoredPosition.x });
        }

        // 6. Floating motes rising through the firelight.
        var rng = new System.Random(21);
        for (int i = 0; i < 24; i++)
        {
            float x = Mathf.Lerp(-780f, 420f, (float)rng.NextDouble());
            float y = Mathf.Lerp(-380f, 320f, (float)rng.NextDouble());
            float size = Mathf.Lerp(8f, 18f, (float)rng.NextDouble());
            var rt = Make("Mote " + i, LoadSprite("mote"), new Vector2(size, size), new Vector2(x, y),
                new Color(1f, .9f, .66f, .0f), ref baseIndex, ref order);
            motes.Add(new Mote { rt = rt, speed = Mathf.Lerp(7f, 16f, (float)rng.NextDouble()), sway = Mathf.Lerp(6f, 22f, (float)rng.NextDouble()), phase = (float)rng.NextDouble() * 6.28f, baseX = x });
        }

        // 8. Tiny chimes that follow the star flares.
        var audioGo = new GameObject("Star twinkle");
        audioGo.transform.SetParent(transform, false);
        chime = audioGo.AddComponent<AudioSource>();
        chime.playOnAwake = false; chime.loop = false; chime.spatialBlend = 0f;
        chimes = new[] { Resources.Load<AudioClip>("Login/twinkle_1"), Resources.Load<AudioClip>("Login/twinkle_2"), Resources.Load<AudioClip>("Login/twinkle_3") };

        // Everything above is now on top of the canvas; the parchment must stay in front of it.
        var root = PanelRoot();
        if (root != null) root.SetAsLastSibling();
    }

    // Twinkling points: small dots that breathe, plus a few four-point stars that flare now and then.
    void BuildStars(ref int baseIndex, ref int order)
    {
        var rng = new System.Random(53);
        for (int i = 0; i < 26; i++)
        {
            bool big = i < 7;
            float x = Mathf.Lerp(-900f, 880f, (float)rng.NextDouble());
            float y = Mathf.Lerp(70f, 500f, (float)rng.NextDouble());
            if (x > 250f && y < 270f) x = -x;          // keep clear of the parchment panel
            float size = big ? Mathf.Lerp(26f, 46f, (float)rng.NextDouble()) : Mathf.Lerp(8f, 16f, (float)rng.NextDouble());

            var sprite = LoadSprite(big ? "glint" : "mote");
            if (sprite == null) continue;
            var rt = Make("Star " + i, sprite, new Vector2(size, size), new Vector2(x, y),
                new Color(1f, .97f, .85f, .2f), ref baseIndex, ref order);

            stars.Add(new Star
            {
                rt = rt,
                hz = Mathf.Lerp(.25f, .95f, (float)rng.NextDouble()),
                phase = (float)rng.NextDouble() * 6.28f,
                amp = big ? Mathf.Lerp(.35f, .55f, (float)rng.NextDouble()) : Mathf.Lerp(.22f, .45f, (float)rng.NextDouble()),
                dim = big ? .34f : .24f,
                flareIn = Mathf.Lerp(1.5f, 7f, (float)rng.NextDouble()),
                flash = 0f
            });
        }
    }

    RectTransform Make(string name, Sprite sprite, Vector2 size, Vector2 pos, Color color, ref int baseIndex, ref int order)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(canvas.transform, false);
        rt.anchorMin = rt.anchorMax = new Vector2(.5f, .5f);
        rt.pivot = new Vector2(.5f, .5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        rt.SetAsLastSibling();
        order++;

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        img.preserveAspect = true;
        return rt;
    }

    void PlayChime(float gain)
    {
        if (chime == null || chimes == null) return;
        if (Time.unscaledTime - lastChime < .35f) return;
        var clip = chimes[Random.Range(0, chimes.Length)];
        if (clip == null) return;
        lastChime = Time.unscaledTime;
        float effects = GlobalSettings.Instance != null ? GlobalSettings.Instance.Data.effects : 1f;
        chime.pitch = Random.Range(.97f, 1.05f);
        chime.PlayOneShot(clip, Mathf.Clamp01(.18f * gain * effects));
    }

    void Update()
    {
        clock += Time.unscaledDeltaTime;
        float t = clock;

        if (glowImage != null && glow != null)
        {
            float flicker = .28f + Mathf.Sin(t * 2.1f) * .05f + Mathf.Sin(t * 6.7f) * .018f;
            var c = glowImage.color; c.a = flicker; glowImage.color = c;
            float s = 1f + Mathf.Sin(t * 2.1f) * .015f;
            glow.localScale = new Vector3(s, s, 1f);
        }

        if (moon != null)
        {
            float s = 1f + Mathf.Sin(t * .35f) * .006f;   // a far object, so it barely breathes
            moon.localScale = new Vector3(s, s, 1f);
        }

        // Twinkle: slow breathing plus a rare bright flare so the sky never looks looped.
        for (int i = 0; i < stars.Count; i++)
        {
            var st = stars[i];
            if (st.rt == null) continue;
            var img = st.rt.GetComponent<Image>();
            if (img == null) continue;

            float breathe = Mathf.Sin(t * st.hz * 6.283f + st.phase);
            float a = st.dim + st.amp * breathe;

            if (st.flash > 0f)
            {
                st.flash -= Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(st.flash / .5f);
                a += Mathf.Sin(k * Mathf.PI) * .7f;
                float sc = Mathf.Lerp(1.7f, .9f, k);
                st.rt.localScale = new Vector3(sc, sc, 1f);
            }
            else
            {
                st.flareIn -= Time.unscaledDeltaTime;
                st.rt.localScale = Vector3.one;
                if (st.flareIn <= 0f)
                {
                    st.flash = .5f;
                    st.flareIn = Random.Range(3.5f, 11f);
                    if (i < 7 && Random.value < .5f) PlayChime(.9f);
                }
            }

            var c = img.color;
            c.a = Mathf.Clamp01(a);
            img.color = c;

            stars[i] = st;      // struct copy: the timers must be written back
        }

        nextChime -= Time.unscaledDeltaTime;
        if (nextChime <= 0f) { PlayChime(.6f); nextChime = Random.Range(1.4f, 3.8f); }

        for (int i = 0; i < bands.Count; i++)
        {
            var b = bands[i];
            if (b.rt == null) continue;
            var p = b.rt.anchoredPosition;
            p.x = b.baseX + Mathf.Repeat(t * b.speed, 220f) - 110f;
            b.rt.anchoredPosition = p;
        }

        for (int i = 0; i < motes.Count; i++)
        {
            var m = motes[i];
            if (m.rt == null) continue;
            var p = m.rt.anchoredPosition;
            p.y += m.speed * Time.unscaledDeltaTime;
            if (p.y > 360f) { p.y = -400f; p.x = Mathf.Lerp(-780f, 420f, UnityEngine.Random.value); }
            p.x = m.baseX + Mathf.Sin(t * .55f + m.phase) * m.sway;
            m.rt.anchoredPosition = p;

            float fade = Mathf.Sin(Mathf.InverseLerp(-400f, 360f, p.y) * Mathf.PI);
            var img = m.rt.GetComponent<Image>();
            if (img != null) { var c = img.color; c.a = fade * .5f; img.color = c; }
        }
    }
}
