using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleSkillTree : MonoBehaviour
{
    public CanvasGroup statsCanvas;
    private bool skillTreeOpen = false;

   
    void Update()
    {
        if (BackpackUI.BlocksOtherInput) return;
        if (statsCanvas != null && Input.GetButtonDown("ToggleSkillTree"))
        {
            if (skillTreeOpen)
            {
                MenuPause.SetPaused(this, false);
                statsCanvas.alpha = 0;
                statsCanvas.interactable = false;
                statsCanvas.blocksRaycasts = false;
                skillTreeOpen = false;
            }
            else
            {
                MenuPause.SetPaused(this, true);
                statsCanvas.alpha = 1;
                statsCanvas.interactable = true;
                statsCanvas.blocksRaycasts = true;
                skillTreeOpen = true;
            }
        }
    }

    private void OnDisable()
    {
        skillTreeOpen = false;
        MenuPause.SetPaused(this, false);
        if (statsCanvas != null)
        {
            statsCanvas.alpha = 0f;
            statsCanvas.interactable = false;
            statsCanvas.blocksRaycasts = false;
        }
    }
}
