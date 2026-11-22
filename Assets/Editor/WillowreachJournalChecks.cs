using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class WillowreachJournalChecks
{
    [MenuItem("Tools/RPG/Scene Expansion/Check Journal And Map In Play")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        var w=UnityEngine.Object.FindObjectOfType<WillowreachWorld>();var root=w.transform.Find("Willowreach HUD");
        var lines=new List<string>();Action<bool,string> check=(ok,s)=>lines.Add((ok?"PASS ":"FAIL ")+s);
        var map=root.Find("Field Map");var chart=map.Find("Terrain");var oldPosition=w.player.position;float scale=Time.timeScale;
        var update=typeof(WillowreachWorld).GetMethod("UpdateFieldMap",BindingFlags.NonPublic|BindingFlags.Instance);
        try
        {
            foreach(var name in new[]{"Travel Journal","Interaction Backing","Message Backing"})
            {var image=root.Find(name).GetComponent<Image>();check(image.sprite==w.journalSprite&&image.color==Color.white&&image.type==Image.Type.Sliced,"Original-art parchment without dark tint: "+name);}
            check(root.GetComponent<GraphicRaycaster>()!=null,"Atlas can receive UI pointer clicks");
            w.OpenFieldMap();check(map.gameObject.activeSelf&&Time.timeScale==0,"Opening atlas pauses gameplay");
            var points=new[]{new Vector3(-36,-24,0),Vector3.zero,new Vector3(36,24,0)};
            foreach(var p in points){w.player.position=p;update.Invoke(w,null);var marker=chart.Find("You").GetComponent<RectTransform>();check(Vector2.Distance(marker.anchoredPosition,new Vector2(p.x/76*930,p.y/52*636))<.01f,"Player map coordinate alignment at "+p);}
            var buttons=chart.GetComponentsInChildren<Button>();check(buttons.Length>=8,"Shops surveys workshop and adventure destinations are clickable");
            buttons[0].onClick.Invoke();update.Invoke(w,null);check(chart.Find("Waypoint").gameObject.activeSelf,"Selecting a destination places waypoint");
            map.Find("Destinations/Clear waypoint").GetComponent<Button>().onClick.Invoke();check(!chart.Find("Waypoint").gameObject.activeSelf,"Clearing waypoint removes map marker");
            map.Find("Close map").GetComponent<Button>().onClick.Invoke();check(!map.gameObject.activeSelf&&Time.timeScale==scale,"Close button restores previous game speed");
            foreach(var t in root.GetComponentsInChildren<Text>(true))if(t.name=="Label")Canvas.ForceUpdateCanvases();
            check(w.mapSprite!=null&&w.mapSprite.texture.width==608&&w.mapSprite.texture.height==416,"Atlas terrain preserves world aspect ratio");
        }
        finally
        {w.player.position=oldPosition;map.gameObject.SetActive(false);MenuPause.SetPaused(w,false);update.Invoke(w,null);File.WriteAllLines("Logs/WillowreachJournalChecks.txt",lines);Debug.Log(string.Join("\n",lines));}
    }
}
