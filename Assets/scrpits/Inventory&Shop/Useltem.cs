using System.Collections;
using UnityEngine;

public class Useltem : MonoBehaviour
{
    // 食物带来的速度加成有上限：同时最多叠 3 层，避免连续吃南瓜把移速拉爆
    const int MaxSpeedStacks = 3;
    static int speedStacks;
    public static int SpeedStacks => speedStacks;

    public void ApplyItemEffects(ItemSO itemSO)
    {
        TryApplyItemEffects(itemSO);
    }

    public bool TryApplyItemEffects(ItemSO itemSO)
    {
        if (itemSO == null || StatsManager.Instance == null)
            return false;

        bool hasStatEffect = itemSO.maxHealth > 0 || itemSO.speed > 0 || itemSO.damage > 0;
        bool canHeal = itemSO.currentHealth > 0 &&
            StatsManager.Instance.currentHealth < StatsManager.Instance.maxHealth;
        if (!hasStatEffect && !canHeal && itemSO.currentHealth >= 0)
            return false;

        int heal=itemSO.currentHealth;
        if(heal>0 && (itemSO==Resources.Load<ItemSO>("Items/mushroom_soup") || itemSO==Resources.Load<ItemSO>("Items/pumpkin_soup")))heal+=PlayerStats.CookingHealBonus;
        if (heal != 0)
        {
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.ChangeHealth(heal);
            }
            else
            {
                StatsManager.Instance.UpdateHealth(heal);
            }
        }

        if (itemSO.maxHealth > 0)
        {
            StatsManager.Instance.UpdateMaxHealth(
                itemSO.maxHealth
            );
        }

        bool speedApplied = false;
        if (itemSO.speed > 0)
        {
            if (speedStacks < MaxSpeedStacks)
            {
                speedStacks++;
                speedApplied = true;
                StatsManager.Instance.UpdateSpeed(
                    itemSO.speed
                );
            }
        }

        if (itemSO.damage > 0)
        {
            StatsManager.Instance.UpdateDamage(
                itemSO.damage
            );
        }

        if (itemSO.duration > 0)
        {
            StartCoroutine(
                EffectTimer(itemSO, itemSO.duration, speedApplied)
            );
        }
        // 只有速度效果、且已经叠满时，这一口不生效
        if (itemSO.speed > 0 && !speedApplied &&
            itemSO.currentHealth <= 0 && itemSO.maxHealth <= 0 && itemSO.damage <= 0)
            return false;
        return true;
    }

    private IEnumerator EffectTimer(
        ItemSO itemSO,
        float duration,
        bool speedApplied
    )
    {
        yield return new WaitForSeconds(duration);

        if (StatsManager.Instance == null)
            yield break;

     

        if (itemSO.maxHealth > 0)
        {
            StatsManager.Instance.UpdateMaxHealth(
                -itemSO.maxHealth
            );
        }

        if (itemSO.speed > 0 && speedApplied)
        {
            speedStacks = Mathf.Max(0, speedStacks - 1);
            StatsManager.Instance.UpdateSpeed(
                -itemSO.speed
            );
        }

        if (itemSO.damage > 0)
        {
            StatsManager.Instance.UpdateDamage(
                -itemSO.damage
            );
        }
    }
}
