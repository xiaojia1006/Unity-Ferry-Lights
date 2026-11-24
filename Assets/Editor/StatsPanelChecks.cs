using System;
using System.IO;
using UnityEngine;
using TMPro;
using UnityEditor;

[InitializeOnLoad]
public static class StatsPanelChecks
{
    static StatsPanelChecks(){EditorApplication.update+=Tick;}
    static void Require(bool value,string message){if(!value)throw new Exception(message);}
    static void Tick()
    {
        const string path="Temp/StatsPanel.request";
        if(!File.Exists(path)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;
        try{File.Delete(path);}catch(IOException){return;}
        var root=new GameObject("Stats panel verification");StatsManager previous=StatsManager.Instance;
        try
        {
            var data=root.AddComponent<StatsManager>();StatsManager.Instance=data;data.damage=9;data.speed=4;data.maxHealth=50;data.currentHealth=23;
            var panel=root.AddComponent<StatsUI>();
            var speed=new GameObject("Speed row",typeof(RectTransform),typeof(TextMeshProUGUI));speed.transform.SetParent(root.transform);speed.GetComponent<TMP_Text>().text="移速: 999";
            var attack=new GameObject("Attack row",typeof(RectTransform),typeof(TextMeshProUGUI));attack.transform.SetParent(root.transform);attack.GetComponent<TMP_Text>().text="攻击: 999";attack.SetActive(false);
            panel.statsSlots=new[]{speed,null,attack};panel.UpdataAllStats();
            Require(speed.GetComponent<TMP_Text>().text=="移速:4","Reordered speed row incorrect");
            Require(attack.GetComponent<TMP_Text>().text=="攻击:"+NightCombat2D.PlayerDamage(9),"Inactive attack row incorrect");
            Require(PlayerStats.Value("生命")=="23/50","Health must display current/max");
            var text=speed.GetComponent<TMP_Text>();text.text="攻击敌人以获取奖励";Require(!StatsUI.RefreshRow(text)&&text.text=="攻击敌人以获取奖励","Instruction text overwritten");
            foreach(string label in new[]{"钓鱼","烹饪","声望"}){text.text=label+":旧说明";StatsUI.RefreshRow(text);Require(!text.text.Contains("（")&&!text.text.Contains("/")&&!text.text.Contains("\n"),"Compact row overflow: "+label);}
            text.text="Speed:3";Require(StatsUI.RefreshRow(text)&&text.text=="移速:4","Legacy label compatibility");
            panel.statsSlots=null;panel.UpdataAllStats();StatsManager.Instance=null;Require(PlayerStats.Value("生命")=="0/0","Missing player handling");
            File.WriteAllText("Logs/StatsPanelChecks.txt","PASS: reordered rows, inactive labels, null slots, legacy labels, current/max HP, effective damage, non-row text isolation, missing player\n");
        }
        catch(Exception e){File.WriteAllText("Logs/StatsPanelChecks.txt","FAIL "+e);Debug.LogException(e);}
        finally{StatsManager.Instance=previous;UnityEngine.Object.DestroyImmediate(root);}
    }
}
