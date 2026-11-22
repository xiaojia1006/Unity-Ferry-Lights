using UnityEngine;

public class SkillManager : MonoBehaviour
{
   
    private void OnEnable()
    {
        SkillSlot.OnAbilityPointSpent += HandleAbilityPointSpent;
    }

    private void OnDisable()
    {
        SkillSlot.OnAbilityPointSpent -= HandleAbilityPointSpent;
    }

    private void HandleAbilityPointSpent(SkillSlot slot)
    {
        if (slot == null || slot.skillSO == null)
            return;

        string skillName = slot.skillSO.skillName;

        switch (skillName)
        {
            case "Vitality":
                if (StatsManager.Instance != null)
                {
                    StatsManager.Instance.UpdateMaxHealth(1);
                }
                break;
           


            default:
                Debug.LogWarning("Unknown skill: " + skillName);
                break;
        }
    }
}
