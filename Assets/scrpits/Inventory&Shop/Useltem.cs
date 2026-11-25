using System.Collections;
using UnityEngine;

public class Useltem : MonoBehaviour
{
    // 食物带来的速度加成有上限：同时最多叠 3 层，避免连续吃南瓜把移速拉爆
    const int MaxSpeedStacks = 3;
    static int speedStacks;
    public static int SpeedStacks => speedStacks;
    int temporaryHealth,temporarySpeed,temporaryDamage;
    public void RemoveTemporaryFromSnapshot(SaveSystem.Data data){data.maxHp-=temporaryHealth;data.hp=Mathf.Min(data.hp,data.maxHp);data.speed-=temporarySpeed;data.damage-=temporaryDamage;}
    readonly System.Collections.Generic.List<System.Action> undo = new System.Collections.Generic.List<System.Action>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStacks(){speedStacks=0;}
    public void ClearTemporaryEffects(){StopAllCoroutines();foreach(var action in undo.ToArray())action();undo.Clear();}
    void OnDisable(){ClearTemporaryEffects();}


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
        var stats=StatsManager.Instance;
        int health=itemSO.maxHealth,speed=speedApplied?itemSO.speed:0,damage=itemSO.damage;
        temporaryHealth+=health;temporarySpeed+=speed;temporaryDamage+=damage;
        System.Action revert=()=>{
            temporaryHealth-=health;temporarySpeed-=speed;temporaryDamage-=damage;
            if(speedApplied)speedStacks=Mathf.Max(0,speedStacks-1);
            if(stats==null)return;
            if(health>0)stats.UpdateMaxHealth(-health);
            if(speed>0)stats.UpdateSpeed(-speed);
            if(damage>0)stats.UpdateDamage(-damage);
        };
        undo.Add(revert);
        yield return new WaitForSeconds(duration);
        if(undo.Remove(revert))revert();
    }
}
