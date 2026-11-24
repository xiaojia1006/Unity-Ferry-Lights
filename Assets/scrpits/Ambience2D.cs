using UnityEngine;

/// <summary>Looping weather bed. The rain layer follows RainAmount so it swells and fades with the
/// sky instead of starting abruptly, and it respects the ambient/effects volume in the settings.</summary>
[DefaultExecutionOrder(205)]
public sealed class Ambience2D : MonoBehaviour
{
    public float maximumVolume = .7f;

    static Ambience2D instance;
    WeatherCycle2D weather;
    AudioSource source;
    AudioClip rainClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        new GameObject("Weather ambience").AddComponent<Ambience2D>();
    }

    void Awake()
    {
        if (instance != null) { Destroy(gameObject); return; }
        instance = this; DontDestroyOnLoad(gameObject);

        rainClip = Resources.Load<AudioClip>("Audio/amb_rain");
        if (rainClip == null) Debug.LogWarning("Ambience2D: Resources/Audio/amb_rain 没有导入成 AudioClip，雨声不会播放。");
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false; source.loop = true; source.spatialBlend = 0f;
        source.clip = rainClip; source.volume = 0f;
        source.ignoreListenerPause = true;
    }

    void Update()
    {
        if (weather == null) weather = FindObjectOfType<WeatherCycle2D>();

        // Debug weather keys: F6 clear, F7 rain, F8 snow. Nothing else in the project uses them.
        if (weather != null)
        {
            if (Input.GetKeyDown(KeyCode.F7)) weather.SetWeather(WeatherCycle2D.Weather.Rain);
            else if (Input.GetKeyDown(KeyCode.F8)) weather.SetWeather(WeatherCycle2D.Weather.Snow);
            else if (Input.GetKeyDown(KeyCode.F6)) weather.SetWeather(WeatherCycle2D.Weather.Sunny);
        }

        float rain = weather != null ? weather.RainAmount : 0f;
        float effects = GlobalSettings.EnvironmentVolume;
        float target = Mathf.Clamp01(rain * maximumVolume * effects);

        // Unscaled so the rain does not stop dead while a menu is open.
        source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * .8f);
        if (rainClip == null) return;
        if (source.volume > .002f && !source.isPlaying) source.Play();
        else if (source.volume <= .002f && source.isPlaying) source.Pause();
    }
}
