using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class ObliqueView2D
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void WatchScenes()
    {
        SceneManager.sceneLoaded -= SceneLoaded;
        SceneManager.sceneLoaded += SceneLoaded;
    }

    private static void SceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Configure(scene);
        foreach (var player in UnityEngine.Object.FindObjectsOfType<PlayerMovement>())
        {
            ConfigureActor(player.GetComponent<SpriteRenderer>());
            var depth = player.GetComponent<ObliqueDepth2D>();
            if (depth != null && mode == LoadSceneMode.Single) depth.elevated = false;
        }
    }

    public static void Configure(Scene scene)
    {
        if (!scene.isLoaded || (scene.name != "RPG1" && scene.name != "RPG2")) return;
        foreach (var root in scene.GetRootGameObjects())
        {
            foreach (var camera in root.GetComponentsInChildren<Camera>(true))
            {
                camera.transparencySortMode = TransparencySortMode.CustomAxis;
                camera.transparencySortAxis = Vector3.up;
            }
            foreach (var map in root.GetComponentsInChildren<Tilemap>(true))
            {
                var renderer = map.GetComponent<TilemapRenderer>();
                if (renderer == null || map.GetComponent<ObliqueTilemap2D>() != null) continue;
                bool trees = false;
                foreach (var cell in map.cellBounds.allPositionsWithin)
                    if (ObliqueTilemap2D.IsTree(map.GetSprite(cell))) { trees = true; break; }
                int order = renderer.sortingOrder;
                var depth = map.gameObject.AddComponent<ObliqueTilemap2D>();
                depth.terrainOrder = Mathf.Clamp(order * 1000, -30000, 30000);
                depth.elevatedTrees = map.name.StartsWith("gao");
                depth.splitTrees = trees;
            }
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (renderer.gameObject.hideFlags != HideFlags.None) continue;
                if (renderer.GetComponentInParent<Canvas>() == null && renderer.sortingOrder >= 50 && renderer.sortingOrder < 30000 &&
                    renderer.GetComponent<ObliqueDepth2D>() == null)
                    renderer.sortingOrder = 30000 + Mathf.Clamp(renderer.sortingOrder - 50, 0, 1000);
                if (renderer.GetComponent<PlayerMovement>() != null || renderer.GetComponent<NPC>() != null ||
                    renderer.GetComponent<Enemy_Movement>() != null || renderer.GetComponent<TreasureChest>() != null ||
                    renderer.GetComponent<Loot>() != null || renderer.name.StartsWith("House_") || renderer.name == "QuestBoard")
                    ConfigureActor(renderer);
            }
        }
        RepairWorldPrompts(scene);
    }

    public static void RepairWorldPrompts(Scene scene)
    {
        if (!scene.isLoaded) return;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                if (renderer.name == "Button_Blue_3Slides") ConfigurePrompt(renderer);
    }

    public static void ConfigurePrompt(SpriteRenderer background)
    {
        if (background == null) return;
        background.sortingOrder = 30000;
        foreach (var icon in background.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (icon == background) continue;
            int depth = 0;
            for (var parent = icon.transform; parent != background.transform; parent = parent.parent) depth++;
            // Also repairs scenes whose original 50/51 orders were both promoted to 30000.
            icon.sortingLayerID = background.sortingLayerID;
            icon.sortingOrder = 30000 + Mathf.Clamp(depth, 1, 100);
        }
    }

    public static void ConfigureActor(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.GetComponent<ObliqueDepth2D>() != null) return;
        bool raised = renderer.GetComponent<TreasureChest>() != null && renderer.sortingOrder == 10;
        // Keep the original lower edge fixed. No texture pivots or art positions change.
        bool character = renderer.GetComponent<PlayerMovement>() != null || renderer.GetComponent<NPC>() != null ||
            renderer.GetComponent<Enemy_Movement>() != null;
        if (character || renderer.name.StartsWith("House_"))
        {
            foreach (var collider in renderer.GetComponents<Collider2D>())
            {
                if (collider.isTrigger) continue;
                if (collider is CapsuleCollider2D capsule)
                {
                    Vector2 size = capsule.size;
                    float height = Mathf.Min(size.y, size.x * 0.65f);
                    capsule.offset += Vector2.down * (size.y - height) * 0.5f;
                    capsule.size = new Vector2(size.x, height);
                    capsule.direction = CapsuleDirection2D.Horizontal;
                }
                else if (collider is BoxCollider2D box)
                {
                    Vector2 size = box.size;
                    float height = Mathf.Min(size.y, size.x * (character ? 0.65f : 0.45f));
                    box.offset += Vector2.down * (size.y - height) * 0.5f;
                    box.size = new Vector2(size.x, height);
                }
            }
        }
        var depth = renderer.gameObject.AddComponent<ObliqueDepth2D>();
        depth.elevated = raised;
        depth.Refresh();
    }
}
