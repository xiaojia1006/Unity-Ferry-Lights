using UnityEngine;

/// <summary>玩家属性集合：生存三项来自 StatsManager，体力/负重/熟练/声望在这里统一管理。
/// 面板只负责显示，数值全部从这里取，避免每个 UI 各算一套。</summary>
public static class PlayerStats
{
    const string KeyFish = "Rpg.FishCaught";
    const string KeyCook = "Rpg.Cooked";
    const string KeyQuest = "Rpg.Quests";

    public static int FishCaught { get { return PlayerPrefs.GetInt(AccountKey(KeyFish), 0); } }
    public static int Cooked { get { return PlayerPrefs.GetInt(AccountKey(KeyCook), 0); } }
    public static int Quests { get { return PlayerPrefs.GetInt(AccountKey(KeyQuest), 0); } }

    public static void AddFish() { PlayerPrefs.SetInt(AccountKey(KeyFish), FishCaught + 1); PlayerPrefs.Save(); }
    public static void AddCook() { PlayerPrefs.SetInt(AccountKey(KeyCook), Cooked + 1); PlayerPrefs.Save(); }
    public static void AddQuest() { PlayerPrefs.SetInt(AccountKey(KeyQuest), Quests + 1); PlayerPrefs.Save(); }

    public static string AccountKey(string key) => key + ".v2." + System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(string.IsNullOrEmpty(LoginSession.Account)?"guest":LoginSession.Account));
    public static bool AddQuest(string taskId)
    {
        if(string.IsNullOrWhiteSpace(taskId))return false;
        string key=AccountKey("Rpg.Task."+taskId);
        if(PlayerPrefs.GetInt(key,0)!=0)return false;
        PlayerPrefs.SetInt(key,1);AddQuest();return true;
    }
    public static float FishingWindowBonus => Mathf.Min(.5f,FishingLevel*.1f);
    public static int CookingHealBonus => Mathf.Min(5,CookingLevel);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession(){Stamina=StaminaMax;}
    public static void ResetStamina(){Stamina=StaminaMax;}
    // 体力：冲刺消耗、静止回复
    public const float StaminaMax = 100f;
    public static float Stamina { get; private set; } = 100f;
    public static bool CanSprint { get { return Stamina > 1f; } }
    public static void DrainStamina(float amount) { Stamina = Mathf.Max(0f, Stamina - amount); }
    public static void RegenStamina(float amount) { Stamina = Mathf.Min(StaminaMax, Stamina + amount); }

    // 负重：背包里所有物品的件数
    public static int CarryMax => 20 + ReputationLevel*2;
    public static int Carry
    {
        get
        {
            var inv = InventortyManager.Instance;
            if (inv == null || inv.itemSlots == null) return 0;
            int total = 0;
            foreach (var slot in inv.itemSlots)
                if (slot != null && slot.itemSO != null) total += slot.quantity;
            return total;
        }
    }
    public static bool Overloaded { get { return Carry > CarryMax; } }

    public static int FishingLevel { get { return FishCaught / 10; } }
    public static int CookingLevel { get { return Cooked / 5; } }

    static readonly string[] ReputationNames = { "NEW", "KNOWN", "TRUSTED", "FRIEND" };
    public static int ReputationLevel
    {
        get
        {
            if (Quests >= 4) return 3;
            if (Quests >= 3) return 2;
            if (Quests >= 1) return 1;
            return 0;
        }
    }

    /// <summary>面板的 8 行文本，顺序固定。</summary>
    public static string[] Rows()
    {
        int hp = StatsManager.Instance != null ? StatsManager.Instance.maxHealth : 0;
        int atk = StatsManager.Instance != null ? NightCombat2D.PlayerDamage(StatsManager.Instance.damage) : 0;
        int spd = StatsManager.Instance != null ? StatsManager.Instance.speed : 0;

        float range = StatsManager.Instance != null ? StatsManager.Instance.weaponRange : 0f;
        float knock = StatsManager.Instance != null ? StatsManager.Instance.knockbackForce : 0f;

        return new[]
        {
            "生命:" + Value("生命"),
            "攻击:" + atk,
            "移速:" + spd + (Overloaded ? " 超重" : ""),
            "体力:" + Mathf.RoundToInt(Stamina) + "/" + Mathf.RoundToInt(StaminaMax),
            "负重:" + Carry + "/" + CarryMax,
            "钓鱼:" + FishingLevel + "级",
            "烹饪:" + CookingLevel + "级",
            "声望:" + ReputationLevel,
            "射程:" + range.ToString("0.0"),
            "击退:" + knock.ToString("0")
        };
    }

    /// <summary>按面板标签取当前数值文本，标签改顺序也不会错位。</summary>
    public static string Value(string label)
    {
        int hp = StatsManager.Instance != null ? StatsManager.Instance.maxHealth : 0;
        int atk = StatsManager.Instance != null ? NightCombat2D.PlayerDamage(StatsManager.Instance.damage) : 0;
        int spd = StatsManager.Instance != null ? StatsManager.Instance.speed : 0;
        float range = StatsManager.Instance != null ? StatsManager.Instance.weaponRange : 0f;
        float knock = StatsManager.Instance != null ? StatsManager.Instance.knockbackForce : 0f;

        switch (label)
        {
            case "生命": return StatsManager.Instance == null ? "0/0" : StatsManager.Instance.currentHealth + "/" + hp;
            case "攻击": return atk.ToString();
            case "移速": return spd.ToString();
            case "体力": return Mathf.RoundToInt(Stamina) + "/" + Mathf.RoundToInt(StaminaMax);
            case "负重": return Carry + "/" + CarryMax;
            case "钓鱼": return FishingLevel + "级";
            case "烹饪": return CookingLevel + "级";
            case "声望": return ReputationLevel + "级";
            case "射程": return range.ToString("0.0");
            case "击退": return knock.ToString("0");
            default: return "";
        }
    }
}
