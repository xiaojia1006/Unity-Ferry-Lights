using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>RPG2-only visual day/night. Scene-owned, no changes to quests, collision or actor statistics.</summary>
[DefaultExecutionOrder(250)]
public sealed class DayNight2D : MonoBehaviour
{
    public enum Phase { Day, Dusk, Night, Dawn }
    public static DayNight2D Instance { get; private set; }
    public Phase Current { get; private set; }
    public float Darkness { get; private set; }
    public float PhaseSeconds { get; private set; }
    public const float MaximumDarkness=.5f;
    public bool enableAtmosphere=true;
    public int RegisteredLights => lights.Count;
    public int ActiveLightCount { get; private set; }
    public float BeaconPulse => beaconPulse;
    public Color Tint { get; private set; }
    readonly List<Lamp> lights=new List<Lamp>();
    readonly List<SpriteRenderer> warnings=new List<SpriteRenderer>();
    readonly Vector4[] lightData=new Vector4[8],lightColors=new Vector4[8],warningData=new Vector4[4];
    sealed class Lamp { public RPG2Point point;public float radius,strength;public Color color;public bool lit; }
    NightCamera2D effect;
    WeatherCycle2D weather;
    NightAudioLibrary audioLibrary;
    PlayerMovement player;
    TMP_Text badge;
    float clock,beaconPulse,lookup;
    int observedMode=-1;
    bool ruinsMusic;
    static readonly Color NightTint=new Color32(10,26,46,255),WarmTint=new Color32(42,27,18,255);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics(){Instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Watch()
    {SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
    static void Loaded(Scene scene,LoadSceneMode mode)
    {
        if(scene.name!="RPG2")return;
        foreach(var root in scene.GetRootGameObjects())if(root.GetComponentInChildren<DayNight2D>(true)!=null)return;
        var go=new GameObject("Reedbank Day and Night");SceneManager.MoveGameObjectToScene(go,scene);go.AddComponent<DayNight2D>();
    }
    void Awake()
    {
        Instance=this;audioLibrary=Resources.Load<NightAudioLibrary>("Night/Audio");
        DiscoverLights();CreateBadge();ReadMode();BindCamera();RefreshVisuals(true);
    }
    void DiscoverLights()
    {
        lights.Clear();warnings.Clear();
        foreach(var root in gameObject.scene.GetRootGameObjects())
        {
            foreach(var p in root.GetComponentsInChildren<RPG2Point>(true))
            {
                var l=new Lamp{point=p,color=new Color32(255,185,107,255)};
                switch(p.kind)
                {
                    case RPG2Point.Kind.Camp:l.radius=3.6f;l.strength=.55f;break;
                    case RPG2Point.Kind.Beacon:l.radius=2.6f;l.strength=.5f;l.color=new Color32(255,215,154,255);break;
                    case RPG2Point.Kind.Inn:l.radius=2.3f;l.strength=.4f;break;
                    case RPG2Point.Kind.Treasure:l.radius=1.8f;l.strength=.35f;l.color=new Color32(201,230,176,255);break;
                    default:continue;
                }
                l.lit=IsLit(p);lights.Add(l);
            }
            foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>(true))if(sr.name=="Guard attack telegraph")warnings.Add(sr);
        }
    }
    static bool IsLit(RPG2Point p)=>p!=null&&p.gameObject.activeInHierarchy&&(p.kind!=RPG2Point.Kind.Beacon||p.glow!=null&&p.glow.activeSelf);
    void BindCamera()
    {
        var camera=Camera.main;if(camera==null)return;
        if(effect!=null&&effect.gameObject==camera.gameObject)return;
        if(effect!=null)Destroy(effect);
        effect=camera.GetComponent<NightCamera2D>();if(effect==null)effect=camera.gameObject.AddComponent<NightCamera2D>();effect.owner=this;
    }
    int Mode => GlobalSettings.Instance==null?0:GlobalSettings.Instance.Data.dayNightMode;
    void ReadMode()
    {
        observedMode=Mode;
        SetPhase(observedMode==2?Phase.Night:Phase.Day,true);
    }
    void Update()
    {
        if(Mode!=observedMode)ReadMode();
        if(!GlobalSettings.BlocksInput&&Time.timeScale>0&&Input.GetKeyDown(KeyCode.F9))CyclePreview();
        Advance(Time.deltaTime);
    }
    void LateUpdate(){BindCamera();}
    public static float Duration(Phase phase)=>phase==Phase.Day||phase==Phase.Night?180:60;
    public void Advance(float deltaTime)
    {
        if(Mode!=observedMode)ReadMode();
        float dt=Mathf.Max(0,deltaTime);clock+=dt;beaconPulse=Mathf.Max(0,beaconPulse-dt*.8f);
        if(Mode==0)
        {
            PhaseSeconds+=dt;
            while(PhaseSeconds>=Duration(Current)){PhaseSeconds-=Duration(Current);Current=(Phase)(((int)Current+1)%4);}
        }
        RefreshVisuals(false,dt);
        for(int i=0;i<lights.Count;i++)
        {
            var l=lights[i];bool now=IsLit(l.point);
            if(now&&!l.lit&&l.point.kind==RPG2Point.Kind.Beacon&&Darkness>.12f)beaconPulse=.12f;
            l.lit=now;
        }
        if(weather==null)weather=FindObjectOfType<WeatherCycle2D>();
        if(player==null)player=FindObjectOfType<PlayerMovement>();
        if(clock>=lookup){lookup=clock+.5f;SelectMusic();}
    }
    public void SetPhase(Phase phase,bool snap=true)
    {
        Current=phase;PhaseSeconds=phase==Phase.Day?0:Duration(phase)*(phase==Phase.Dusk?.75f:phase==Phase.Night?.8f:.5f);
        RefreshVisuals(snap);SelectMusic();
    }
    public void CyclePreview(){SetPhase((Phase)(((int)Current+1)%4),false);}
    void RefreshVisuals(bool snap,float delta=0)
    {
        float t=Mathf.SmoothStep(0,1,PhaseSeconds/Duration(Current));float target=0;Color tint=NightTint;
        switch(Current)
        {
            case Phase.Dusk:target=.28f*t;tint=WarmTint;break;
            case Phase.Night:target=Mathf.Lerp(.28f,.45f,t);tint=Color.Lerp(WarmTint,NightTint,Mathf.SmoothStep(0,1,PhaseSeconds/28));break;
            case Phase.Dawn:target=.45f*(1-t);tint=Color.Lerp(NightTint,WarmTint,Mathf.SmoothStep(0,1,PhaseSeconds/22));break;
        }
        if(!enableAtmosphere)target=0;
        Darkness=snap?target:Mathf.MoveTowards(Darkness,target,Mathf.Max(0,delta)*.23f);
        Tint=snap?tint:Color.Lerp(Tint,tint,1-Mathf.Exp(-delta*1.5f));
        if(badge!=null)badge.text="苇岸 · "+new[]{"白昼","黄昏","夜晚","黎明"}[(int)Current]+"   [F9]";
    }
    void SelectMusic()
    {
        var settings=GlobalSettings.Instance;if(settings==null)return;
        if(Current!=Phase.Night||audioLibrary==null){settings.ReleaseWorldMusic(this);ruinsMusic=false;return;}
        if(player!=null)
        {
            foreach(var l in lights)if(l.point!=null&&l.point.kind==RPG2Point.Kind.Treasure)
            {float distance=Vector2.Distance(player.transform.position,l.point.transform.position);ruinsMusic=distance<(ruinsMusic?10:8);break;}
        }
        settings.SetWorldMusic(this,ruinsMusic&&audioLibrary.ruins!=null?audioLibrary.ruins:audioLibrary.night);
    }
    public void ConfigureMaterial(Material material,Camera camera)
    {
        float snow=weather!=null?weather.SnowAmount:0,rain=weather!=null?weather.RainAmount:0;
        float darkness=Mathf.Min(MaximumDarkness,Darkness+(rain*.05f+snow*.03f)*Mathf.Clamp01(Darkness/.4f));
        material.SetColor("_Tint",Tint);material.SetFloat("_Darkness",darkness);material.SetFloat("_Pulse",beaconPulse);material.SetFloat("_Snow",snow);material.SetFloat("_Aspect",camera.aspect);
        int count=0;
        foreach(var l in lights)
        {
            if(count>=8)break;if(!IsLit(l.point))continue;
            var at=l.point.kind==RPG2Point.Kind.Beacon&&l.point.glow!=null?l.point.glow.transform.position:l.point.transform.position;
            var screen=camera.WorldToViewportPoint(at);if(screen.z<=0)continue;
            float radius=Mathf.Abs(camera.WorldToViewportPoint(at+Vector3.up*l.radius).y-screen.y);
            float flicker=.97f+.025f*Mathf.Sin(clock*7.3f+count*2)+.015f*Mathf.Sin(clock*11.1f+count);
            lightData[count]=new Vector4(screen.x,screen.y,radius,Mathf.Min(snow>.5f?.35f:l.strength,l.strength*flicker));
            lightColors[count]=Color.Lerp(l.color,new Color32(255,234,208,255),snow*.4f);count++;
        }
        ActiveLightCount=count;material.SetInt("_LightCount",count);material.SetVectorArray("_Lights",lightData);material.SetVectorArray("_LightColors",lightColors);
        int n=0;foreach(var sr in warnings)
        {
            if(n>=4)break;if(sr==null||!sr.enabled||!sr.gameObject.activeInHierarchy)continue;
            var center=camera.WorldToViewportPoint(sr.bounds.center);var extent=camera.WorldToViewportPoint(sr.bounds.center+sr.bounds.extents)-center;
            warningData[n++]=new Vector4(center.x,center.y,Mathf.Abs(extent.x)*1.05f,Mathf.Abs(extent.y)*1.05f);
        }
        material.SetInt("_WarningCount",n);material.SetVectorArray("_Warnings",warningData);
    }
    void CreateBadge()
    {
        var go=new GameObject("Time of day",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));go.transform.SetParent(transform,false);
        var canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=60;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
        var label=new GameObject("Reedbank time",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(go.transform,false);
        var rect=(RectTransform)label.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-30);rect.sizeDelta=new Vector2(260,38);
        badge=label.GetComponent<TextMeshProUGUI>();badge.font=Resources.Load<TMP_FontAsset>("Settings/Chinese");badge.fontSize=18;badge.alignment=TextAlignmentOptions.Center;badge.color=new Color32(255,237,190,255);badge.outlineWidth=.18f;badge.outlineColor=new Color32(30,42,42,230);badge.raycastTarget=false;
    }
    void OnDestroy()
    {
        if(GlobalSettings.Instance!=null)GlobalSettings.Instance.ReleaseWorldMusic(this);
        if(effect!=null&&effect.owner==this)Destroy(effect);
        if(Instance==this)Instance=null;
    }
}

