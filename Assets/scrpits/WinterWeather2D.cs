using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[ExecuteAlways, DisallowMultipleComponent]
public sealed class WinterWeather2D : MonoBehaviour
{
    [Range(0,1)] public float snowCoverage = 0.9f;
    [Range(0,2)] public float snowfall = 0.8f;
    [Range(-2,2)] public float wind = 0.3f;
    public bool shoreIce = true;
    [Tooltip("Only visual weather; does not freeze the river or change movement/collisions.")]
    public bool previewInEditor = true;
    [Header("Snow drifts and tracks")]
    [Range(0,1)] public float snowDepthVariation = 0.85f;
    [Min(0.2f)] public float snowDriftSize = 3;
    public bool leaveFootprints = true;
    [Range(5,120)] public float footprintLifetime = 35;
    public float EffectiveSnowCoverage => isActiveAndEnabled ? snowCoverage * snowBlend : 0;

    [Serializable]
    private sealed class SurfaceBinding
    {
        public Renderer renderer;
        public Material original;
    }
    [SerializeField, HideInInspector] private List<SurfaceBinding> surfaces = new List<SurfaceBinding>();
    [SerializeField, HideInInspector] private Material summerDefault;
    private readonly Dictionary<Renderer, SurfaceBinding> known = new Dictionary<Renderer, SurfaceBinding>();
    private readonly List<WaterSurface2D> rivers = new List<WaterSurface2D>();
    private readonly List<ParticleSystem> flakes = new List<ParticleSystem>();
    private Material snowMaterial;
    private Material flakeMaterial;
    private MaterialPropertyBlock properties;
    private float previousTime;
    private float nextRefresh;
    private float snowBlend = 1;
    private float rainBlend;
    private bool weatherDirty;

    public void SetWeather(float snow, float rain)
    {
        snow = Mathf.Clamp01(snow);
        rain = Mathf.Clamp01(rain);
        if (Mathf.Abs(snowBlend - snow) + Mathf.Abs(rainBlend - rain) > 0.001f)
            weatherDirty = true;
        snowBlend = snow;
        rainBlend = rain;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WatchScenes()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }
    private static void SceneLoaded(Scene scene, LoadSceneMode mode) => Install(scene);

    public static void Install(Scene scene)
    {
        if (!scene.isLoaded || (scene.name != "RPG1" && scene.name != "RPG2")) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            var existing = root.GetComponentInChildren<WinterWeather2D>(true);
            if (existing == null) continue;
            if (existing.GetComponent<WeatherCycle2D>() == null) existing.gameObject.AddComponent<WeatherCycle2D>();
            return;
        }
        var weather = new GameObject("Winter Weather");
        SceneManager.MoveGameObjectToScene(weather, scene);
        weather.AddComponent<WinterWeather2D>();
        weather.AddComponent<WeatherCycle2D>();
    }

    private void OnEnable()
    {
        snowMaterial = Resources.Load<Material>("WinterSurface");
        flakeMaterial = Resources.Load<Material>("PixelSnow");
        properties = new MaterialPropertyBlock();
        known.Clear();
        surfaces.RemoveAll(binding => binding.renderer == null);
        foreach (var binding in surfaces) known[binding.renderer] = binding;
        previousTime = Time.realtimeSinceStartup;
        nextRefresh = 0;
    }

    private void OnValidate() => nextRefresh = 0;

    public static int SurfaceKind(Renderer renderer)
    {
        if (renderer is TilemapRenderer) return 0;
        var sprite = renderer as SpriteRenderer;
        if (sprite == null || sprite.sprite == null) return -1;
        string texture = sprite.sprite.texture.name;
        if (texture == "Tree") return 1;
        if (texture.StartsWith("House_")) return 2;
        if (sprite.GetComponentInParent<Tilemap>() != null) return 0;
        return -1;
    }

    public void RefreshSurfaces()
    {
        if (snowMaterial == null) return;
        rivers.Clear();
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            rivers.AddRange(root.GetComponentsInChildren<WaterSurface2D>(true));
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                ApplySurface(renderer);
        }
        foreach (var river in rivers)
        {
            river.SetWinter(shoreIce ? snowCoverage * snowBlend : 0);
            river.SetRain(rainBlend);
        }
        surfaces.RemoveAll(binding => binding.renderer == null);
    }

    public void ApplySurface(Renderer renderer)
    {
        if (snowMaterial == null || renderer == null) return;
        int kind = SurfaceKind(renderer);
        if (kind < 0) return; // Actors, loot, shop coins, prompts and all UI keep their art/material.
        if (!known.ContainsKey(renderer))
        {
            var original = renderer.sharedMaterial;
            if (original != snowMaterial && summerDefault == null) summerDefault = original;
            var binding = new SurfaceBinding { renderer = renderer, original = original == snowMaterial ? summerDefault : original };
            known.Add(renderer, binding);
            surfaces.Add(binding);
        }
        renderer.sharedMaterial = snowMaterial;
        renderer.GetPropertyBlock(properties);
        properties.SetFloat("_Kind", kind);
        properties.SetFloat("_Coverage", snowCoverage * snowBlend);
        properties.SetFloat("_Wetness", rainBlend);
        properties.SetFloat("_SnowVariation", snowDepthVariation);
        properties.SetFloat("_DriftScale", Mathf.Max(0.2f,snowDriftSize));
        renderer.SetPropertyBlock(properties);
    }

    private void Update()
    {
        if (Application.isPlaying && leaveFootprints && GetComponent<SnowFootprints2D>() == null)
            gameObject.AddComponent<SnowFootprints2D>();
        if (!Application.isPlaying && !previewInEditor) { Restore(); return; }
        if (snowBlend <= 0 && rainBlend <= 0) { Restore(); return; }
        float now = Time.realtimeSinceStartup;
        if (now >= nextRefresh)
        {
            nextRefresh = now + 0.75f;
            RefreshSurfaces();
        }
        if (weatherDirty)
        {
            // Update cached renderers during a blend; do not rescan the entire scene each frame.
            foreach (var binding in surfaces) ApplySurface(binding.renderer);
            foreach (var river in rivers)
                if (river != null)
                {
                    river.SetWinter(shoreIce ? snowCoverage * snowBlend : 0);
                    river.SetRain(rainBlend);
                }
            weatherDirty = false;
        }
        if (snowMaterial == null || flakeMaterial == null) return;
        if (flakes.Count == 0 && snowBlend > 0) CreateSnowfall();
        Camera camera = Camera.main;
        float height = camera != null && camera.orthographic ? camera.orthographicSize * 2 : 14;
        float width = height * (camera != null ? camera.aspect : 1.7778f);
        Vector3 center = camera != null ? camera.transform.position : Vector3.zero;
        for (int i = 0; i < flakes.Count; i++)
        {
            var snow = flakes[i];
            if (snow == null) continue;
            snow.transform.position = new Vector3(center.x, center.y + 1, 0);
            var shape = snow.shape;
            shape.scale = new Vector3(width + 6, height + 6, 0.01f);
            var emission = snow.emission;
            emission.rateOverTime = (width + 6) * (height + 6) * snowfall * snowBlend * (i == 2 ? 0.018f : 0.055f);
            var velocity = snow.velocityOverLifetime;
            velocity.x = wind * (0.65f + i * 0.45f);
            if (!Application.isPlaying) snow.Simulate(Mathf.Clamp(now - previousTime, 0, 0.1f), false, false);
        }
        previousTime = now;
    }

    private void CreateSnowfall()
    {
        for (int i = 0; i < 3; i++)
        {
            var child = new GameObject(new[] { "Distant snow", "Snowflakes", "Near snow" }[i]) { hideFlags = HideFlags.HideAndDontSave };
            child.transform.SetParent(transform, false);
            var snow = child.AddComponent<ParticleSystem>();
            snow.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            snow.useAutoRandomSeed = false;
            snow.randomSeed = (uint)(1531 + i * 71);
            var main = snow.main;
            main.loop = true;
            main.duration = 12;    
            main.startLifetime = 12;
            main.startSpeed = 0;
            main.startSize = new ParticleSystem.MinMaxCurve(0.035f + i * 0.025f, 0.06f + i * 0.04f);
            main.startColor = new Color(0.9f, 0.96f, 1, 0.45f + i * 0.16f);
            main.maxParticles = 1200;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            var shape = snow.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            var velocity = snow.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = wind * (0.65f + i * 0.45f);
            velocity.y = -(0.45f + i * 0.38f);
            velocity.z = 0; 
            var color = snow.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0,0), new GradientAlphaKey(1,0.12f), new GradientAlphaKey(1,0.8f), new GradientAlphaKey(0,1) });
            color.color = fade;
            var renderer = snow.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = flakeMaterial;
            renderer.sortingOrder = 24000 + i;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            snow.Play();
            flakes.Add(snow);
        }
    }

    private void Restore()
    {
        foreach (var binding in surfaces)
            if (binding.renderer != null && binding.renderer.sharedMaterial == snowMaterial)
                binding.renderer.sharedMaterial = binding.original;
        foreach (var river in rivers) if (river != null) { river.SetWinter(0); river.SetRain(0); }
        foreach (var snow in flakes)
            if (snow != null)
            {
                snow.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(snow.gameObject); else DestroyImmediate(snow.gameObject);
            }
        flakes.Clear();
        nextRefresh = 0;
    }

    private void OnDisable() => Restore();
}
