using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// Keep authored tiles and their physics intact. Draw their visual fragments at
// the shared tree-root depth; all three rows of a tree must stay together.
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(Tilemap))]
public sealed class ObliqueTilemap2D : MonoBehaviour
{
    public bool elevatedTrees;
    public int terrainOrder;
    public bool splitTrees;
    private Tilemap map;
    private TilemapRenderer source;
    private GameObject preview;
    private bool dirty;
    private readonly List<Fragment> fragments = new List<Fragment>();
    private sealed class Fragment
    {
        public Vector3Int cell;
        public SpriteRenderer renderer;
        public Sprite[] animation;
        public float speed;
    }

    public static bool IsTree(Sprite sprite) => sprite != null && sprite.texture.name == "Tree";

    public static float RootOffset(Sprite sprite)
    {
        // This project's Tree sheet uses three 64px rows per tree (192px).
        // Stumps are in the bottom row and use the same ground-contact rule.
        return (sprite.rect.y % 192f + sprite.pivot.y - 16f) / sprite.pixelsPerUnit;
    }

    private void OnEnable()
    {
        map = GetComponent<Tilemap>();
        source = GetComponent<TilemapRenderer>();
        Tilemap.tilemapTileChanged += TilesChanged;
        dirty = true;
    }

    private void TilesChanged(Tilemap changed, Tilemap.SyncTile[] changes)
    {
        if (changed == map) dirty = true;
    }

    private void OnValidate() => dirty = true;

    private void LateUpdate()
    {
        if (dirty) Rebuild();
        if (!splitTrees && source != null) source.sortingOrder = terrainOrder;
        if (source == null || preview == null) return;
        source.forceRenderingOff = true;
        foreach (var fragment in fragments)
        {
            var renderer = fragment.renderer;
            if (renderer == null) continue;
            if (fragment.animation != null && fragment.animation.Length > 0)
                renderer.sprite = fragment.animation[Mathf.FloorToInt(Time.realtimeSinceStartup * fragment.speed) % fragment.animation.Length];
            var sprite = renderer.sprite;
            if (sprite == null) continue;
            renderer.color = map.color * map.GetColor(fragment.cell);
            renderer.enabled = source.enabled;
            Vector3 contact = renderer.transform.TransformPoint(new Vector3(0, -RootOffset(sprite), 0));
            renderer.sortingOrder = IsTree(sprite) ? ObliqueDepth2D.Order(contact.y, elevatedTrees) : terrainOrder;
        }
    }

    private void Rebuild()
    {
        dirty = false;
        Clear();
        if (map == null || source == null) return;
        if (!splitTrees) { source.sortingOrder = terrainOrder; return; }
        preview = new GameObject("3-4 View Tile Preview") { hideFlags = HideFlags.HideAndDontSave };
        preview.transform.SetParent(transform, false);
        foreach (var cell in map.cellBounds.allPositionsWithin)
        {
            var sprite = map.GetSprite(cell);
            if (sprite == null) continue;
            var child = new GameObject("Tile " + cell) { hideFlags = HideFlags.HideAndDontSave };
            child.layer = gameObject.layer;
            child.transform.SetParent(preview.transform, false);
            Matrix4x4 matrix = map.orientationMatrix * map.GetTransformMatrix(cell);
            child.transform.localPosition = map.GetCellCenterLocal(cell) + (Vector3)matrix.GetColumn(3);
            child.transform.localRotation = matrix.rotation;
            child.transform.localScale = matrix.lossyScale;
            var renderer = child.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.sortingLayerID = source.sortingLayerID;
            renderer.maskInteraction = source.maskInteraction;
            var fragment = new Fragment { cell = cell, renderer = renderer };
            var tile = map.GetTile<AnimatedTile>(cell);
            if (tile != null)
            {
                fragment.animation = tile.m_AnimatedSprites;
                fragment.speed = Mathf.Max(0.01f, tile.m_MinSpeed * map.animationFrameRate);
            }
            fragments.Add(fragment);
        }
        source.forceRenderingOff = true;
    }

    private void Clear()
    {
        if (preview != null)
        {
            if (Application.isPlaying) Destroy(preview); else DestroyImmediate(preview);
        }
        preview = null;
        fragments.Clear();
    }

    private void OnDisable()
    {
        Tilemap.tilemapTileChanged -= TilesChanged;
        if (source != null) source.forceRenderingOff = false;
        Clear();
    }
}
