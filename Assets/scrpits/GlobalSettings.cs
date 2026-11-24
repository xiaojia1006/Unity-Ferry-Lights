using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>One device-wide settings owner, created independently of scene contents.</summary>
[DefaultExecutionOrder(-10000)]
public sealed class GlobalSettings : MonoBehaviour
{
    [Serializable] public sealed class Preferences
    {
        public float master=.8f, music=.35f, effects=.8f, footstep=.75f, pickup=.85f;
        public bool mute, ambient=true, loginMusic=true, fullscreen=true, vsync=true, fpsCounter;
        public int frameRate=60, movement;
        public int dayNightMode; // 0 automatic (legacy default), 1 day, 2 night; RPG2 only
    }
    const string StorageKey="Rpg.GlobalSettings.v1";
    public static GlobalSettings Instance { get; private set; }
    public static bool BlocksInput { get { return Instance!=null && (Instance.IsOpen || Instance.consumedFrame==Time.frameCount); } }
    public static float EnvironmentVolume { get { return Instance==null?1:Instance.Data.ambient?Instance.Data.effects:0; } }
    public static float FootstepVolume { get { return Instance==null?.75f:Instance.Data.footstep; } }
    public static float PickupVolume { get { return Instance==null?.85f:Instance.Data.pickup; } }
    public Preferences Data { get; private set; }
    public bool IsOpen { get; private set; }
    // Weather ducks the score so rain and footsteps stay audible in front of it.
    public float WorldDuck=1f;
    int consumedFrame=-1, page;
    GameObject panel, content;
    TMP_FontAsset font;
    TMP_Text account, fps;
    AudioSource music, click;
    AudioClip song, tick, fallbackSong, daySong, loginSong;
    UnityEngine.Object worldMusicOwner;
    AudioClip worldMusic;
    float musicFade=1f;
    public AudioClip CurrentMusic => music==null?null:music.clip;
    float frameAverage;
    bool confirmingLogout;
    readonly GameObject[] tabs = new GameObject[6];
    readonly Color green=new Color32(43,64,51,255), cream=new Color32(246,235,201,255), ink=new Color32(65,63,47,255);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics(){Instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot()
    { if(Instance==null)new GameObject("Global Settings").AddComponent<GlobalSettings>(); }
    void Awake()
    {
        if(Instance!=null){Destroy(gameObject);return;} Instance=this; DontDestroyOnLoad(gameObject);
        Data=new Preferences();
        try{if(PlayerPrefs.HasKey(StorageKey))Data=JsonUtility.FromJson<Preferences>(PlayerPrefs.GetString(StorageKey))??new Preferences();}catch(Exception){Data=new Preferences();}
        // Saves written before the ambience and login-music fields existed deserialize them as false,
        // which silently muted rain and thunder. Missing fields are treated as their intended default.
        string saved=PlayerPrefs.GetString(StorageKey,string.Empty);
        if(saved.Length>0)
        {
            if(!saved.Contains("\"ambient\""))Data.ambient=true;
            if(!saved.Contains("\"loginMusic\""))Data.loginMusic=true;
            if(!saved.Contains("\"footstep\""))Data.footstep=.75f;
            if(!saved.Contains("\"pickup\""))Data.pickup=.85f;
            if(!saved.Contains("\"vsync\""))Data.vsync=true;
            if(!saved.Contains("\"fullscreen\""))Data.fullscreen=true;
        }
        Data.master=SafeVolume(Data.master,.8f);Data.music=SafeVolume(Data.music,.35f);Data.effects=SafeVolume(Data.effects,.8f);Data.footstep=SafeVolume(Data.footstep,.75f);Data.pickup=SafeVolume(Data.pickup,.85f);
        Data.frameRate=Data.frameRate==30||Data.frameRate==60||Data.frameRate==120?Data.frameRate:60;Data.movement=Mathf.Clamp(Data.movement,0,2);Data.dayNightMode=Mathf.Clamp(Data.dayNightMode,0,2);
        font=Resources.Load<TMP_FontAsset>("Settings/Chinese");
        CreateAudio();Build();Apply();SceneManager.sceneLoaded+=SceneLoaded;
    }
    static float SafeVolume(float n,float fallback){return float.IsNaN(n)||float.IsInfinity(n)?fallback:Mathf.Clamp01(n);}
    void SceneLoaded(Scene scene,LoadSceneMode mode)
    {
        EnsureEventSystem();
        song=scene.name=="Login"&&loginSong!=null?loginSong:(daySong!=null?daySong:fallbackSong);
        ApplyAudio();
    }
    void EnsureEventSystem()
    {if(EventSystem.current==null){var e=new GameObject("Settings EventSystem");e.AddComponent<EventSystem>();e.AddComponent<StandaloneInputModule>();}}
    void Update()
    {
        bool f10=Input.GetKeyDown(KeyCode.F10),esc=Input.GetKeyDown(KeyCode.Escape);
        // F10 always toggles; Esc toggles too but yields to an open backpack, map, shop or dialogue first.
        if(f10||esc&&(IsOpen||CanOpenSettings())){consumedFrame=Time.frameCount;SetOpen(!IsOpen);}
        if(IsOpen)RefreshAccount();

        AdvanceMusic(Time.unscaledDeltaTime);
        frameAverage=Mathf.Lerp(frameAverage,Time.unscaledDeltaTime,.05f);
        fps.gameObject.SetActive(Data.fpsCounter);fps.text=Mathf.RoundToInt(1/Mathf.Max(.001f,frameAverage))+" FPS";
    }
    // Esc belongs to whatever menu is already open (those hold a pause, or run a dialogue) before it belongs to settings.
    static bool CanOpenSettings(){return Time.timeScale>0f&&(DialogueManager.Instance==null||!DialogueManager.Instance.isDialogueActive);}
    void RefreshAccount(){if(account!=null)account.text="当前账号："+(string.IsNullOrEmpty(LoginSession.Account)?"访客":LoginSession.Account)+"    ·    全局生效";}
    public void SetOpen(bool open)
    {
        IsOpen=open;consumedFrame=Time.frameCount;panel.SetActive(open);
        MenuPause.SetPaused(this,open);if(open){RefreshAccount();EnsureEventSystem();ShowPage(page);}else{Save();if(EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null);}
    }
    public void Save(){PlayerPrefs.SetString(StorageKey,JsonUtility.ToJson(Data));PlayerPrefs.Save();}
    public void Apply()
    {
        AudioListener.volume=Data.mute?0:Data.master;ApplyAudio();
        QualitySettings.vSyncCount=Data.vsync?1:0;Application.targetFrameRate=Data.frameRate;
        if(!Application.isEditor)Screen.fullScreen=Data.fullscreen;
    }
    void Changed(){Apply();Save();}
    void ApplyAudio()
    {
        if(music==null)return;music.volume=Data.music*.3f*musicFade*WorldDuck;click.volume=Data.effects*.28f;
        bool shouldPlay=SceneManager.GetActiveScene().name!="Login"||Data.loginMusic;
        if(shouldPlay&&!music.isPlaying)music.Play();else if(!shouldPlay)music.Stop();
    }
    // Scene atmosphere borrows the existing music source; all volume/mute controls still apply.
    public void SetWorldMusic(UnityEngine.Object owner,AudioClip clip)
    {if(owner==null)return;worldMusicOwner=owner;worldMusic=clip;}
    public void ReleaseWorldMusic(UnityEngine.Object owner)
    {if(worldMusicOwner!=owner)return;worldMusicOwner=null;worldMusic=null;}
    public void AdvanceMusic(float deltaTime)
    {
        if(music==null)return;
        if(worldMusicOwner==null)worldMusic=null;
        AudioClip desired=worldMusic!=null?worldMusic:song;
        float dt=Mathf.Max(0,deltaTime);
        if(music.clip!=desired)
        {
            musicFade=Mathf.MoveTowards(musicFade,0,dt/1.1f);
            if(musicFade<=0){music.Stop();music.clip=desired;}
        }
        else musicFade=Mathf.MoveTowards(musicFade,1,dt/1.4f);
        ApplyAudio();
    }
    void CreateAudio()
    {
        music=gameObject.AddComponent<AudioSource>();music.playOnAwake=false;music.loop=true;music.spatialBlend=0;
        click=gameObject.AddComponent<AudioSource>();click.playOnAwake=false;click.spatialBlend=0;click.ignoreListenerPause=true;
        // Quiet original pentatonic music, available even when the project has no imported audio.
        const int rate=22050;float[] samples=new float[rate*16];int[] notes={0,4,7,9,7,4,2,4,0,4,9,12,9,7,4,2};
        for(int i=0;i<samples.Length;i++){
            float t=i/(float)rate,local=t%1;int note=notes[Mathf.Min(15,(int)t)];float frequency=220*Mathf.Pow(2,note/12f);
            float envelope=Mathf.Min(1,local*12)*Mathf.Exp(-local*3);
            samples[i]=(Mathf.Sin(t*frequency*Mathf.PI*2)+.2f*Mathf.Sin(t*frequency*Mathf.PI*4))*.19f*envelope;
        }
        song=AudioClip.Create("Reedbank evening",samples.Length,1,rate,false);song.SetData(samples,0);
        // Imported tracks win over the generated placeholder; the clip only stays in memory as a fallback.
        fallbackSong=song;
        var dayTrack=Resources.Load<AudioClip>("Audio/BGM/day_clear");
        daySong=dayTrack!=null?dayTrack:fallbackSong;
        var loginTrack=Resources.Load<AudioClip>("Audio/BGM/login");
        loginSong=loginTrack!=null?loginTrack:daySong;
        song=daySong;music.clip=song;
        var taps=new float[2205];for(int i=0;i<taps.Length;i++)taps[i]=Mathf.Sin(i*2*Mathf.PI*660/rate)*Mathf.Exp(-i/300f)*.2f;
        tick=AudioClip.Create("Menu tap",taps.Length,1,rate,false);tick.SetData(taps,0);
    }
    public static Vector2 Movement()
    {
        int mode=Instance==null?0:Instance.Data.movement;
        if(mode==0)return new Vector2(Input.GetAxis("Horizontal"),Input.GetAxis("Vertical"));
        KeyCode left=mode==1?KeyCode.A:KeyCode.LeftArrow,right=mode==1?KeyCode.D:KeyCode.RightArrow,up=mode==1?KeyCode.W:KeyCode.UpArrow,down=mode==1?KeyCode.S:KeyCode.DownArrow;
        return new Vector2((Input.GetKey(right)?1:0)-(Input.GetKey(left)?1:0),(Input.GetKey(up)?1:0)-(Input.GetKey(down)?1:0));
    }
    void Build()
    {
        var canvas=gameObject.AddComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32000;
        var scaler=gameObject.AddComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
        gameObject.AddComponent<GraphicRaycaster>();
        // No persistent on-screen button: the panel is opened with Esc (F10 also works) so the HUD stays clean.
        panel=Box(transform,"Settings overlay",new Vector2(640,360),new Vector2(1280,720),new Color(0,0,0,.65f));
        var shade=(RectTransform)panel.transform;shade.anchorMin=Vector2.zero;shade.anchorMax=Vector2.one;shade.offsetMin=shade.offsetMax=Vector2.zero;
        var outer=Box(panel.transform,"Forest frame",Vector2.zero,new Vector2(1000,600),green);
        var paper=Box(outer.transform,"Parchment",Vector2.zero,new Vector2(980,580),cream);
        // Background only: the flat cream plate becomes the game's own pixel parchment (Resources/Settings/Parchment).
        var paperArt=Resources.Load<Sprite>("Settings/Parchment");
        if(paperArt!=null){var paperImage=paper.GetComponent<Image>();paperImage.sprite=paperArt;paperImage.type=Image.Type.Sliced;paperImage.pixelsPerUnitMultiplier=1f;paperImage.color=Color.white;}
        Text(paper.transform,"设  置",new Vector2(0,242),new Vector2(400,54),34);
        account=Text(paper.transform,"",new Vector2(0,195),new Vector2(900,34),18);
        string[] tabNames={"音频","画面","操作","游戏","账号","关于"};
        for(int i=0;i<tabNames.Length;i++){int p=i;tabs[i]=Button(paper.transform,tabNames[i],new Vector2(-380,125-i*56),new Vector2(150,44),()=>{confirmingLogout=false;ShowPage(p);});}
        Box(paper.transform,"Divider",new Vector2(-280,-10),new Vector2(2,340),new Color32(195,185,151,255));
        content=Box(paper.transform,"Page",new Vector2(100,-10),new Vector2(710,350),new Color(0,0,0,0));
        Button(paper.transform,"恢复默认",new Vector2(-350,-244),new Vector2(160,40),()=>{Data=new Preferences();Changed();ShowPage(page);});
        Button(paper.transform,"切换账号",new Vector2(140,-244),new Vector2(160,40),()=>{confirmingLogout=true;ShowPage(4);});
        Button(paper.transform,"关闭  Esc",new Vector2(340,-244),new Vector2(160,40),()=>SetOpen(false));
        Text(paper.transform,"设置自动保存在本机 · 切换场景持续生效",new Vector2(70,-196),new Vector2(720,28),15);
        fps=Text(transform,"",new Vector2(75,685),new Vector2(130,32),18);
        var fr=(RectTransform)fps.transform;fr.anchorMin=fr.anchorMax=new Vector2(0,1);fr.anchoredPosition=new Vector2(75,-24);
        panel.SetActive(false);
    }
    void ShowPage(int index)
    {
        page=index;
        for(int i=0;i<tabs.Length;i++){tabs[i].GetComponent<Image>().color=i==page?new Color32(157,65,47,255):new Color32(225,213,173,255);tabs[i].GetComponentInChildren<TMP_Text>().color=i==page?cream:ink;}
        foreach(Transform child in content.transform){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        if(index==0){
            SliderRow("主音量",150,Data.master,v=>Data.master=v);
            SliderRow("音乐",104,Data.music,v=>Data.music=v);
            SliderRow("音效",58,Data.effects,v=>Data.effects=v);
            SliderRow("脚步声",12,Data.footstep,v=>Data.footstep=v);
            SliderRow("拾取音",-34,Data.pickup,v=>Data.pickup=v);
            ToggleRow("静音",-80,Data.mute,v=>Data.mute=v);
            ToggleRow("环境声（雨声、雷声）",-126,Data.ambient,v=>Data.ambient=v);
            ToggleRow("登录界面也播放音乐",-172,Data.loginMusic,v=>Data.loginMusic=v);
        }else if(index==1){
            ToggleRow("全屏显示",140,Data.fullscreen,v=>Data.fullscreen=v);
            ToggleRow("垂直同步",82,Data.vsync,v=>Data.vsync=v);
            Choice("帧率上限",24,Data.frameRate+" FPS",()=>{Data.frameRate=Data.frameRate==30?60:Data.frameRate==60?120:30;Changed();ShowPage(page);});
            string[] timeModes={"自动昼夜","常亮白天","夜晚"};
            Choice("苇岸昼夜",-34,timeModes[Data.dayNightMode],()=>{Data.dayNightMode=(Data.dayNightMode+1)%3;Changed();ShowPage(page);});
            Text(content.transform,"昼夜只影响 RPG2，F9 可预览四个时段。\n垂直同步跟随显示器；全屏在独立游戏生效。",new Vector2(0,-112),new Vector2(650,66),17);
        }else if(index==2){
            string[] modes={"WASD + 方向键","仅 WASD","仅方向键"};Choice("移动方式",130,modes[Data.movement],()=>{Data.movement=(Data.movement+1)%3;Changed();ShowPage(page);});
            Text(content.transform,"E 互动     R 委托（RPG2）     Tab 地图\nB 背包     1 属性\nEsc 开关设置（F10 同效）\n\n设置打开时暂停游戏；关闭后恢复。",new Vector2(0,-30),new Vector2(650,220),22);
        }else if(index==3){
            ToggleRow("显示实时帧率",125,Data.fpsCounter,v=>Data.fpsCounter=v);
            Text(content.transform,"游戏时间在设置打开时暂停。\n音量及画面设置立即应用并自动保存。\n\n恢复默认只重置设置，不会删除账号。",new Vector2(0,-30),new Vector2(650,210),22);
        }else if(index==4){
            Text(content.transform,"当前账号："+(string.IsNullOrEmpty(LoginSession.Account)?"访客":LoginSession.Account)+"\n\n账号密码由本机账号服务验证。\n设置保存在当前设备，不包含游戏进度。",new Vector2(0,65),new Vector2(650,160),22);
            if(confirmingLogout){Text(content.transform,"返回登录将结束本次游玩，未保存进度会丢失。",new Vector2(0,-55),new Vector2(670,50),19);Button(content.transform,"确认返回登录",new Vector2(-120,-120),new Vector2(220,44),Logout);Button(content.transform,"取消",new Vector2(150,-120),new Vector2(150,44),()=>{confirmingLogout=false;ShowPage(4);});}
            else Button(content.transform,"切换账号",new Vector2(0,-110),new Vector2(220,44),()=>{confirmingLogout=true;ShowPage(4);});
        }else Text(content.transform,"芦岸冒险\n\n森林、渡口与旅人的故事\n\n全局设置 · 本机保存\n音乐：五声音阶 / 夜间场景音乐",Vector2.zero,new Vector2(650,300),25);
    }
    void Logout()
    {
        SetOpen(false);LoginSession.Clear();
        // Dispose persistent gameplay roots so another account gets a fresh session.
        foreach(var root in gameObject.scene.GetRootGameObjects())if(root!=gameObject)Destroy(root);
        Time.timeScale=1;SceneManager.LoadScene("Login");
    }
    void SliderRow(string title,float y,float value,Action<float> write)
    {
        Text(content.transform,title,new Vector2(-245,y),new Vector2(180,36),22);
        var readout=Text(content.transform,Mathf.RoundToInt(value*100)+"%",new Vector2(305,y),new Vector2(85,36),20);
        var track=Box(content.transform,title+" slider",new Vector2(60,y),new Vector2(370,32),new Color(0,0,0,0));
        Box(track.transform,"Track",Vector2.zero,new Vector2(370,8),new Color32(191,180,146,255));
        var fillArea=Box(track.transform,"Fill area",Vector2.zero,new Vector2(350,8),Color.clear);
        var fill=Box(fillArea.transform,"Fill",Vector2.zero,new Vector2(350,8),green);
        var handleArea=Box(track.transform,"Handle area",Vector2.zero,new Vector2(350,32),Color.clear);
        var handle=Box(handleArea.transform,"Handle",Vector2.zero,new Vector2(20,28),new Color32(160,69,48,255));
        ((RectTransform)fill.transform).sizeDelta=Vector2.zero;
        ((RectTransform)handle.transform).sizeDelta=new Vector2(20,0);
        var slider=track.AddComponent<Slider>();slider.fillRect=(RectTransform)fill.transform;slider.handleRect=(RectTransform)handle.transform;slider.targetGraphic=handle.GetComponent<Image>();slider.value=value;slider.navigation=new Navigation{mode=Navigation.Mode.None};
        slider.onValueChanged.AddListener(v=>{write(v);readout.text=Mathf.RoundToInt(v*100)+"%";Changed();});
    }
    void ToggleRow(string label,float y,bool value,Action<bool> write)
    {Button(content.transform,(value?"[开]  ":"[关]  ")+label,new Vector2(0,y),new Vector2(650,42),()=>{write(!value);Changed();ShowPage(page);});}
    void Choice(string label,float y,string value,Action next)
    {Text(content.transform,label,new Vector2(-215,y),new Vector2(240,40),22);Button(content.transform,"〈  "+value+"  〉",new Vector2(135,y),new Vector2(370,44),next);}
    GameObject Box(Transform parent,string name,Vector2 pos,Vector2 size,Color color)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.anchoredPosition=pos;r.sizeDelta=size;go.GetComponent<Image>().color=color;return go;}
    TMP_Text Text(Transform parent,string message,Vector2 pos,Vector2 size,int fontSize)
    {var go=new GameObject("Label",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.anchoredPosition=pos;r.sizeDelta=size;var t=go.GetComponent<TextMeshProUGUI>();t.font=font;t.richText=false;t.text=message;t.fontSize=fontSize;t.color=ink;t.alignment=TextAlignmentOptions.Center;t.raycastTarget=false;return t;}
    GameObject Button(Transform parent,string label,Vector2 pos,Vector2 size,Action action)
    {var go=Box(parent,label,pos,size,new Color32(225,213,173,255));var b=go.AddComponent<Button>();b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(()=>{if(tick!=null)click.PlayOneShot(tick);action();});Text(go.transform,label,Vector2.zero,size,21);return go;}
    void OnApplicationPause(bool paused){if(paused)Save();}
    void OnApplicationQuit(){Save();}
    void OnDestroy(){SceneManager.sceneLoaded-=SceneLoaded;MenuPause.SetPaused(this,false);if(Instance==this){Instance=null;AudioListener.volume=1;}
        // Only the generated placeholders belong to this object; imported clips are project assets.
        if(fallbackSong!=null)Destroy(fallbackSong);if(tick!=null)Destroy(tick);}
}


