using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public static class ExperienceHudSetup
{
    static ExperienceHudSetup()
    {
        EditorApplication.delayCall+=Refresh;
        EditorSceneManager.sceneOpened+=(scene,mode)=>EditorApplication.delayCall+=Refresh;
    }
    [MenuItem("Tools/RPG/Refresh Experience HUD")]
    public static void Refresh()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        foreach(var manager in UnityEngine.Object.FindObjectsOfType<ExpManager>(true))
        {
            manager.UpdataUI();
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
    }

    [MenuItem("Tools/RPG/Check Experience HUD %#F6")]
    public static void Check()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode) return;
        var log=new List<string>();
        var root=new GameObject("Experience HUD check");
        int awarded=0;
        Action<int> onLevel=points=>awarded+=points;
        try
        {
            var manager=root.AddComponent<ExpManager>();
            var bar=new GameObject("Bar",typeof(RectTransform),typeof(Slider));
            bar.transform.SetParent(root.transform,false);
            manager.expSlider=bar.GetComponent<Slider>();
            var label=new GameObject("Level",typeof(RectTransform),typeof(TextMeshProUGUI));
            label.transform.SetParent(root.transform,false);
            manager.currentLeveText=label.GetComponent<TextMeshProUGUI>();
            manager.level=1; manager.expToLevel=12; manager.expGrowMultiplier=1.2f;
            manager.UpdataUI();
            Require(manager.currentLeveText.text=="Lv.1   EXP 0/12","Initial level and progress visible",log);
            Require(manager.currentLeveText.color==Color.white && !manager.currentLeveText.raycastTarget && label.transform.parent==bar.transform,
                "White label overlays bar without blocking clicks",log);
            ExpManager.OnLevelUP+=onLevel;
            manager.GainExperience(5);
            Require(manager.level==1 && manager.expSlider.value==5 && manager.currentLeveText.text.Contains("5/12"),"Partial experience updates label and fill",log);
            manager.GainExperience(7);
            Require(manager.level==2 && manager.currentExp==0 && manager.expToLevel==14 && awarded==1,"Exact threshold levels up and grants one skill point",log);
            manager.GainExperience(32);
            Require(manager.level==4 && manager.currentExp==1 && manager.expToLevel==20 && awarded==3 && manager.currentLeveText.text=="Lv.4   EXP 1/20",
                "Multiple levels retain overflow and refresh HUD",log);
            manager.GainExperience(-5);
            Require(manager.currentExp==1 && awarded==3,"Negative rewards ignored",log);
            manager.expSlider=null; manager.currentLeveText=null; manager.UpdataUI();
            log.Add("PASS: Missing optional UI references do not throw");
        }
        catch(Exception e) { log.Add("FAIL: "+e); }
        finally
        {
            ExpManager.OnLevelUP-=onLevel;
            UnityEngine.Object.DestroyImmediate(root);
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/ExperienceHudChecks.txt",log);
        }
    }
    private static void Require(bool ok,string message,List<string> log)
    {
        if(!ok) throw new InvalidOperationException(message);
        log.Add("PASS: "+message);
    }
}
