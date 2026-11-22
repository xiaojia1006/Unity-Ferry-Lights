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
        if (GlobalSettings.BlocksInput) return;
        if (Input.GetButtonDown("ChangeEquipment"))
        {
            combat.enabled = !combat.enabled;
            bow.enabled = !bow.enabled;
        }
    }
}
