using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class SnowTracksChecks
{
    private const string Pending="RPG.SnowTracks.Check";
    private static double ready;
    static SnowTracksChecks()
    {
        EditorApplication.playModeStateChanged+=s=>
        {
            if(s!=PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending,false)) return;
            SessionState.SetBool(Pending,false);
            ready=EditorApplication.timeSinceStartup+1;
            EditorApplication.update+=Wait;
        };
    }
    [MenuItem("Tools/RPG/Check Snow Tracks %#F7")]
    private static void Start()
    {
        if(EditorApplication.isPlaying) return;
        SessionState.SetBool(Pending,true);
        EditorApplication.EnterPlaymode();
    }
    private static void Wait()
    {
        if(EditorApplication.timeSinceStartup<ready) return;
        EditorApplication.update-=Wait;
        Run();
    }
    private static void Run()
    {
        var log=new List<string>();
        try
        {
            foreach(string name in new[]{"WinterSurface","SnowFootprint"})
            {
                var material=Resources.Load<Material>(name);
                Check(material!=null && !ShaderUtil.ShaderHasError(material.shader),name+" available and shader compiled",log);
            }
            var winter=UnityEngine.Object.FindObjectOfType<WinterWeather2D>();
            var cycle=winter.GetComponent<WeatherCycle2D>();
            cycle.randomWeather=false;
            cycle.transitionSeconds=0;
            cycle.SetWeather(WeatherCycle2D.Weather.Snow); cycle.Advance(0);
            winter.RefreshSurfaces();
            var tracks=winter.GetComponent<SnowFootprints2D>();
            if(tracks==null) tracks=winter.gameObject.AddComponent<SnowFootprints2D>();
            var player=UnityEngine.Object.FindObjectOfType<PlayerMovement>();
            Vector2 origin=player.transform.position;
            Vector2 dry=origin;
            bool found=false;
            for(int y=0;y<12 && !found;y++)
                for(int x=-6;x<7 && !found;x++)
                {
                    var point=origin+new Vector2(x*0.5f,y*0.5f);
                    if(tracks.TryStamp(point,Vector2.up)) { dry=point; found=true; }
                }
            Check(found,"Found dry snow terrain near actual player",log);
            int before=tracks.ActiveCount;
            tracks.Track(dry,false,false); tracks.Track(dry+Vector2.right,false,false);
            Check(tracks.ActiveCount==before,"Standing or non-walking movement creates no footprints",log);
            tracks.Track(dry+Vector2.right*20,true,false);
            Check(tracks.ActiveCount==before,"Teleport does not draw a bridge of footprints",log);
            tracks.Track(dry,true,true);
            Check(tracks.ActiveCount==before,"Raised platforms do not receive low-ground prints",log);
            cycle.SetWeather(WeatherCycle2D.Weather.Sunny); cycle.Advance(0);
            Check(!tracks.TryStamp(dry,Vector2.up),"Sunny rejects new prints",log);
            tracks.UpdateStamps();
            Check(tracks.ActiveCount==0,"Melted snow hides all tracks",log);
            cycle.SetWeather(WeatherCycle2D.Weather.Snow); cycle.Advance(0);
            for(int i=0;i<120;i++) tracks.TryStamp(dry,Vector2.up);
            Check(tracks.GetComponentsInChildren<SpriteRenderer>().Length==96,"Track pool stays bounded at 96",log);
            foreach(var river in UnityEngine.Object.FindObjectsOfType<WaterSurface2D>())
            {
                var collider=river.GetComponent<PolygonCollider2D>();
                bool checkedWater=false;
                for(float y=collider.bounds.min.y;y<collider.bounds.max.y && !checkedWater;y+=0.5f)
                    for(float x=collider.bounds.min.x;x<collider.bounds.max.x && !checkedWater;x+=0.5f)
                    {
                        var point=new Vector2(x,y);
                        if(!collider.OverlapPoint(point)) continue;
                        Check(!tracks.TryStamp(point,Vector2.up),"River and decorative shore ice reject footprints",log);
                        checkedWater=true;
                    }
            }
            // Replace pool entries with a visible alternating trail near the player for visual QA.
            tracks.ClearTracks();
            winter.footprintLifetime=120;
            for(int i=0;i<24;i++)
                tracks.TryStamp(dry+new Vector2(i%2==0 ? -0.075f : 0.075f,i*0.38f),Vector2.up);
            foreach(var renderer in tracks.GetComponentsInChildren<SpriteRenderer>())
                if(renderer.enabled) log.Add("Print: "+renderer.transform.position+" order="+renderer.sortingOrder);
            log.Add("Preview trail origin: "+dry);
            log.Add("PASS: checks finished; Play mode remains open for visual inspection. Stop normally to restore scene.");
        }
        catch(Exception e) { log.Add("FAIL: "+e); }
        Directory.CreateDirectory("Logs");
        File.WriteAllLines("Logs/SnowTracksChecks.txt",log);
    }
    private static void Check(bool ok,string message,List<string> log)
    {
        if(!ok) throw new InvalidOperationException(message);
        log.Add("PASS: "+message);
    }
}
