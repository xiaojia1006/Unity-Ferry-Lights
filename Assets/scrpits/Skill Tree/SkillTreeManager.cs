using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class SkillTreeManager : MonoBehaviour
{
    [FormerlySerializedAs("SkillSlots")]
    public SkillSlot[] skillSlots;

    public TMP_Text pointsText;
    public int availablePoints;

    private void OnEnable()
    {
        SkillSlot.OnSkillMaxed += HandleSkillMaxed;
        ExpManager.OnLevelUP += UpdateAbilityPoints;
    }

    private void OnDisable()
    {
        SkillSlot.OnSkillMaxed -= HandleSkillMaxed;
        ExpManager.OnLevelUP -= UpdateAbilityPoints;
    }

    private void Start()
    {
        if (skillSlots != null)
        {
            foreach (SkillSlot slot in skillSlots)
            {
                if (slot == null || slot.skillButton == null)
                    continue;

                slot.skillButton.onClick.AddListener(
                    () => UpgradeSkill(slot)
                );

                slot.RefreshUnlockState();
            }
        }

        UpdateAbilityPoints(0);
    }

    private void UpgradeSkill(SkillSlot slot)
    {
        if (slot == null || availablePoints <= 0)
            return;

        if (slot.TryUpgradeSkill())
            UpdateAbilityPoints(-1);
    }

    private void HandleSkillMaxed(SkillSlot skillSlot)
    {
        if (skillSlots == null)
            return;

        foreach (SkillSlot slot in skillSlots)
        {
            if (slot == null)
                continue;

            slot.RefreshUnlockState();

            if (!slot.isUnlocked && slot.CanUnlockSkill())
                slot.Unlock();
        }
    }

    public void UpdateAbilityPoints(int amount)
    {
        availablePoints = Mathf.Max(
            0,
            availablePoints + amount
        );

        if (pointsText != null)
            pointsText.text = "Points:" + availablePoints;
    }

    // Kept so existing UnityEvent references still work.
    public void UpdateAbiltyPoints(int amount)
    {
        UpdateAbilityPoints(amount);
    }
}
