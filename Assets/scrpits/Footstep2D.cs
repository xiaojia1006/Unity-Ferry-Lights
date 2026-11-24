using System.Collections.Generic;
using UnityEngine;

/// <summary>Footsteps on grass, snow, stone and water. Timing is driven by a step interval rather than
/// a distance, because the player walks at five units per second and any short distance turned the
/// steps into a machine-gun rattle. Snow runs through a low-pass so the crunch reads as soft powder.</summary>
[DefaultExecutionOrder(210)]
public sealed class Footstep2D : MonoBehaviour
{
    [Tooltip("关掉后完全没有脚步音，只留环境声与拾取反馈。")] public bool enableFootsteps = true;
    public float stepInterval = .46f;
    public float intervalJitter = .05f;
    public float grassVolume = .12f;
    public float snowVolume = .1f;
    public float stoneVolume = .14f;
    public float waterVolume = .18f;
    public float snowCutoff = 0f;   // 合成雪声已经够闷，不再叠加滤波

    static Footstep2D instance;
    PlayerMovement player;
    WeatherCycle2D weather;
    AudioSource drySource, snowSource;

    AudioClip[] grassSteps, snowSteps, stoneSteps, waterSteps;
    int lastGrass = -1, lastSnow = -1, lastStone = -1, lastWater = -1;

    float nextStepAt;
    Vector2 lastPosition;
    bool placed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        new GameObject("Footsteps").AddComponent<Footstep2D>();
    }

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this; DontDestroyOnLoad(gameObject);

        drySource = MakeSource("Dry steps", 0f);
        snowSource = MakeSource("Snow steps", snowCutoff);

        var all = Resources.LoadAll<AudioClip>("Audio/Steps");
        grassSteps = Collect(all, "foot_grass");
        snowSteps = Collect(all, "foot_snow");
        stoneSteps = Collect(all, "foot_stone");
        waterSteps = Collect(all, "foot_water");
    }

    AudioSource MakeSource(string name, float cutoff)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var src = go.AddComponent<AudioSource>();
        src.playOnAwake = false; src.loop = false; src.spatialBlend = 0f;
        if (cutoff > 0f)
        {
            var filter = go.AddComponent<AudioLowPassFilter>();
            filter.cutoffFrequency = cutoff;
            filter.lowpassResonanceQ = .7f;
        }
        return src;
    }

    static AudioClip[] Collect(AudioClip[] all, string prefix)
    {
        var found = new List<AudioClip>();
        foreach (var clip in all) if (clip != null && clip.name.StartsWith(prefix)) found.Add(clip);
        return found.ToArray();
    }

    void Update()
    {
        if (!enableFootsteps) return;
        if (Time.timeScale <= 0f) { placed = false; return; }
        if (player == null) player = FindObjectOfType<PlayerMovement>();
        if (player == null) return;

        Vector2 now = player.transform.position;
        if (!placed) { lastPosition = now; placed = true; nextStepAt = Time.time + stepInterval; return; }

        float moved = Vector2.Distance(now, lastPosition);
        lastPosition = now;
        if (moved > 1.6f) { nextStepAt = Time.time + stepInterval; return; }   // teleport
        if (moved < .002f) return;                                            // standing still
        if (Time.time < nextStepAt) return;

        nextStepAt = Time.time + stepInterval * Random.Range(1f - intervalJitter, 1f + intervalJitter);
        Step(player.IsWading);
    }

    void Step(bool wading)
    {
        if (weather == null) weather = FindObjectOfType<WeatherCycle2D>();
        float effects = (GlobalSettings.Instance != null ? GlobalSettings.Instance.Data.effects : 1f) * GlobalSettings.FootstepVolume;

        if (wading && waterSteps.Length > 0)
        {
            Play(drySource, waterSteps, ref lastWater, waterVolume * effects, .95f, 1.02f);
            return;
        }
        bool snowing = weather != null && weather.SnowAmount > .5f;
        if (snowing && snowSteps.Length > 0)
        {
            // Soft powder, not a gravel crunch: gentle pitch and the low-pass on this source.
            Play(snowSource, snowSteps, ref lastSnow, snowVolume * effects, .86f, .95f);
            return;
        }
        if (grassSteps.Length > 0) Play(drySource, grassSteps, ref lastGrass, grassVolume * effects, .97f, 1.03f);
    }

    static void Play(AudioSource source, AudioClip[] bank, ref int last, float volume, float pitchLow, float pitchHigh)
    {
        if (source == null || bank == null || bank.Length == 0) return;
        int index = bank.Length == 1 ? 0 : (last + Random.Range(1, bank.Length)) % bank.Length;
        last = index;
        source.pitch = Random.Range(pitchLow, pitchHigh);
        source.PlayOneShot(bank[index], Mathf.Clamp01(volume));
    }
}
