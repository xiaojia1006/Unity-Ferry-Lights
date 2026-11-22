using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class WeatherCycle2DChecks
{
    static WeatherCycle2DChecks() => EditorApplication.delayCall += Run;

    [MenuItem("Tools/RPG/Weather/Preview Sunny")]
    private static void Sunny() => Preview(WeatherCycle2D.Weather.Sunny);
    [MenuItem("Tools/RPG/Weather/Preview Rain")]
    private static void Rain() => Preview(WeatherCycle2D.Weather.Rain);
    [MenuItem("Tools/RPG/Weather/Preview Snow")]
    private static void Snow() => Preview(WeatherCycle2D.Weather.Snow);

    private static void Preview(WeatherCycle2D.Weather kind)
    {
        WinterWeather2DSetup.Install();
        foreach (var weather in UnityEngine.Object.FindObjectsOfType<WeatherCycle2D>())
        {
            Undo.RecordObject(weather,"Preview weather");
            weather.SetWeather(kind);
            EditorUtility.SetDirty(weather);
            if (!Application.isPlaying) EditorSceneManager.MarkSceneDirty(weather.gameObject.scene);
            Selection.activeGameObject = weather.gameObject;
        }
        EditorApplication.QueuePlayerLoopUpdate();
        SceneView.RepaintAll();
    }

    [MenuItem("Tools/RPG/Weather/Check Weather Cycle")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var report = new List<string>();
        var randomState = UnityEngine.Random.state;
        GameObject test = null;
        try
        {
            WinterWeather2DSetup.Install();
            var material = Resources.Load<Material>("PixelRain");
            Require(material != null && !ShaderUtil.ShaderHasError(material.shader),"Rain shader compiles and is referenced for builds",report);
            Require(!ShaderUtil.ShaderHasError(Resources.Load<Material>("WinterSurface").shader) &&
                    !ShaderUtil.ShaderHasError(Shader.Find("RPG/Pixel Water")),"Wet surfaces and river ripple shader compile",report);
            test = new GameObject("Weather cycle checks") { hideFlags = HideFlags.HideAndDontSave };
            var cycle = test.AddComponent<WeatherCycle2D>();
            var winter = test.GetComponent<WinterWeather2D>();
            cycle.minimumDuration = cycle.maximumDuration = 10;
            cycle.SetWeather(WeatherCycle2D.Weather.Sunny);
            cycle.Advance(11);
            Require(cycle.currentWeather != WeatherCycle2D.Weather.Sunny && cycle.SecondsRemaining == 10,"Elapsed timer chooses a different weather and resets duration",report);
            float remaining = cycle.SecondsRemaining;
            cycle.Advance(0);
            Require(cycle.SecondsRemaining == remaining,"Zero game time pauses the weather clock",report);
            for (int i=0;i<100;i++)
            {
                var before = cycle.currentWeather;
                var next = cycle.ChooseNextWeather();
                if (before == next) throw new InvalidOperationException("Repeated consecutive weather");
                cycle.SetWeather(next);
            }
            report.Add("PASS: 100 random selections avoid consecutive repeats");
            cycle.sunnyWeight = cycle.snowWeight = 0;
            cycle.rainWeight = 1;
            cycle.SetWeather(WeatherCycle2D.Weather.Sunny);
            Require(cycle.ChooseNextWeather() == WeatherCycle2D.Weather.Rain,"Zero weights exclude unwanted weather",report);
            cycle.rainWeight = 0;
            Require(cycle.ChooseNextWeather() == cycle.currentWeather,"All zero weights safely keep current weather",report);
            cycle.randomWeather = false;
            cycle.transitionSeconds = 2;
            cycle.SetWeather(WeatherCycle2D.Weather.Sunny);
            cycle.Advance(2);
            cycle.SetWeather(WeatherCycle2D.Weather.Snow);
            cycle.Advance(1);
            Require(Mathf.Abs(cycle.SnowAmount-0.5f)<0.001f,"Snow blends in gradually",report);
            cycle.Advance(1);
            cycle.SetWeather(WeatherCycle2D.Weather.Rain);
            cycle.Advance(2);
            var groundObject = new GameObject("Test ground");
            groundObject.transform.SetParent(test.transform,false);
            groundObject.AddComponent<Tilemap>();
            var ground = groundObject.AddComponent<TilemapRenderer>();
            var original = ground.sharedMaterial;
            winter.ApplySurface(ground);
            var block = new MaterialPropertyBlock();
            ground.GetPropertyBlock(block);
            Require(cycle.SnowAmount == 0 && cycle.RainAmount == 1 && block.GetFloat("_Coverage") == 0 && block.GetFloat("_Wetness") == 1,
                "Rain removes snow coverage and wets ground",report);
            cycle.SetWeather(WeatherCycle2D.Weather.Sunny);
            cycle.Advance(2);
            Require(cycle.SnowAmount == 0 && cycle.RainAmount == 0,"Sunny clears both precipitation states",report);
            winter.enabled = false;
            Require(ground.sharedMaterial == original,"Original ground material remains recoverable",report);
            var waterObject = new GameObject("Test river");
            waterObject.transform.SetParent(test.transform,false);
            var river = waterObject.AddComponent<WaterSurface2D>();
            river.SetRain(1);
            waterObject.GetComponent<SpriteRenderer>().GetPropertyBlock(block);
            Require(block.GetTexture("_WaterMask") != null && block.GetFloat("_RainAmount") == 1,"Rain ripples preserve explicit river mask",report);
            Require(test.GetComponentsInChildren<Collider2D>().Length == 1,"Weather adds no collision or UI blockers",report);
            report.Add("PASS: all weather cycle checks completed");
        }
        catch (Exception error) { report.Add("FAIL: "+error); Debug.LogException(error); }
        finally
        {
            if (test != null) UnityEngine.Object.DestroyImmediate(test);
            UnityEngine.Random.state = randomState;
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/WeatherCycle2DChecks.txt",report);
        }
    }

    private static void Require(bool condition, string message, List<string> report)
    {
        if (!condition) throw new InvalidOperationException(message);
        report.Add("PASS: " + message);
    }
}
