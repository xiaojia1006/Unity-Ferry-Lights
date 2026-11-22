using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestRewardSlot : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text quantityText;

    public void DisplayReward(Sprite icon, int quantity)
    {
        if (itemImage != null)
        {
            itemImage.sprite = icon;
            itemImage.enabled = icon != null;
        }

        if (quantityText != null)
            quantityText.text = quantity.ToString();
    }
}
