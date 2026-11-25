using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class ExpManager : MonoBehaviour
{
    // Start is called before the first frame update
    public int level = 1;
    public int currentExp;
    public int expToLevel;
    public float expGrowMultiplier = 1.2f;
    public Slider expSlider;
    public TMP_Text currentLeveText;
    Material levelMaterial;
    void OnDestroy(){if(levelMaterial!=null)Destroy(levelMaterial);}

    public static event Action<int> OnLevelUP;
    private void OnEnable()
    {
        Enemy_Health.OnMonsterDefeated += GainExperience;
        InventortyManager.OnExperienceGained += GainExperience;
    }
    private void OnDisable()
    {
        Enemy_Health.OnMonsterDefeated -= GainExperience;
        InventortyManager.OnExperienceGained -= GainExperience;
    }
    private void Start()
    {
        level = Mathf.Max(1, level);
        expToLevel = Mathf.Max(1, expToLevel);
        UpdataUI();
    }
    public void GainExperience(int amount)
    {
        if (amount <= 0)
            return;
        level = Mathf.Max(1, level);
        expToLevel = Mathf.Max(1, expToLevel);
        currentExp += amount;
        while (currentExp >= expToLevel)
        {
            LevelUP();
        }
        UpdataUI();
    }
    private void LevelUP ()
    {
        level++;
        currentExp -= expToLevel;
        expToLevel = Mathf.Max(1, Mathf.RoundToInt(expToLevel * Mathf.Max(1f, expGrowMultiplier)));
        OnLevelUP?.Invoke(1);
    } 
    public void UpdataUI() 
    {
        if (expSlider != null)
        {
            expSlider.minValue = 0;
            expSlider.maxValue = Mathf.Max(1, expToLevel);
            expSlider.value = currentExp;
            expSlider.interactable = false;
            expSlider.navigation = new Navigation { mode = Navigation.Mode.None };
            foreach (var graphic in expSlider.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
        }
        if(currentLeveText==null&&expSlider!=null)currentLeveText=expSlider.GetComponentInChildren<TMP_Text>(true);
        if (currentLeveText == null) return;
        currentLeveText.gameObject.SetActive(true);
        currentLeveText.enabled=true;
        currentLeveText.alpha=1;
        currentLeveText.maxVisibleCharacters=int.MaxValue;
        currentLeveText.maxVisibleWords=int.MaxValue;
        currentLeveText.maxVisibleLines=int.MaxValue;
        currentLeveText.overflowMode=TextOverflowModes.Overflow;
        currentLeveText.canvasRenderer.SetAlpha(1);
        currentLeveText.text = "Lv." + Mathf.Max(1, level) + "   EXP " + currentExp + "/" + Mathf.Max(1, expToLevel);
        // The shared Bangers material is black for parchment UI. Give only this dark-bar label a white face.
        if(levelMaterial==null&&currentLeveText.fontSharedMaterial!=null)
        {levelMaterial=new Material(currentLeveText.fontSharedMaterial);levelMaterial.name="Experience label white face";currentLeveText.fontSharedMaterial=levelMaterial;}
        if(levelMaterial!=null&&levelMaterial.HasProperty("_FaceColor"))levelMaterial.SetColor("_FaceColor",Color.white);
        currentLeveText.color = Color.white;
        currentLeveText.fontStyle = FontStyles.Bold;
        currentLeveText.alignment = TextAlignmentOptions.Center;
        currentLeveText.enableAutoSizing = true;
        currentLeveText.fontSizeMin = 14;
        currentLeveText.fontSizeMax = 25;
        currentLeveText.enableWordWrapping = false;
        currentLeveText.raycastTarget = false;
        var shadow = currentLeveText.GetComponent<Shadow>();
        if (shadow == null) shadow = currentLeveText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0,0,0,0.95f);
        shadow.effectDistance = new Vector2(1,-1);
        if (expSlider == null) return;
        // Keep the label above the fill and tied to the bar at every Canvas resolution.
        var rect = currentLeveText.rectTransform;
        rect.SetParent(expSlider.transform, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f,0.5f);
        rect.offsetMin = new Vector2(8,0);
        rect.offsetMax = new Vector2(-8,0);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
        rect.SetAsLastSibling();
    }
}
