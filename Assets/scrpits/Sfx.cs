using UnityEngine;

// One shared 2D sound source for combat. Volume follows the settings panel's effect slider
// (master volume and mute already reach every source through AudioListener).
public static class Sfx
{
    private static AudioSource source;
    private static AudioClip[] blade,impact,die;

    private static AudioSource Source()
    {
        if(source!=null)return source;
        var go=new GameObject("Combat SFX");
        Object.DontDestroyOnLoad(go);
        source=go.AddComponent<AudioSource>();
        source.playOnAwake=false;source.loop=false;source.spatialBlend=0f;source.volume=1f;
        return source;
    }

    private static AudioClip[] Bank(string[] paths)
    {
        var clips=new AudioClip[paths.Length];
        for(int i=0;i<paths.Length;i++)clips[i]=Resources.Load<AudioClip>(paths[i]);
        return clips;
    }

    public static void Blade(float scale=1f)=>Emit(ref blade,scale,1f,"Audio/blade_01","Audio/blade_02","Audio/blade_03");
    public static void Bow(float scale=1f)=>Emit(ref blade,scale,1.2f,"Audio/blade_01","Audio/blade_02","Audio/blade_03");
    public static void Impact(float scale=1f)=>Emit(ref impact,scale*.85f,1f,"Audio/metal_01","Audio/metal_02");
    public static void EnemySwing(float scale=1f)=>Emit(ref blade,scale*.9f,.9f,"Audio/blade_01","Audio/blade_02","Audio/blade_03");
    public static void Die(float scale=1f)=>Emit(ref die,scale,1f,"Audio/creature_die_01");

    private static void Emit(ref AudioClip[] bank,float scale,float pitch,params string[] paths)
    {
        if(bank==null||bank.Length==0)bank=Bank(paths);
        if(bank.Length==0)return;
        var audio=Source();
        if(audio==null)return;
        float effects=GlobalSettings.Instance!=null?GlobalSettings.Instance.Data.effects:1f;
        float volume=Mathf.Clamp01(effects*Mathf.Clamp01(scale));
        if(volume<=0f)return;
        var clip=bank[Random.Range(0,bank.Length)];
        if(clip==null)return;
        audio.pitch=pitch*Random.Range(.94f,1.06f);
        audio.PlayOneShot(clip,volume);
    }
}
