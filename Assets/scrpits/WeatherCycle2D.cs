using UnityEngine;

/// <summary>Visual weather on the XY plane; UI, collision and camera projection stay untouched.</summary>
[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(-100)]
[RequireComponent(typeof(WinterWeather2D))]
public sealed class WeatherCycle2D : MonoBehaviour
{
    public enum Weather { Sunny, Rain, Snow }
    [Tooltip("Current target. Change this in Edit mode to preview, or during Play to switch manually.")]
    public Weather currentWeather = Weather.Sunny;
    public bool randomWeather = true;
    [Min(5)] public float minimumDuration = 45;
    [Min(5)] public float maximumDuration = 100;
    [Min(0)] public float transitionSeconds = 6;
    [Min(0)] public float sunnyWeight = 5;
    [Min(0)] public float rainWeight = 3;
    [Min(0)] public float snowWeight = 2;
    [Range(0,2)] public float rainIntensity = 0.85f;
    [SerializeField] private float secondsRemaining;
    public float SecondsRemaining => secondsRemaining;
    public float SnowAmount => snowAmount;
    public float RainAmount => rainAmount;

    private WinterWeather2D winter;
    private ParticleSystem rain;
    private float snowAmount, rainAmount, previousTime;
    private Weather observed;
    private bool initialized;
    // Carry the same weather and countdown across RPG scene loads, without a persistent scene object.
    private static bool hasSession;
    private static Weather sessionWeather;
    private static float sessionRemaining, sessionSnow, sessionRain;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession() => hasSession = false;

    private void OnEnable()
    {
        if (Application.isPlaying && GetComponent<RainFootsteps2D>() == null)
            gameObject.AddComponent<RainFootsteps2D>();
        winter = GetComponent<WinterWeather2D>();
        if (Application.isPlaying && hasSession)
        {
            currentWeather = sessionWeather;
            secondsRemaining = sessionRemaining;
            snowAmount = sessionSnow;
            rainAmount = sessionRain;
        }
        else
        {
            snowAmount = currentWeather == Weather.Snow ? 1 : 0;
            rainAmount = currentWeather == Weather.Rain ? 1 : 0;
            ResetTimer();
        }
        observed = currentWeather;
        previousTime = Time.realtimeSinceStartup;
        initialized = true;
        winter.SetWeather(snowAmount, rainAmount);
    }

    private void OnValidate()
    {
        minimumDuration = Mathf.Max(5, minimumDuration);
        maximumDuration = Mathf.Max(minimumDuration, maximumDuration);
        transitionSeconds = Mathf.Max(0, transitionSeconds);
    }

    private void ResetTimer() => secondsRemaining = Random.Range(Mathf.Max(5,minimumDuration), Mathf.Max(5,Mathf.Max(minimumDuration,maximumDuration)));

    public Weather ChooseNextWeather()
    {
        float sun = currentWeather == Weather.Sunny ? 0 : Mathf.Max(0,sunnyWeight);
        float wet = currentWeather == Weather.Rain ? 0 : Mathf.Max(0,rainWeight);
        float snow = currentWeather == Weather.Snow ? 0 : Mathf.Max(0,snowWeight);
        float total = sun + wet + snow;
        if (total <= 0) return currentWeather;
        float roll = Random.value * total;
        if (sun > 0 && roll < sun) return Weather.Sunny;
        if (wet > 0 && roll < sun + wet) return Weather.Rain;
        return snow > 0 ? Weather.Snow : (wet > 0 ? Weather.Rain : Weather.Sunny);
    }

    public void SetWeather(Weather weather)
    {
        currentWeather = observed = weather;
        ResetTimer();
    }

    [ContextMenu("Preview/Sunny")] private void Sunny() => SetWeather(Weather.Sunny);
    [ContextMenu("Preview/Rain")] private void Rain() => SetWeather(Weather.Rain);
    [ContextMenu("Preview/Snow")] private void Snow() => SetWeather(Weather.Snow);

    public void Advance(float deltaTime)
    {
        if (observed != currentWeather) SetWeather(currentWeather);
        float dt = Mathf.Max(0,deltaTime);
        if (randomWeather)
        {
            secondsRemaining -= dt;
            if (secondsRemaining <= 0) SetWeather(ChooseNextWeather());
        }
        float step = transitionSeconds <= 0 ? 1 : dt / transitionSeconds;
        snowAmount = Mathf.MoveTowards(snowAmount, currentWeather == Weather.Snow ? 1 : 0, step);
        rainAmount = Mathf.MoveTowards(rainAmount, currentWeather == Weather.Rain ? 1 : 0, step);
        winter.SetWeather(snowAmount, rainAmount);
    }

    private void Update()
    {
        if (!initialized) return;
        if (Application.isPlaying)
        {
            Advance(Time.deltaTime);
            hasSession = true;
            sessionWeather = currentWeather;
            sessionRemaining = secondsRemaining;
            sessionSnow = snowAmount;
            sessionRain = rainAmount;
        }
        else
        {
            snowAmount = currentWeather == Weather.Snow ? 1 : 0;
            rainAmount = currentWeather == Weather.Rain ? 1 : 0;
            winter.SetWeather(snowAmount, rainAmount);
        }
        float now = Time.realtimeSinceStartup;
        bool visible = winter.enabled && (Application.isPlaying || winter.previewInEditor);
        if (visible && rainAmount > 0 && rain == null) CreateRain();
        if (rain != null)
        {
            rain.gameObject.SetActive(visible);
            var camera = Camera.main;
            float height = camera != null && camera.orthographic ? camera.orthographicSize*2 : 14;
            float width = height*(camera != null ? camera.aspect : 1.7778f);
            Vector3 center = camera != null ? camera.transform.position : Vector3.zero;
            rain.transform.position = new Vector3(center.x,center.y+2,0);
            var shape = rain.shape;
            shape.scale = new Vector3(width+8,height+10,0.01f);
            var emission = rain.emission;
            emission.rateOverTime = visible ? (width+8)*(height+10)*rainIntensity*rainAmount*0.65f : 0;
            var velocity = rain.velocityOverLifetime;
            velocity.x = winter.wind*2-1;
            if (!Application.isPlaying && visible) rain.Simulate(Mathf.Clamp(now-previousTime,0,0.1f),false,false);
        }
        previousTime = now;
    }

    private void CreateRain()
    {
        var material = Resources.Load<Material>("PixelRain");
        if (material == null) return;
        var child = new GameObject("Rain streaks") { hideFlags = HideFlags.HideAndDontSave };
        child.transform.SetParent(transform,false);
        rain = child.AddComponent<ParticleSystem>();
        rain.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = rain.main;
        main.loop = true;
        main.duration = 3;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f,0.8f);
        main.startSpeed = 0;
        main.startSize = new ParticleSystem.MinMaxCurve(0.24f,0.42f);
        main.startColor = new Color(0.69f,0.84f,0.94f,0.62f);
        main.maxParticles = 2400;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        var shape = rain.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        var velocity = rain.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = -12;
        var color = rain.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1) },
            new[] { new GradientAlphaKey(0,0),new GradientAlphaKey(1,0.1f),new GradientAlphaKey(1,0.75f),new GradientAlphaKey(0,1) });
        color.color = fade;
        var renderer = rain.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 24003;
        rain.Play();
    }

    private void OnDisable()
    {
        if (winter != null) winter.SetWeather(0,0);
        if (rain != null)
        {
            rain.gameObject.SetActive(false);
            if (Application.isPlaying) Destroy(rain.gameObject); else DestroyImmediate(rain.gameObject);
        }
        rain = null;
        initialized = false;
    }
}
