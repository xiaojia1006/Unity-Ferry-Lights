using UnityEngine;
using TMPro;

// Keep serialized scene references; bind each authored row by its label, never its array index.
public class StatsUI : MonoBehaviour
{
    public GameObject[] statsSlots;
    public CanvasGroup statsCanvas;
    bool statsOpen;
    float nextRefresh;
    static readonly string[] Labels={"生命","攻击","移速","体力","负重","钓鱼","烹饪","声望","射程","击退"};
    void Start(){SetVisible(false);UpdataAllStats();}
    void Update()
    {
        if(statsOpen && Time.unscaledTime>=nextRefresh){UpdataAllStats();nextRefresh=Time.unscaledTime+.15f;}
        if(GlobalSettings.BlocksInput||BackpackUI.BlocksOtherInput||RPG1Activities.BlocksMenuInput||statsCanvas==null)return;
        if(Input.GetButtonDown("ToggleStats")){SetVisible(!statsOpen);if(statsOpen)UpdataAllStats();}
    }
    void SetVisible(bool open)
    {
        statsOpen=open;MenuPause.SetPaused(this,open);
        if(statsCanvas==null)return;
        statsCanvas.alpha=open?1:0;statsCanvas.blocksRaycasts=open;statsCanvas.interactable=open;
    }
    void OnDisable(){SetVisible(false);}
    public void UpdateDamage(){UpdataAllStats();}
    public void UpdateSpeed(){UpdataAllStats();}
    public void UpdataAllStats()
    {
        if(statsSlots==null)return;
        foreach(var slot in statsSlots)
        {
            if(slot==null)continue;
            foreach(var text in slot.GetComponentsInChildren<TMP_Text>(true))RefreshRow(text);
        }
    }
    public static bool RefreshRow(TMP_Text text)
    {
        if(text==null||string.IsNullOrWhiteSpace(text.text))return false;
        string value=text.text.Trim();
        foreach(var label in Labels)
        {
            if(value!=label&&!value.StartsWith(label+":")&&!value.StartsWith(label+"："))continue;
            text.text=label+":"+PlayerStats.Value(label);return true;
        }
        // Older scenes still use English labels for the first two rows.
        if(value.StartsWith("Damage:")||value.StartsWith("Damage：")){text.text="攻击:"+PlayerStats.Value("攻击");return true;}
        if(value.StartsWith("Speed:")||value.StartsWith("Speed：")){text.text="移速:"+PlayerStats.Value("移速");return true;}
        return false;
    }
}
