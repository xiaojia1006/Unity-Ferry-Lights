using UnityEngine;

// Built-in pipeline effect: runs on the world camera before ScreenSpaceOverlay HUDs.
[DisallowMultipleComponent, RequireComponent(typeof(Camera))]
public sealed class NightCamera2D : MonoBehaviour
{
    public DayNight2D owner;
    Material material;
    public bool Ready => material!=null && material.shader.isSupported;
    void OnEnable()
    {
        var shader=Resources.Load<Shader>("Night/NightAtmosphere");
        if(shader!=null)material=new Material(shader){hideFlags=HideFlags.HideAndDontSave};
    }
    void OnRenderImage(RenderTexture source,RenderTexture destination)
    {
        if(owner==null||!owner.isActiveAndEnabled||!Ready||owner.Darkness<=.0001f){Graphics.Blit(source,destination);return;}
        owner.ConfigureMaterial(material,GetComponent<Camera>());
        Graphics.Blit(source,destination,material);
    }
    void OnDisable(){if(material!=null)Destroy(material);material=null;}
}
