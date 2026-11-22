using UnityEngine;

// Serialized references keep the existing music assets in builds without copying them.
public sealed class NightAudioLibrary : ScriptableObject
{
    public AudioClip night;
    public AudioClip ruins;
}
