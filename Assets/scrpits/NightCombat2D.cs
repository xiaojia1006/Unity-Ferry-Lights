using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Transient hit-time modifiers. Never mutate saved/equipment statistics.</summary>
public static class NightCombat2D
{
    public const int EnemyBonus = 3;
    public const int PlayerPenalty = 2;
    public static bool Active => DayNight2D.Instance != null && DayNight2D.Instance.isActiveAndEnabled &&
        DayNight2D.Instance.Current == DayNight2D.Phase.Night && SceneManager.GetActiveScene().name == "RPG2";
    public static int EnemyDamage(int baseDamage) => baseDamage <= 0 ? 0 : baseDamage + (Active ? EnemyBonus : 0);
    public static int PlayerDamage(int baseDamage) => baseDamage <= 0 ? 0 : Mathf.Max(1,baseDamage - (Active ? PlayerPenalty : 0));
}
