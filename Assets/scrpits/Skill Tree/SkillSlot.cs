using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlot : MonoBehaviour
{
    public List<SkillSlot> prerequisteSkillSlots;
    public SkillSO skillSO;

    public int currentLevel;
    public bool isUnlocked;
    public Image skillIcon;
    public Button skillButton;
    public TMP_Text skillLevelText;

    public Color unlockedColor = Color.white;
    public Color lockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

    internal static Action<SkillSlot> OnAbilityPointSpent;
    public static event Action<SkillSlot> OnSkillMaxed;

    private void Start()
    {
        RefreshUnlockState();
    }

    private void OnValidate()
    {
        UpdateUI();
    }

    public bool TryUpgradeSkill()
    {
        if (
            !isUnlocked ||
            skillSO == null ||
            currentLevel >= skillSO.maxLevel
        )
        {
            return false;
        }

        currentLevel++;
        UpdateUI();
        OnAbilityPointSpent?.Invoke(this);

        if (currentLevel >= skillSO.maxLevel)
            OnSkillMaxed?.Invoke(this);

        return true;
    }

    public bool CanUnlockSkill()
    {
        if (
            prerequisteSkillSlots == null ||
            prerequisteSkillSlots.Count == 0
        )
        {
            return false;
        }

        foreach (SkillSlot prerequisite in prerequisteSkillSlots)
        {
            if (
                prerequisite == null ||
                prerequisite.skillSO == null ||
                prerequisite.currentLevel <
                prerequisite.skillSO.maxLevel
            )
            {
                return false;
            }
        }

        return true;
    }

    public void RefreshUnlockState()
    {
        if (!isUnlocked && CanUnlockSkill())
            isUnlocked = true;

        UpdateUI();
    }

    public void Unlock()
    {
        isUnlocked = true;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (skillButton != null)
        {
            skillButton.interactable =
                isUnlocked && skillSO != null;

            if (skillButton.image != null)
            {
                skillButton.image.color =
                    isUnlocked ? unlockedColor : lockedColor;
            }
        }

        if (skillSO == null)
        {
            if (skillIcon != null)
                skillIcon.color = lockedColor;

            if (skillLevelText != null)
            {
                skillLevelText.text = "No Skill";
                skillLevelText.color = lockedColor;
            }

            return;
        }

        if (skillIcon != null)
            skillIcon.sprite = skillSO.skillIcon;

        if (skillIcon != null)
        {
            skillIcon.color =
                isUnlocked ? unlockedColor : lockedColor;
        }

        if (skillLevelText != null)
        {
            skillLevelText.text = isUnlocked
                ? currentLevel + "/" + skillSO.maxLevel
                : "Locked";

            skillLevelText.color =
                isUnlocked ? unlockedColor : lockedColor;
        }
    }
}
