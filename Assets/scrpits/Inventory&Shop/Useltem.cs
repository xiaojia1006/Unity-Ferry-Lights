using System.Collections;
using UnityEngine;

public class Useltem : MonoBehaviour
{
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

        if (itemSO.currentHealth != 0)
        {
            PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.ChangeHealth(itemSO.currentHealth);
            }
            else
            {
                StatsManager.Instance.UpdateHealth(itemSO.currentHealth);
            }
        }

        if (itemSO.maxHealth > 0)
        {
            StatsManager.Instance.UpdateMaxHealth(
                itemSO.maxHealth
            );
        }

        if (itemSO.speed > 0)
        {
            StatsManager.Instance.UpdateSpeed(
                itemSO.speed
            );
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
                EffectTimer(itemSO, itemSO.duration)
            );
        }
        return true;
    }

    private IEnumerator EffectTimer(
        ItemSO itemSO,
        float duration
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

        if (itemSO.speed > 0)
        {
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
