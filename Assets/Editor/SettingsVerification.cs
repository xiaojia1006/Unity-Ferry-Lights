using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
[InitializeOnLoad]
public static class SettingsVerification
{
    static SettingsVerification(){EditorApplication.update+=Update;}
    static void Update()
    {
        const string request="Temp/Settings.request";
        if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists(request))return;
        string command;try{command=File.ReadAllText(request).Trim();if(command.Length==0)return;File.Delete(request);}catch(IOException){return;}
        try {
            if(command=="refresh")AssetDatabase.Refresh();
            else if(command=="play")EditorApplication.isPlaying=true;
            else if(command=="stop")EditorApplication.isPlaying=false;
            else if(command=="capture")Capture();
            else if(command=="open"){GlobalSettings.Instance.SetOpen(true);}
            else if(command=="test") Test();
            else if(command=="login")SceneManager.LoadScene("Login");
            else if(command=="logout"){LoginSession.Set("verification", "local-test");GlobalSettings.Instance.SendMessage("Logout");Require(string.IsNullOrEmpty(LoginSession.Account)&&string.IsNullOrEmpty(LoginSession.Token),"Session not cleared");}
            else if(command=="status"){Require(GlobalSettings.Instance!=null,"Lost instance across scenes");Require(UnityEngine.Object.FindObjectsOfType<GlobalSettings>().Length==1,"Duplicate settings");File.AppendAllText("Logs/SettingsChecks.txt",SceneManager.GetActiveScene().name+" instance=1 master="+GlobalSettings.Instance.Data.master+" time="+Time.timeScale+"\n");}
            else if(command=="rpg1")SceneManager.LoadScene("RPG1");
            else if(command=="rpg2")SceneManager.LoadScene("RPG2");
            File.WriteAllText("Logs/SettingsVerification.txt",DateTime.Now+" "+command+" PASS\n");
            File.AppendAllText("Logs/SettingsChecks.txt",DateTime.Now+" "+command+" PASS\n");
        }catch(Exception e){File.WriteAllText("Logs/SettingsVerification.txt",command+" FAIL "+e);Debug.LogException(e);}
    }
    static void Capture()
    {
        var g=GlobalSettings.Instance;g.SetOpen(true);Canvas.ForceUpdateCanvases();
        var canvas=g.GetComponent<Canvas>();var go=new GameObject("Settings preview camera");var camera=go.AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.13f,.2f,.16f);camera.transform.position=new Vector3(0,0,-100);camera.orthographic=true;
        var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;
        try{camera.targetTexture=rt;canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=rt;var image=new Texture2D(1280,720,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();File.WriteAllBytes("Logs/SettingsPreview.png",image.EncodeToPNG());UnityEngine.Object.Destroy(image);}
        finally{canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.worldCamera=null;RenderTexture.active=previous;camera.targetTexture=null;rt.Release();UnityEngine.Object.Destroy(rt);UnityEngine.Object.Destroy(go);}
    }
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void Test()
    {
        var g=GlobalSettings.Instance;Require(g!=null,"Missing global settings");
        string original=JsonUtility.ToJson(g.Data);float previous=Time.timeScale;
        try {
            g.SetOpen(true);Require(Time.timeScale==0&&GlobalSettings.BlocksInput,"Open must pause and block inputs");
            Require(g.GetComponentsInChildren<Slider>().Length==3,"Three audio sliders required");
            g.GetComponentsInChildren<Slider>()[0].value=.42f;Require(Mathf.Abs(AudioListener.volume-.42f)<.001f,"Master slider not routed");
            g.Data.mute=true;g.Apply();Require(AudioListener.volume==0,"Mute not applied");
            g.Data.mute=false;g.Data.ambient=false;g.Apply();Require(GlobalSettings.EnvironmentVolume==0,"Ambient not muted");
            g.Data.vsync=false;g.Data.frameRate=30;g.Apply();Require(QualitySettings.vSyncCount==0&&Application.targetFrameRate==30,"Display options not applied");
            g.Save();Require(Mathf.Abs(JsonUtility.FromJson<GlobalSettings.Preferences>(PlayerPrefs.GetString("Rpg.GlobalSettings.v1")).master-.42f)<.001f,"Save failed");
            g.SetOpen(false);Require(Mathf.Approximately(previous,Time.timeScale),"Close did not restore time scale");
            var owner=new GameObject("Pause nesting verification");MenuPause.SetPaused(owner,true);g.SetOpen(true);g.SetOpen(false);Require(Time.timeScale==0,"Settings released another menu pause");MenuPause.SetPaused(owner,false);UnityEngine.Object.Destroy(owner);
            var font=Resources.Load<TMPro.TMP_FontAsset>("Settings/Chinese");Require(font!=null,"Missing build font resource");
        }finally{JsonUtility.FromJsonOverwrite(original,g.Data);g.Apply();g.Save();g.SetOpen(true);}
    }
}

