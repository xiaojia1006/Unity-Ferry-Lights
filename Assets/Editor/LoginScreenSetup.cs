using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class LoginScreenSetup
{
    private const string ScenePath = "Assets/Scenes/Login.unity";
    private const string ArtPath = "Assets/Login/";
    private static TMP_FontAsset font;
    private static readonly Color Ink = new Color32(66, 53, 44, 255);
    static LoginScreenSetup()
    {
        EditorApplication.delayCall += FirstBuild;
        EditorApplication.playModeStateChanged += state => {
            if(state==PlayModeStateChange.ExitingEditMode && SceneManager.GetActiveScene().path==ScenePath) StartService();
        };
    }

    [MenuItem("Tools/RPG/Login/Start Local Account Service")]
    public static void StartService()
    {
        var start=new System.Diagnostics.ProcessStartInfo("powershell.exe", "-NoProfile -File \""+Path.GetFullPath("Server/Start-AuthServer.ps1")+"\"");
        start.UseShellExecute=false; start.CreateNoWindow=true;
        start.WindowStyle=System.Diagnostics.ProcessWindowStyle.Hidden;
        System.Diagnostics.Process.Start(start);
    }

    private static void FirstBuild()
    {
        if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(ScenePath)) Build();
        if (!EditorApplication.isPlayingOrWillChangePlaymode) Upgrade();
    }

    [MenuItem("Tools/RPG/Login/Connect Local Service And Tree Animation")]
    public static void Upgrade()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || !File.Exists(ScenePath)) return;
        var previous=SceneManager.GetActiveScene();
        var scene=SceneManager.GetSceneByPath(ScenePath);
        bool opened=!scene.IsValid() || !scene.isLoaded;
        if(opened) scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
        try
        {
            var screen=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<LoginScreen>(true)).First();
            if(screen.heading==null || screen.GetComponentsInChildren<LoginTreeAnimation>().Length!=15)
            {
                var panel=screen.transform.Find("Account Parchment");
                screen.heading=panel.Find("冒险者登录").GetComponent<TMP_Text>();
                screen.welcomeLabel=panel.Find("欢迎回到营火旁").GetComponent<TMP_Text>();
                screen.submitLabel=screen.login.GetComponentInChildren<TMP_Text>();
                screen.registerLabel=panel.Find("注册账号").GetComponentInChildren<TMP_Text>();
                screen.serviceUrl="http://127.0.0.1:18765";
                screen.firstGameScene="RPG1";
                var frames=Enumerable.Range(0,4).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/SceneExpansion/WillowPineFrame"+i+".asset")).ToArray();
                if(frames.Any(f=>f==null)) throw new Exception("Missing original gameplay tree frames");
                foreach(var image in screen.GetComponentsInChildren<Image>().Where(i=>i.name.StartsWith("Distant pine ")||i.name=="Foreground pine"))
                {
                    var animation=image.GetComponent<LoginTreeAnimation>();
                    if(animation==null) animation=image.gameObject.AddComponent<LoginTreeAnimation>();
                    animation.frames=frames; animation.framesPerSecond=5;
                    image.sprite=frames[0];
                }
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            var scenes=EditorBuildSettings.scenes.Where(s=>s.path!=ScenePath).ToList();
            scenes.Insert(0,new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes=scenes.ToArray();
        }
        finally
        {
            if(previous.IsValid()) SceneManager.SetActiveScene(previous);
            if(opened) EditorSceneManager.CloseScene(scene,true);
        }
    }

    [MenuItem("Tools/RPG/Login/Open Login Scene")]
    public static void Open()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!File.Exists(ScenePath)) Build();
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
    }

    [MenuItem("Tools/RPG/Login/Check And Capture In Play Mode %#F9")]
    public static void Check()
    {
        var screen = UnityEngine.Object.FindObjectOfType<LoginScreen>();
        if (!EditorApplication.isPlaying || screen == null) { Debug.LogWarning("Open Login and enter Play mode first."); return; }
        string oldAccount=screen.account.text, oldPassword=screen.password.text, oldMessage=screen.message.text;
        var results=new System.Collections.Generic.List<string>();
        try
        {
            screen.account.text=" "; screen.password.text=""; screen.Submit();
            if(screen.message.text!="请先填写你的账号") throw new Exception("Blank account validation failed");
            results.Add("PASS blank account validation");
            screen.account.text="ui_preview"; screen.Submit();
            if(screen.message.text!="请输入密码") throw new Exception("Blank password validation failed");
            results.Add("PASS blank password validation");
            var originalType=screen.password.contentType;
            screen.TogglePassword();
            if(screen.password.contentType==originalType) throw new Exception("Visibility toggle failed");
            screen.TogglePassword();
            if(screen.password.contentType!=originalType) throw new Exception("Password masking restore failed");
            results.Add("PASS password show/hide and restore");
            var trees=screen.GetComponentsInChildren<LoginTreeAnimation>();
            if(trees.Length!=15 || trees.Any(t=>t.frames.Length!=4 || t.framesPerSecond!=5 || t.frames.Any(f=>f==null))) throw new Exception("Gameplay tree animation is not connected");
            results.Add("PASS 15 trees use original four-frame gameplay animation at 5 FPS");
            if(screen.firstGameScene!="RPG1" || !Application.CanStreamedLevelBeLoaded("RPG1")) throw new Exception("First gameplay scene is not loadable");
            results.Add("PASS RPG1 target and build configuration");
            foreach(var text in screen.GetComponentsInChildren<TMP_Text>())
                if(!string.IsNullOrEmpty(text.text) && !text.font.HasCharacters(text.text))
                    throw new Exception("Missing font coverage: "+text.gameObject.name);
            results.Add("PASS Chinese label font coverage");
            if(screen.GetComponentInChildren<CanvasScaler>().referenceResolution != new Vector2(1920,1080)) throw new Exception("Unexpected canvas scale");
            results.Add("PASS responsive canvas reference resolution");
            results.Add("Scene: "+SceneManager.GetActiveScene().path);
            File.WriteAllLines("Logs/LoginChecks.txt",results);
            Debug.Log("Login checks passed: "+results.Count);
        }
        catch(Exception error) { results.Add("FAIL "+error.Message); File.WriteAllLines("Logs/LoginChecks.txt",results); Debug.LogException(error); }
        finally
        {
            screen.account.text=oldAccount; screen.password.text=oldPassword; screen.message.text=oldMessage;
            EventSystem.current.SetSelectedGameObject(null);
            ScreenCapture.CaptureScreenshot("Logs/LoginPreview.png");
        }
    }

    [MenuItem("Tools/RPG/Login/Create Missing Login Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || File.Exists(ScenePath)) return;
        var previous = SceneManager.GetActiveScene();
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        try
        {
            SceneManager.SetActiveScene(scene);
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ArtPath + "LoginChinese.asset");
            if (font == null)
            {
                var source = AssetDatabase.LoadAssetAtPath<Font>(ArtPath + "LoginChinese.ttf");
                font = TMP_FontAsset.CreateFontAsset(source, 48, 5, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 2048, 2048);
                font.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                font.isMultiAtlasTexturesEnabled = true;
                AssetDatabase.CreateAsset(font, ArtPath + "LoginChinese.asset");
                foreach (var texture in font.atlasTextures) AssetDatabase.AddObjectToAsset(texture, font);
                AssetDatabase.AddObjectToAsset(font.material, font);
                font.TryAddCharacters("柳湾物语冒险者登录欢迎回到营火旁账号密码请输入你的显示隐藏进入世界注册找忘记旅途从这里开始界面已就绪服务暂未接功能将在后开放请先填写新第一次来创建一个寻下一段旅程按切换提交版本正在准备 ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789*._-@ /…，。：·");
                EditorUtility.SetDirty(font);
            }
            var cam = new GameObject("Login Camera", typeof(Camera)).GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color32(32,53,48,255);
            cam.orthographic = true; cam.transform.position = new Vector3(0,0,-10);
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            var canvas = new GameObject("Login Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920,1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            var controller = canvas.AddComponent<LoginScreen>();
            Landscape(canvas.transform, controller);
            var shade = Art("Forest shade", canvas.transform, null, Vector2.zero, new Vector2(5000,5000));
            shade.color = new Color(0.035f,0.09f,0.08f,.23f);
            var title = Rect("Title", canvas.transform,new Vector2(-430,220),new Vector2(780,220));
            var brand = Text("柳湾物语",title,Vector2.zero,new Vector2(760,125),92,new Color32(247,232,183,255));
            var shadow = brand.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color32(26,49,44,255); shadow.effectDistance = new Vector2(5,-7);
            Text("W I L L O W R E A C H", title,new Vector2(0,-80),new Vector2(760,50),23,new Color32(213,213,162,255));
            Text("旅途，从这里开始",canvas.transform,new Vector2(-430,70),new Vector2(700,60),30,new Color32(242,226,184,255));
            Text("在营火旁歇一歇，开始下一段旅程",canvas.transform,new Vector2(-430,-410),new Vector2(800,60),23,new Color32(231,224,194,255));

            var panel = Rect("Account Parchment",canvas.transform,new Vector2(480,0),new Vector2(630,840));
            var ps = Art("Parchment shadow",panel,SpriteAt("Assets/SceneExpansion/JournalParchment.png"),new Vector2(10,-14),new Vector2(630,840)); ps.color = new Color(0,0,0,.25f);
            Art("Parchment",panel,SpriteAt("Assets/SceneExpansion/JournalParchment.png"),Vector2.zero,new Vector2(630,840));
            Art("Title ribbon",panel,SpriteAt("Assets/Sprites/UI/Ribbons/Ribbon_Yellow_3Slides.png"),new Vector2(0,305),new Vector2(520,106));
            Text("冒险者登录",panel,new Vector2(0,309),new Vector2(450,66),43,Ink);
            Text("欢迎回到营火旁",panel,new Vector2(0,214),new Vector2(460,45),24,new Color32(112,96,67,255));
            Text("账号",panel,new Vector2(-188,142),new Vector2(90,40),26,Ink);
            controller.account = Field("Account", "请输入你的账号",panel,new Vector2(0,78),false);
            Text("密码",panel,new Vector2(-188,0),new Vector2(90,40),26,Ink);
            controller.password = Field("Password", "请输入密码",panel,new Vector2(0,-64),true);
            var eye = Button("显示",panel,new Vector2(182,-64),new Vector2(80,48),false);
            controller.visibilityLabel = eye.GetComponentInChildren<TMP_Text>();
            UnityEventTools.AddPersistentListener(eye.onClick,controller.TogglePassword);
            var recovery = Button("忘记密码",panel,new Vector2(164,-133),new Vector2(145,42),false);
            UnityEventTools.AddPersistentListener(recovery.onClick,controller.Recovery);
            controller.message = Text("",panel,new Vector2(0,-189),new Vector2(480,62),22,new Color32(145,60,39,255));
            controller.login = Button("进入世界",panel,new Vector2(0,-260),new Vector2(470,90),true);
            UnityEventTools.AddPersistentListener(controller.login.onClick,controller.Submit);
            Text("第一次来？",panel,new Vector2(-93,-345),new Vector2(180,42),22,new Color32(112,96,67,255));
            var register = Button("注册账号",panel,new Vector2(82,-345),new Vector2(150,42),false);
            UnityEventTools.AddPersistentListener(register.onClick,controller.Registration);
            Text("TAB  切换   /   ENTER  提交",canvas.transform,new Vector2(480,-468),new Vector2(650,40),19,new Color32(229,222,188,255));
            Text("LOGIN UI  ·  0.1",canvas.transform,new Vector2(-760,-500),new Vector2(310,32),18,new Color32(191,205,177,255));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            File.WriteAllText("Logs/LoginSetup.txt", "Created editable Login scene. Original build order preserved.\n");
        }
        finally
        {
            if (previous.IsValid()) SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(scene,true);
        }
    }

    private static void Landscape(Transform parent, LoginScreen controller)
    {
        // All scenery is made from the project's authored pixel sprites.
        Art("Sky",parent,null,Vector2.zero,new Vector2(5000,5000)).color = new Color32(49,83,71,255);
        Art("Distant meadow",parent,null,new Vector2(0,-120),new Vector2(5000,820)).color = new Color32(85,116,75,255);
        Art("Village ground",parent,null,new Vector2(0,-330),new Vector2(5000,480)).color = new Color32(104,126,75,255);
        var tree = Sprites("Assets/Sprites/Tree/Tree.png").First();
        for(int i=0;i<14;i++)
        {
            var t = Art("Distant pine " + i,parent,tree,new Vector2(-1100+i*175,160+(i%3)*29),new Vector2(250,250));
            t.color = new Color32(91,128,106,255);
        }
        Art("Village path",parent,null,new Vector2(-370,-360),new Vector2(1040,150)).color = new Color32(153,139,91,255);
        Art("Guest house",parent,SpriteAt("Assets/Sprites/Building/House/House_Blue.png"),new Vector2(-610,-115),new Vector2(350,420));
        Art("Merchant house",parent,SpriteAt("Assets/Sprites/Building/House/House_Yellow.png"),new Vector2(-190,-135),new Vector2(240,300));
        var foreground = Art("Foreground pine",parent,tree,new Vector2(-1010,-275),new Vector2(660,660)); foreground.color = new Color32(116,148,119,255);
        var warrior = Sprites("Assets/Sprites/Warrior/Warrior_Blue.png").First();
        Art("Resting adventurer",parent,warrior,new Vector2(-300,-270),new Vector2(190,190));
        // Fire is a single texture in this project; split its first animation row.
        var fireTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Effects/Fire/Fire.png");
        int frameSize = fireTexture.height;
        int count = fireTexture.width / frameSize;
        controller.fireFrames = new Sprite[count];
        for(int i=0;i<count;i++) controller.fireFrames[i] = Crop("Fire"+i,fireTexture,new UnityEngine.Rect(i*frameSize,0,frameSize,frameSize));
        controller.campfire = Art("Campfire",parent,controller.fireFrames[0],new Vector2(-435,-275),new Vector2(130,130));
        for(int i=0;i<18;i++)
        {
            var mote = Art("Firefly " + i,parent,null,new Vector2(-950+(i*113)%1040,-350+(i*79)%590),new Vector2(4+(i%2)*2,4+(i%2)*2));
            mote.color = new Color(1,.88f,.48f,.35f+(i%3)*.16f);
        }
    }

    private static Sprite Crop(string name, Texture2D source, UnityEngine.Rect bounds)
    {
        var sprite = Sprite.Create(source,bounds,new Vector2(.5f,.5f),100);
        sprite.name = name;
        AssetDatabase.CreateAsset(sprite,ArtPath+name+".asset");
        return sprite;
    }
    private static Sprite SpriteAt(string path) { return AssetDatabase.LoadAssetAtPath<Sprite>(path); }
    private static Sprite[] Sprites(string path) { return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s=>s.name,StringComparer.Ordinal).ToArray(); }
    private static RectTransform Rect(string name,Transform parent,Vector2 position,Vector2 size)
    {
        var rect = new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent,false); rect.anchoredPosition=position; rect.sizeDelta=size; return rect;
    }
    private static Image Art(string name,Transform parent,Sprite sprite,Vector2 position,Vector2 size)
    {
        var image = Rect(name,parent,position,size).gameObject.AddComponent<Image>();
        image.sprite=sprite; image.raycastTarget=false;
        image.type=sprite != null && sprite.border.sqrMagnitude>0 ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier=2; image.preserveAspect=sprite != null && image.type==Image.Type.Simple; return image;
    }
    private static TMP_Text Text(string value,Transform parent,Vector2 position,Vector2 size,float fontSize,Color color)
    {
        var text=Rect(string.IsNullOrEmpty(value)?"Status":value,parent,position,size).gameObject.AddComponent<TextMeshProUGUI>();
        text.font=font; text.text=value; text.fontSize=fontSize; text.color=color;
        text.alignment=TextAlignmentOptions.Center; text.raycastTarget=false; text.richText=false; return text;
    }
    private static Button Button(string label,Transform parent,Vector2 position,Vector2 size,bool primary)
    {
        var image=Art(label,parent,primary?SpriteAt("Assets/Sprites/UI/Buttons/Button_Blue_9Slides.png"):null,position,size);
        image.raycastTarget=true;
        if(!primary) image.color=Color.clear;
        var button=image.gameObject.AddComponent<Button>(); button.targetGraphic=image;
        var colors=button.colors; colors.highlightedColor=new Color(1.12f,1.12f,1.02f,1); colors.pressedColor=new Color(.8f,.8f,.8f,1); button.colors=colors;
        button.navigation=new Navigation {mode=Navigation.Mode.None};
        Text(label,image.transform,Vector2.zero,size,primary?32:22,primary?new Color32(253,244,213,255):new Color32(48,93,93,255));
        return button;
    }
    private static TMP_InputField Field(string name,string placeholder,Transform parent,Vector2 position,bool secret)
    {
        var border=Art(name,parent,SpriteAt("Assets/Sprites/UI/Buttons/Button_Blue_9Slides.png"),position,new Vector2(476,74)); border.raycastTarget=true;
        var inset=Art("Input parchment",border.transform,null,Vector2.zero,new Vector2(452,52)); inset.color=new Color32(247,234,192,255);
        var viewport=Rect("Text Area",border.transform,new Vector2(secret?-40:0,0),new Vector2(secret?340:420,48));
        viewport.gameObject.AddComponent<RectMask2D>();
        var value=Text("",viewport,Vector2.zero,viewport.sizeDelta,26,Ink); value.alignment=TextAlignmentOptions.MidlineLeft; value.enableWordWrapping=false;
        var hint=Text(placeholder,viewport,Vector2.zero,viewport.sizeDelta,24,new Color32(143,130,96,255)); hint.alignment=TextAlignmentOptions.MidlineLeft;
        var input=border.gameObject.AddComponent<TMP_InputField>(); input.targetGraphic=border; input.textViewport=viewport;
        input.textComponent=(TextMeshProUGUI)value; input.placeholder=hint; input.characterLimit=secret?128:64;
        input.contentType=secret?TMP_InputField.ContentType.Password:TMP_InputField.ContentType.Standard;
        input.lineType=TMP_InputField.LineType.SingleLine; input.customCaretColor=true; input.caretColor=Ink;
        input.navigation=new Navigation {mode=Navigation.Mode.None};
        return input;
    }
}
