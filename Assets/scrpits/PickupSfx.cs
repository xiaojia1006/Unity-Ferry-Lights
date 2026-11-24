using UnityEngine;

/// <summary>One pickup sound per item. Coins, experience, food, bone and wood each get their own
/// recording, and anything new falls back to a stable per-name pick so two items never sound alike.</summary>
public static class PickupSfx
{
    public static bool Suppress;

    static AudioSource source;
    static AudioClip coins, gem, misc1, misc2, misc3, stone, wood;

    static void Load()
    {
        if (source != null) return;
        var go = new GameObject("Pickup SFX");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.playOnAwake = false; source.loop = false; source.spatialBlend = 0f;

        coins = Resources.Load<AudioClip>("Audio/Pickup/coins");
        gem = Resources.Load<AudioClip>("Audio/Pickup/gem");
        misc1 = Resources.Load<AudioClip>("Audio/Pickup/misc_1");
        misc2 = Resources.Load<AudioClip>("Audio/Pickup/misc_2");
        misc3 = Resources.Load<AudioClip>("Audio/Pickup/misc_3");
        stone = Resources.Load<AudioClip>("Audio/Pickup/stone");
        wood = Resources.Load<AudioClip>("Audio/Pickup/wood");
    }

    public static void Play(ItemSO item)
    {
        if (item == null || Suppress) return;
        Load();
        AudioClip clip = Choose(item);
        if (clip == null || source == null) return;

        float effects = (GlobalSettings.Instance != null ? GlobalSettings.Instance.Data.effects : 1f) * GlobalSettings.PickupVolume;
        source.pitch = Random.Range(.97f, 1.04f);
        source.PlayOneShot(clip, Mathf.Clamp01(.5f * effects));
    }

    static AudioClip Choose(ItemSO item)
    {
        string name = (item.itemName ?? string.Empty).ToLowerInvariant();
        if (item.isGold) return coins;
        if (item.isEXP) return gem;
        if (name.Contains("mushroom") || name.Contains("蘑菇")) return misc1;
        if (name.Contains("pumpkin") || name.Contains("南瓜")) return misc2;
        if (name.Contains("meat") || name.Contains("肉")) return misc3;
        if (name.Contains("bone") || name.Contains("骨")) return stone;
        if (name.Contains("wood") || name.Contains("木") || name.Contains("timber")) return wood;

        // Stable per-item pick: the same item always sounds the same, different items differ.
        var bank = new[] { misc1, misc2, misc3, stone, wood, gem };
        int hash = 0;
        foreach (char c in (item.itemName ?? string.Empty)) hash = (hash * 31 + c) & 0x7fffffff;
        return bank[hash % bank.Length];
    }
}
