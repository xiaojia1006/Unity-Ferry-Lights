using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class StatsUI : MonoBehaviour
{
    // Start is called before the first frame update
    public GameObject[] statsSlots;
    public CanvasGroup statsCanvas;
    
    private bool statsOpen = false;
    private void Start()
    {
        UpdataAllStats();
    }
    private void Update()
    {
        if (GlobalSettings.BlocksInput) return;
        if (BackpackUI.BlocksOtherInput || RPG1Activities.BlocksMenuInput || statsCanvas == null || !Input.GetButtonDown("ToggleStats"))
            return;
        statsOpen = !statsOpen;
        MenuPause.SetPaused(this, statsOpen);
        statsCanvas.alpha = statsOpen ? 1f : 0f;
        statsCanvas.blocksRaycasts = statsOpen;
        statsCanvas.interactable = statsOpen;
        if (statsOpen)
            UpdataAllStats();
    }

    private void OnDisable()
    {
        statsOpen = false;
        MenuPause.SetPaused(this, false);
        if (statsCanvas != null)
        {
            statsCanvas.alpha = 0f;
            statsCanvas.blocksRaycasts = false;
            statsCanvas.interactable = false;
        }
    }

    public void UpdateDamage()
    {
        statsSlots[0].GetComponentInChildren<TMP_Text>().text= "Damage:"+StatsManager.Instance.damage; 
    }
    public void UpdateSpeed()
    {
        statsSlots[1].GetComponentInChildren<TMP_Text>().text = "Speed:" + StatsManager.Instance.speed;
    }
    public void UpdataAllStats ()
    {
        UpdateDamage();
        UpdateSpeed();
    }
}
