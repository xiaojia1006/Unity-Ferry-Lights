using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor;

[InitializeOnLoad]
public static class NightWorkshop
{
    static NightWorkshop(){EditorApplication.update+=Tick;}
    static void Tick()
    {
        const string path="Temp/Night.request";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(path))return;
        string command;try{command=File.ReadAllText(path).Trim();if(command.Length==0)return;File.Delete(path);}catch(IOException){return;}
        try
        {
            if(command=="setup")Setup();
            else if(command=="test")Test();
            else if(command=="day")DayNight2D.Instance.SetPhase(DayNight2D.Phase.Day);
            else if(command=="night")DayNight2D.Instance.SetPhase(DayNight2D.Phase.Night);
            else if(command=="capture")Capture("Logs/Night-RPG2.png");
            else if(command=="capture-day")Capture("Logs/Day-RPG2.png");
            else if(command=="capture-beacons")BeaconView();
            else if(command=="capture-weather")WeatherView();
            else if(command=="capture-settings"){GlobalSettings.Instance.SetOpen(true);GlobalSettings.Instance.SendMessage("ShowPage",1);Capture("Logs/Night-Settings.png");}
            else if(command=="status")Status();
            else throw new Exception("Unknown night request: "+command);
            File.WriteAllText("Logs/NightRequest.txt",command+" PASS");File.AppendAllText("Logs/NightChecks.txt",DateTime.Now+" "+command+" PASS\n");
        }
        catch(Exception e){File.WriteAllText("Logs/NightRequest.txt",command+" FAIL "+e);Debug.LogException(e);}
    }
    static void Setup()
    {
        if(EditorApplication.isPlaying)throw new Exception("Setup requires Edit mode");
        const string path="Assets/Resources/Night/Audio.asset";
        var bank=AssetDatabase.LoadAssetAtPath<NightAudioLibrary>(path);
        if(bank==null){bank=ScriptableObject.CreateInstance<NightAudioLibrary>();AssetDatabase.CreateAsset(bank,path);}
        bank.night=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/Continue_Life.mp3");
        bank.ruins=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/BGM/Lightless_Dawn.mp3");
        Require(bank.night!=null&&bank.ruins!=null,"Existing night music missing");EditorUtility.SetDirty(bank);AssetDatabase.SaveAssets();
        foreach(var clip in new[]{bank.night,bank.ruins})
        {
            var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));var settings=importer.defaultSampleSettings;
            settings.loadType=AudioClipLoadType.Streaming;importer.defaultSampleSettings=settings;importer.preloadAudioData=false;importer.SaveAndReimport();
        }
    }
    static void Require(bool good,string message){if(!good)throw new Exception(message);}
    static void Test()
    {
        var n=DayNight2D.Instance;Require(n!=null,"RPG2 night controller missing");var g=GlobalSettings.Instance;
        int original=g.Data.dayNightMode;float timeScale=Time.timeScale;
        try
        {
            g.Data.dayNightMode=0;n.Advance(0);n.SetPhase(DayNight2D.Phase.Day);Require(n.Darkness==0,"Day must preserve original art");
            n.Advance(180);Require(n.Current==DayNight2D.Phase.Dusk&&n.Darkness<.001f,"Dusk boundary discontinuity");
            n.Advance(60);Require(n.Current==DayNight2D.Phase.Night&&n.Darkness<=.281f,"Night boundary discontinuity");
            n.SetPhase(DayNight2D.Phase.Night);Require(n.Darkness>.4f&&n.Darkness<=.5f,"Night darkness bound");
            float seconds=n.PhaseSeconds;n.Advance(0);Require(n.PhaseSeconds==seconds,"Paused clock advanced");
            Require(n.RegisteredLights==6,"Expected camp, inn, treasure and three beacon lights");
            var shader=Resources.Load<Shader>("Night/NightAtmosphere");Require(shader!=null&&shader.isSupported,"Unsupported night shader");Require(!UnityEditor.ShaderUtil.ShaderHasError(shader),"Shader compile error");
            var mat=new Material(shader);var cam=Camera.main;n.ConfigureMaterial(mat,cam);Require(n.ActiveLightCount==3,"Unlit beacons must not light ground");
            var beacon=UnityEngine.Object.FindObjectsOfType<RPG2Point>().First(p=>p.kind==RPG2Point.Kind.Beacon);
            bool lit=beacon.glow.activeSelf;
            try{beacon.SetLit(true);n.Advance(.01f);n.ConfigureMaterial(mat,cam);Require(n.ActiveLightCount==4,"Lit beacon did not illuminate");Require(n.BeaconPulse>0,"No beacon feedback");}
            finally{beacon.SetLit(lit);n.Advance(.2f);UnityEngine.Object.Destroy(mat);}
            g.Data.dayNightMode=1;n.Advance(0);Require(n.Current==DayNight2D.Phase.Day&&n.Darkness==0,"Always day preference broken");
            g.Data.dayNightMode=2;n.Advance(0);seconds=n.PhaseSeconds;n.Advance(30);Require(n.Current==DayNight2D.Phase.Night&&n.PhaseSeconds==seconds,"Fixed night preference broken");
            var bank=Resources.Load<NightAudioLibrary>("Night/Audio");Require(bank!=null&&bank.night!=null&&bank.ruins!=null,"Build music references missing");
            g.AdvanceMusic(2);Require(g.CurrentMusic==bank.night,"Night music not connected to global player");
            n.SetPhase(DayNight2D.Phase.Day);g.AdvanceMusic(2);Require(g.CurrentMusic!=bank.night&&g.CurrentMusic!=bank.ruins,"Day music not restored");
            Require(Time.timeScale==timeScale,"Night changed pause state");
            Require(UnityEngine.Object.FindObjectsOfType<DayNight2D>().Length==1,"Duplicate night roots");
        }
        finally{g.Data.dayNightMode=original;n.Advance(0);n.SetPhase(DayNight2D.Phase.Day);g.AdvanceMusic(2);g.Save();}
    }
    static void Status()
    {
        var n=DayNight2D.Instance;
        string line=SceneManager.GetActiveScene().name+" nightRoots="+UnityEngine.Object.FindObjectsOfType<DayNight2D>().Length+" effects="+UnityEngine.Object.FindObjectsOfType<NightCamera2D>().Length;
        if(n!=null)line+=" phase="+n.Current+" darkness="+n.Darkness+" lights="+n.RegisteredLights;
        File.AppendAllText("Logs/NightChecks.txt",line+"\n");
    }
    static void BeaconView()
    {
        var n=DayNight2D.Instance;n.SetPhase(DayNight2D.Phase.Night);
        var beacon=UnityEngine.Object.FindObjectsOfType<RPG2Point>().First(p=>p.kind==RPG2Point.Kind.Beacon&&p.number==0);bool before=beacon.glow.activeSelf;
        var camera=Camera.main;var at=camera.transform.position;camera.transform.position=new Vector3(beacon.transform.position.x,beacon.transform.position.y,-10);
        try{beacon.SetLit(true);n.Advance(.3f);Capture("Logs/Night-Beacon.png");}finally{beacon.SetLit(before);camera.transform.position=at;n.Advance(.3f);}
    }
    static void WeatherView()
    {
        var weather=UnityEngine.Object.FindObjectOfType<WeatherCycle2D>();var before=weather.currentWeather;bool random=weather.randomWeather;
        try{weather.randomWeather=false;foreach(var kind in new[]{WeatherCycle2D.Weather.Rain,WeatherCycle2D.Weather.Snow}){weather.SetWeather(kind);weather.Advance(7);DayNight2D.Instance.Advance(.1f);Capture("Logs/Night-"+kind+".png");}}
        finally{weather.SetWeather(before);weather.Advance(7);weather.randomWeather=random;}
    }
    static void Capture(string path)
    {
        if(GlobalSettings.Instance.IsOpen&&!path.Contains("Settings"))GlobalSettings.Instance.SetOpen(false);
        var camera=Camera.main;Require(camera!=null,"No game camera");var n=DayNight2D.Instance;if(n!=null)n.Advance(0);
        var canvases=UnityEngine.Object.FindObjectsOfType<Canvas>().Where(c=>c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay).OrderBy(c=>c.sortingOrder).ToArray();
        var ui=new GameObject("Night QA UI camera");var uiCamera=ui.AddComponent<Camera>();uiCamera.clearFlags=CameraClearFlags.Depth;uiCamera.cullingMask=1<<31;uiCamera.orthographic=true;uiCamera.transform.position=new Vector3(0,0,-100);
        var layers=new Dictionary<GameObject,int>();var rt=new RenderTexture(1280,720,24);var target=camera.targetTexture;float aspect=camera.aspect;var active=RenderTexture.active;int mask=camera.cullingMask;
        try
        {
            foreach(var canvas in canvases){foreach(var t in canvas.GetComponentsInChildren<Transform>(true)){if(!layers.ContainsKey(t.gameObject))layers[t.gameObject]=t.gameObject.layer;t.gameObject.layer=31;}canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=uiCamera;canvas.planeDistance=1;}
            camera.cullingMask&=~(1<<31);camera.targetTexture=rt;camera.aspect=1280f/720;uiCamera.targetTexture=rt;uiCamera.aspect=camera.aspect;
            Canvas.ForceUpdateCanvases();camera.Render();uiCamera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
        }
        finally
        {
            foreach(var canvas in canvases)if(canvas!=null){canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;}
            foreach(var pair in layers)if(pair.Key!=null)pair.Key.layer=pair.Value;
            camera.targetTexture=target;camera.aspect=aspect;camera.cullingMask=mask;uiCamera.targetTexture=null;RenderTexture.active=active;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(ui);
        }
    }
}
