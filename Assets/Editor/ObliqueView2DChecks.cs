using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class ObliqueView2DChecks
{
    static ObliqueView2DChecks() => EditorApplication.delayCall += Run;

    [MenuItem("Tools/RPG/Check 3-4 View")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var report = new List<string>();
        GameObject testRoot = null;
        Texture2D texture = null;
        Sprite top = null, bottom = null;
        try
        {
            Require(ObliqueDepth2D.Order(-1, false) > ObliqueDepth2D.Order(1, false), "Southern actor draws in front", report);
            Require(ObliqueDepth2D.Order(100, true) > ObliqueDepth2D.Order(-100, false), "Platform band stays above ground actors", report);
            testRoot = new GameObject("3-4 validation") { hideFlags = HideFlags.HideAndDontSave };
            var actor = new GameObject("House_footprint check") { hideFlags = HideFlags.HideAndDontSave };
            actor.transform.SetParent(testRoot.transform);
            var renderer = actor.AddComponent<SpriteRenderer>();
            var body = actor.AddComponent<CapsuleCollider2D>();
            body.size = new Vector2(0.5f, 1f);
            body.offset = new Vector2(0, 0.1f);
            var trigger = actor.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 2f;
            float originalFoot = body.offset.y - body.size.y / 2;
            ObliqueView2D.ConfigureActor(renderer);
            Require(Mathf.Approximately(originalFoot, body.offset.y - body.size.y / 2), "Footprint keeps original foot position", report);
            Require(body.size.y < 0.5f && trigger.radius == 2f, "Shortened body leaves interaction trigger intact", report);
            var firstSize = body.size;
            ObliqueView2D.ConfigureActor(renderer);
            Require(body.size == firstSize && actor.GetComponents<ObliqueDepth2D>().Length == 1, "Setup is idempotent", report);
            var depth = actor.GetComponent<ObliqueDepth2D>();
            depth.Refresh();
            int ground = renderer.sortingOrder;
            depth.elevated = true;
            depth.Refresh();
            Require(renderer.sortingOrder > ground + 9000, "Player elevation changes sorting band", report);

            texture = new Texture2D(192, 576) { name = "Tree", hideFlags = HideFlags.HideAndDontSave };
            top = Sprite.Create(texture, new Rect(64, 512, 64, 64), Vector2.one * 0.5f, 64);
            bottom = Sprite.Create(texture, new Rect(64, 384, 64, 64), Vector2.one * 0.5f, 64);
            float topRoot = 2.5f - ObliqueTilemap2D.RootOffset(top);
            float bottomRoot = 0.5f - ObliqueTilemap2D.RootOffset(bottom);
            Require(Mathf.Approximately(topRoot, bottomRoot), "Tree crown and trunk share root sorting depth", report);
            var grid = testRoot.AddComponent<Grid>();
            var tiles = new GameObject("Tree tiles") { hideFlags = HideFlags.HideAndDontSave };
            tiles.transform.SetParent(grid.transform, false);
            var map = tiles.AddComponent<Tilemap>();
            var tileRenderer = tiles.AddComponent<TilemapRenderer>();
            var collision = tiles.AddComponent<TilemapCollider2D>();
            var projection = tiles.AddComponent<ObliqueTilemap2D>();
            projection.splitTrees = true;
            tiles.SendMessage("LateUpdate");
            Require(tileRenderer.forceRenderingOff && collision.enabled, "Tree projection preserves original tile collision", report);
            projection.enabled = false;
            Require(!tileRenderer.forceRenderingOff, "Disabling projection restores original Tilemap renderer", report);
            var prompt = new GameObject("Button_Blue_3Slides") { hideFlags = HideFlags.HideAndDontSave };
            prompt.transform.SetParent(testRoot.transform, false);
            var background = prompt.AddComponent<SpriteRenderer>();
            var iconObject = new GameObject("G_Idle") { hideFlags = HideFlags.HideAndDontSave };
            iconObject.transform.SetParent(prompt.transform, false);
            var icon = iconObject.AddComponent<SpriteRenderer>();
            background.sortingOrder = icon.sortingOrder = 30000;
            ObliqueView2D.ConfigurePrompt(background);
            Require(icon.sortingOrder > background.sortingOrder, "Merchant coin renders above its prompt background", report);
            Require(background.sortingOrder > ObliqueDepth2D.Order(-100, true), "Merchant prompt stays above world geometry", report);
            int firstIconOrder = icon.sortingOrder;
            ObliqueView2D.ConfigurePrompt(background);
            Require(icon.sortingOrder == firstIconOrder && icon.enabled && icon.gameObject.activeSelf, "Repeated prompt repair preserves visible icon", report);
            report.Add("PASS: all checks completed");
        }
        catch (Exception error) { report.Add("FAIL: " + error); Debug.LogException(error); }
        finally
        {
            if (testRoot != null) UnityEngine.Object.DestroyImmediate(testRoot);
            if (top != null) UnityEngine.Object.DestroyImmediate(top);
            if (bottom != null) UnityEngine.Object.DestroyImmediate(bottom);
            if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/ObliqueView2DChecks.txt", report);
        }
    }

    private static void Require(bool condition, string message, List<string> report)
    {
        if (!condition) throw new InvalidOperationException(message);
        report.Add("PASS: " + message);
    }
}
