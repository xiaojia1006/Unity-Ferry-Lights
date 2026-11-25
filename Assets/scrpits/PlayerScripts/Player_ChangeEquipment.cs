using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_ChangeEquipment : MonoBehaviour
{
    // Start is called before the first frame update
    public player_combat combat;
    public Player_Bow bow;

    // Update is called once per frame
    void Update()
    {
        if (GlobalSettings.BlocksInput||BackpackUI.BlocksOtherInput||Time.timeScale<=0||FishingRig2D.Busy) return;
        if (Input.GetButtonDown("ChangeEquipment"))
        {
            if(combat==null||bow==null)return;
            bool useBow=!bow.enabled;
            combat.enabled=!useBow;bow.enabled=useBow;
        }
    }
}
