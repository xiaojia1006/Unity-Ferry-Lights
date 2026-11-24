using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Picks one track for the current day phase and weather, and hands it to the settings
/// music source. Clips load straight from Resources/Audio/BGM, so swapping a track is a file copy.</summary>
[DefaultExecutionOrder(200)]
public sealed class MusicDirector : MonoBehaviour
{
    static MusicDirector instance;
    public static MusicDirector Instance => instance;

    AudioClip dayClear, dayRain, daySnow, nightClear, nightRain, nightSnow, ruinsClip;
    WeatherCycle2D weather;
    PlayerMovement player;
    Transform ruins;
    AudioClip current;
    float nextLookup;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        new GameObject("Music Director").AddComponent<MusicDirector>();
    }

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this; DontDestroyOnLoad(gameObject);
        dayClear = Load("day_clear");
        dayRain = Load("day_rain");
        daySnow = Load("day_snow");
        nightClear = Load("night_clear");
        nightRain = Load("night_rain");
        nightSnow = Load("night_snow");
        ruinsClip = Load("ruins");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static AudioClip Load(string name) => Resources.Load<AudioClip>("Audio/BGM/" + name);

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        weather = null; player = null; ruins = null; current = null;
        nextLookup = 0f;
        Refresh();
    }

    void Update()
    {
        if (Time.unscaledTime < nextLookup) return;
        nextLookup = Time.unscaledTime + .5f;
        Refresh();
    }

    public void Refresh()
    {
        var settings = GlobalSettings.Instance;
        if (settings == null) return;

        // The login screen keeps its own calm theme instead of a weather track.
        if (SceneManager.GetActiveScene().name == "Login")
        {
            settings.WorldDuck = 1f;
            if (current != null) { current = null; settings.ReleaseWorldMusic(this); }
            return;
        }

        if (weather == null) weather = FindObjectOfType<WeatherCycle2D>();
        if (player == null) player = FindObjectOfType<PlayerMovement>();
        if (ruins == null) CacheRuins();

        bool night = DayNight2D.Instance != null && DayNight2D.Instance.Current == DayNight2D.Phase.Night;
        bool raining = weather != null && weather.RainAmount > .5f;
        bool snowing = weather != null && weather.SnowAmount > .5f;

        AudioClip clip = night
            ? (snowing ? nightSnow : raining ? nightRain : nightClear)
            : (snowing ? daySnow : raining ? dayRain : dayClear);
        if (clip == null) clip = dayClear;

        // Standing at the ruins at night swaps in the watchful theme.
        if (night && ruinsClip != null && ruins != null && player != null &&
            Vector2.Distance(player.transform.position, ruins.position) < (current == ruinsClip ? 11f : 9f))
            clip = ruinsClip;

        // Rain and snow pull the score back so the weather bed and the footsteps lead.
        settings.WorldDuck = raining ? .4f : snowing ? .55f : 1f;
        if (clip == null || clip == current) return;
        current = clip;
        settings.SetWorldMusic(this, clip);
    }

    void CacheRuins()
    {
        ruins = null;
        foreach (var point in FindObjectsOfType<RPG2Point>())
            if (point != null && point.kind == RPG2Point.Kind.Treasure) { ruins = point.transform; return; }
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }
}
