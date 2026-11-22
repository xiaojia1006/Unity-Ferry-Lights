using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class WinterWeather2DChecks
{
    static WinterWeather2DChecks() => EditorApplication.delayCall += Run;

    [MenuItem("Tools/RPG/Check Winter Weather")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var report = new List<string>();
        GameObject test = null;
        try
        {
            var material = Resources.Load<Material>("WinterSurface");
            var flakes = Resources.Load<Material>("PixelSnow");
            Require(material != null && flakes != null, "Winter materials are available in Resources for builds", report);
            Require(!ShaderUtil.ShaderHasError(material.shader) && !ShaderUtil.ShaderHasError(flakes.shader), "Snow surface and particle shaders have no compilation errors", report);
            test = new GameObject("Winter checks") { hideFlags = HideFlags.HideAndDontSave };
            var weather = test.AddComponent<WinterWeather2D>();
            var groundObject = Child(test, "Ground");
            groundObject.AddComponent<Tilemap>();
            var ground = groundObject.AddComponent<TilemapRenderer>();
            Material original = ground.sharedMaterial;
            weather.ApplySurface(ground);
            Require(ground.sharedMaterial == material, "Terrain uses accumulation material", report);
            weather.enabled = false;
            Require(ground.sharedMaterial == original, "Disabling winter restores original terrain material", report);
            weather.enabled = true;
            var coin = Child(test, "G_Idle").AddComponent<SpriteRenderer>();
            coin.sprite = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath("fef16024a59e0834abe3e82fc488dd4a")).OfType<Sprite>().FirstOrDefault();
            Require(coin.sprite != null, "Real merchant coin sprite is available", report);
            coin.sortingOrder = 30001;
            var coinMaterial = coin.sharedMaterial;
            weather.ApplySurface(coin);
            Require(coin.sharedMaterial == coinMaterial && coin.sortingOrder == 30001, "Shop icons retain material and sorting", report);
            var tree = Child(test, "Tree").AddComponent<SpriteRenderer>();
            tree.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/Tree/Tree.png").OfType<Sprite>().First();
            Require(WinterWeather2D.SurfaceKind(tree) == 1, "Tree sheet receives branch-top snow treatment", report);
            var roof = Child(test, "House").AddComponent<SpriteRenderer>();
            roof.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Building/House/House_Purple.png");
            Require(WinterWeather2D.SurfaceKind(roof) == 2, "House uses roof accumulation treatment", report);
            var waterObject = Child(test, "Water check");
            var river = waterObject.AddComponent<WaterSurface2D>();
            var collider = waterObject.GetComponent<PolygonCollider2D>();
            var points = collider.GetPath(0);
            var sprite = waterObject.GetComponent<SpriteRenderer>().sprite;
            river.SetWinter(1);
            var block = new MaterialPropertyBlock();
            waterObject.GetComponent<SpriteRenderer>().GetPropertyBlock(block);
            Require(block.GetFloat("_WinterAmount") == 1 && block.GetTexture("_WaterMask") != null, "Shore ice preserves the explicit river mask", report);
            Require(collider.GetPath(0).SequenceEqual(points) && collider.isTrigger && waterObject.GetComponent<SpriteRenderer>().sprite == sprite,
                "Winter does not change river geometry or collision", report);
            Require(test.GetComponentsInChildren<Collider2D>().Length == 1, "Snow surfaces add no gameplay colliders", report);
            report.Add("PASS: all winter checks completed");
        }
        catch (Exception error) { report.Add("FAIL: " + error); Debug.LogException(error); }
        finally
        {
            if (test != null) UnityEngine.Object.DestroyImmediate(test);
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/WinterWeather2DChecks.txt", report);
        }
    }

    private static GameObject Child(GameObject parent, string name)
    {
        var child = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
        child.transform.SetParent(parent.transform, false);
        return child;
    }
    private static void Require(bool condition, string message, List<string> report)
    {
        if (!condition) throw new InvalidOperationException(message);
        report.Add("PASS: " + message);
    }
}
