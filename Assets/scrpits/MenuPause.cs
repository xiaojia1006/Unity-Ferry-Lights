using System.Collections.Generic;
using UnityEngine;

// Each open menu holds a pause until it closes or is disabled.
public static class MenuPause
{
    private static readonly HashSet<Object> owners = new HashSet<Object>();
    private static float previousTimeScale = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        if (owners.Count > 0)
            Time.timeScale = previousTimeScale;
        owners.Clear();
        previousTimeScale = 1f;
    }

    public static void SetPaused(Object owner, bool paused)
    {
        if (paused)
        {
            if (owners.Contains(owner))
                return;
            if (owners.Count == 0)
                previousTimeScale = Time.timeScale;
            owners.Add(owner);
            Time.timeScale = 0f;
        }
        else if (owners.Remove(owner) && owners.Count == 0)
        {
            Time.timeScale = previousTimeScale;
        }
    }
}
