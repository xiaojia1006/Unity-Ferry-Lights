using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(SpriteRenderer), typeof(PolygonCollider2D))]
public class WaterSurface2D : MonoBehaviour
{
    public Color deepColor = new Color(0.13f, 0.28f, 0.36f);
    public Color shallowColor = new Color(0.36f, 0.55f, 0.58f);
    public Color foamColor = new Color(0.78f, 0.87f, 0.82f);
    public Vector2 flow = new Vector2(0.042f, -0.018f);
    [Range(8, 64)] public float pixelsPerUnit = 34;
    [Range(0.5f, 5)] public float shallowDistance = 2.3f;
    [Range(0, 1)] public float foamStrength = 0.5f;
    [Range(0.3f, 1)] public float swimSpeedMultiplier = 0.62f;

    private const int TextureWidth = 512;
    private const int TextureHeight = 512;
    private static readonly Vector2[] centerline =
    {
        new Vector2(-0.08f, 0.24f), new Vector2(0.06f, 0.27f),
        new Vector2(0.19f, 0.40f), new Vector2(0.34f, 0.50f),
        new Vector2(0.47f, 0.51f), new Vector2(0.59f, 0.40f),
        new Vector2(0.72f, 0.31f), new Vector2(0.84f, 0.35f),
        new Vector2(0.96f, 0.49f), new Vector2(1.08f, 0.53f)
    };

    private SpriteRenderer surface;
    private PolygonCollider2D waterCollider;
    private Sprite waterSprite;
    private Texture2D surfaceTexture;
    private Texture2D depthTexture;
    private MaterialPropertyBlock block;
    private Material initialMaterial;
    private Material runtimeMaterial;
    private Sprite initialSprite;
    private bool rebuildQueued;
    private float winterAmount;
    private float rainAmount;

    public void SetRain(float amount)
    {
        rainAmount = Mathf.Clamp01(amount);
        if (block != null)
        {
            block.SetFloat("_RainAmount", rainAmount);
            surface.SetPropertyBlock(block);
        }
    }

    public void SetWinter(float amount)
    {
        winterAmount = Mathf.Clamp01(amount);
        if (block != null)
        {
            block.SetFloat("_WinterAmount", winterAmount);
            surface.SetPropertyBlock(block);
        }
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InstallInLoadedScene()
    {
        foreach (var water in FindObjectsOfType<GameObject>())
        {
            if (water.name == "Water" && water.GetComponent<SpriteRenderer>() != null &&
                water.GetComponent<WaterSurface2D>() == null)
                water.AddComponent<WaterSurface2D>();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WatchSceneLoads() => SceneManager.sceneLoaded += OnSceneLoaded;

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => InstallInLoadedScene();

    private void Awake()
    {
        surface = GetComponent<SpriteRenderer>();
        waterCollider = GetComponent<PolygonCollider2D>();
        initialMaterial = surface.sharedMaterial;
        initialSprite = surface.sprite;
        surface.sortingOrder = Mathf.Max(surface.sortingOrder, -1);
        BuildSurface();
    }

    private void OnValidate()
    {
        if (surface == null || !isActiveAndEnabled) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            if (!rebuildQueued)
            {
                rebuildQueued = true;
                UnityEditor.EditorApplication.delayCall += RebuildAfterValidation;
            }
            return;
        }
#endif
        BuildSurface();
    }

#if UNITY_EDITOR
    private void RebuildAfterValidation()
    {
        rebuildQueued = false;
        if (this != null && isActiveAndEnabled && !Application.isPlaying)
            BuildSurface();
    }
#endif

    private void Update()
    {
        if (surface == null || block == null) return;
        block.SetFloat("_WaterTime", Time.realtimeSinceStartup);
        surface.SetPropertyBlock(block);
    }

    private void BuildSurface()
    {
        DestroyRuntimeObject(waterSprite);
        DestroyRuntimeObject(surfaceTexture);
        DestroyRuntimeObject(depthTexture);
        waterSprite = null;
        surfaceTexture = null;
        depthTexture = null;

        var material = Resources.Load<Material>("PixelWater");
        if (material == null)
        {
            var shader = Shader.Find("RPG/Pixel Water");
            if (shader == null)
            {
                Debug.LogError("Pixel water shader is missing. Add Assets/Resources/PixelWater.mat to the build.", this);
                enabled = false;
                return;
            }
            runtimeMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material = runtimeMaterial;
        }
        surface.sharedMaterial = material;

        var outline = BuildRiverOutline();
        var colorPixels = new Color32[TextureWidth * TextureHeight];
        var inside = new bool[colorPixels.Length];
        var distance = new float[colorPixels.Length];
        for (int y = 0; y < TextureHeight; y++)
            for (int x = 0; x < TextureWidth; x++)
            {
                int i = y * TextureWidth + x;
                Vector2 uv = new Vector2((x + 0.5f) / TextureWidth, (y + 0.5f) / TextureHeight);
                inside[i] = PointInside(outline, uv);
                distance[i] = inside[i] ? 10000f : 0f;
            }

        // Chamfer distance transform creates a real shallow-water band from the shoreline.
        float diagonal = Mathf.Sqrt(1f / (TextureWidth * TextureWidth) + 1f / (TextureHeight * TextureHeight));
        for (int y = 0; y < TextureHeight; y++)
            for (int x = 0; x < TextureWidth; x++)
            {
                int i = y * TextureWidth + x;
                if (x > 0) distance[i] = Mathf.Min(distance[i], distance[i - 1] + 1f / TextureWidth);
                if (y > 0) distance[i] = Mathf.Min(distance[i], distance[i - TextureWidth] + 1f / TextureHeight);
                if (x > 0 && y > 0) distance[i] = Mathf.Min(distance[i], distance[i - TextureWidth - 1] + diagonal);
                if (x + 1 < TextureWidth && y > 0) distance[i] = Mathf.Min(distance[i], distance[i - TextureWidth + 1] + diagonal);
            }
        for (int y = TextureHeight - 1; y >= 0; y--)
            for (int x = TextureWidth - 1; x >= 0; x--)
            {
                int i = y * TextureWidth + x;
                if (x + 1 < TextureWidth) distance[i] = Mathf.Min(distance[i], distance[i + 1] + 1f / TextureWidth);
                if (y + 1 < TextureHeight) distance[i] = Mathf.Min(distance[i], distance[i + TextureWidth] + 1f / TextureHeight);
                if (x + 1 < TextureWidth && y + 1 < TextureHeight) distance[i] = Mathf.Min(distance[i], distance[i + TextureWidth + 1] + diagonal);
                if (x > 0 && y + 1 < TextureHeight) distance[i] = Mathf.Min(distance[i], distance[i + TextureWidth - 1] + diagonal);
            }

        var depthPixels = new Color32[colorPixels.Length];
        var colliderPoints = new Vector2[outline.Count];
        float aspect = (float)TextureHeight / TextureWidth;
        for (int i = 0; i < outline.Count; i++)
            colliderPoints[i] = new Vector2(outline[i].x - 0.5f, (outline[i].y - 0.5f) * aspect);
        for (int i = 0; i < colorPixels.Length; i++)
        {
            colorPixels[i] = new Color32(255, 255, 255, inside[i] ? (byte)255 : (byte)0);
            byte d = (byte)Mathf.RoundToInt(Mathf.Clamp01(distance[i] * TextureWidth / 72f) * 255);
            depthPixels[i] = new Color32(d, d, d, 255);
        }

        surfaceTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false, true)
        { name = "Generated winding creek", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        surfaceTexture.SetPixels32(colorPixels);
        surfaceTexture.Apply(false, true);
        depthTexture = new Texture2D(TextureWidth, TextureHeight, TextureFormat.RGBA32, false, true)
        { name = "Generated creek depth", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        depthTexture.SetPixels32(depthPixels);
        depthTexture.Apply(false, true);
        waterSprite = Sprite.Create(surfaceTexture, new Rect(0, 0, TextureWidth, TextureHeight), new Vector2(0.5f, 0.5f), TextureWidth, 0, SpriteMeshType.FullRect);
        waterSprite.name = "Generated winding creek";
        surface.sprite = waterSprite;
        waterCollider.pathCount = 1;
        waterCollider.SetPath(0, colliderPoints);
        waterCollider.isTrigger = true;

        block = new MaterialPropertyBlock();
        block.SetTexture("_WaterMask", surfaceTexture);
        block.SetTexture("_ShoreTex", depthTexture);
        block.SetColor("_DeepColor", deepColor);
        block.SetColor("_ShallowColor", shallowColor);
        block.SetColor("_FoamColor", foamColor);
        block.SetVector("_Flow", new Vector4(flow.x, flow.y, 0, 0));
        block.SetFloat("_PixelScale", pixelsPerUnit);
        block.SetFloat("_ShallowDistance", shallowDistance);
        block.SetFloat("_FoamStrength", foamStrength);
        block.SetFloat("_WinterAmount", winterAmount);
        block.SetFloat("_RainAmount", rainAmount);
        surface.SetPropertyBlock(block);
    }

    private static List<Vector2> BuildRiverOutline()
    {
        var left = new List<Vector2>();
        var right = new List<Vector2>();
        for (int i = 0; i < centerline.Length - 1; i++)
        {
            Vector2 p0 = centerline[Mathf.Max(0, i - 1)];
            Vector2 p1 = centerline[i];
            Vector2 p2 = centerline[i + 1];
            Vector2 p3 = centerline[Mathf.Min(centerline.Length - 1, i + 2)];
            for (int step = 0; step < 10; step++)
            {
                float t = step / 10f, t2 = t * t, t3 = t2 * t;
                Vector2 p = 0.5f * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
                Vector2 tangent = (p2 - p0).normalized;
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                float width = 0.074f + 0.014f * Mathf.Sin((i + t) * 1.4f) + 0.008f * Mathf.Sin((i + t) * 3.1f);
                left.Add(p + normal * width);
                right.Add(p - normal * width);
            }
        }
        left.Add(centerline[centerline.Length - 1] + new Vector2(0.02f, 0.08f));
        right.Add(centerline[centerline.Length - 1] - new Vector2(0.02f, 0.08f));
        left[0] = centerline[0] + new Vector2(-0.02f, 0.08f);
        right[0] = centerline[0] + new Vector2(0.02f, -0.08f);
        right.Reverse();
        left.AddRange(right);
        return left;
    }

    private static bool PointInside(List<Vector2> polygon, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            if ((polygon[i].y > p.y) != (polygon[j].y > p.y) &&
                p.x < (polygon[j].x - polygon[i].x) * (p.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                inside = !inside;
        return inside;
    }

    private void OnTriggerEnter2D(Collider2D other) => ApplyWater(other, true);
    private void OnTriggerExit2D(Collider2D other) => ApplyWater(other, false);

    private void OnDisable()
    {
        foreach (var movement in FindObjectsOfType<PlayerMovement>())
            movement.SetWaterState(this, false, 1f);
    }

    private void ApplyWater(Collider2D other, bool entering)
    {
        var movement = other.GetComponent<PlayerMovement>();
        if (movement == null) movement = other.GetComponentInParent<PlayerMovement>();
        if (movement == null) return;
        if (!other.CompareTag("Player") && !movement.CompareTag("Player")) return;
        movement.SetWaterState(this, entering, swimSpeedMultiplier);
    }

    private void OnDestroy()
    {
        if (surface != null) surface.SetPropertyBlock(null);
        if (surface != null && surface.sprite == waterSprite)
        {
            surface.sprite = initialSprite;
            surface.sharedMaterial = initialMaterial;
        }
        DestroyRuntimeObject(waterSprite);
        DestroyRuntimeObject(surfaceTexture);
        DestroyRuntimeObject(depthTexture);
        DestroyRuntimeObject(runtimeMaterial);
    }

    private static void DestroyRuntimeObject(UnityEngine.Object obj)
    {
        if (obj == null) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(obj);
        else UnityEngine.Object.DestroyImmediate(obj);
    }
}
