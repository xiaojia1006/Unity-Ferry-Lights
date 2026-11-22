using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class WillowreachWorld : MonoBehaviour
{
    public WeatherCycle2D weather;
    public WaterSurface2D river;
    public Rect[] bridges;
    public GameObject shortcutBridge;
    public Rect shortcutBridgeArea;
    public WillowreachNode[] nodes;
    public Sprite panelSprite;
    public Sprite journalSprite, ribbonSprite;
    public Sprite mapSprite;
    public Transform player;
    public float nextRest;
    public int SurveyCount => nodes.Count(n => n != null && n.kind == WillowreachNode.Kind.Survey && n.completed);
    public WillowreachNode Nearest { get; private set; }
    private Text objective, prompt, toast;
    private GameObject promptPanel, toastPanel;
    private GameObject map;
    private RectTransform marker;
    private Font font;
    private float messageUntil;
    private PlayerMovement movement;
    private Vector3 spawn;
    private Canvas canvas;
    private WillowreachPastoral pastoral;
    private WillowreachAdventure adventure;
    private WillowreachKitchen kitchen;

    public static string WeatherName(WeatherCycle2D.Weather w) => w == WeatherCycle2D.Weather.Sunny ? "晴天" : w == WeatherCycle2D.Weather.Rain ? "雨天" : "雪天";
    private void Start()
    {
        movement = FindObjectOfType<PlayerMovement>();
        if (movement != null) { player = movement.transform; spawn = player.position; }
        font = Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei", "SimHei", "Arial"}, 18);
        BuildUI();
        pastoral=GetComponent<WillowreachPastoral>();
        adventure=GetComponent<WillowreachAdventure>();
        kitchen=GetComponent<WillowreachKitchen>();
        Notify("欢迎来到柳湾边境。E 采集/观测，K 商店/宝箱，Tab 地图，B 背包。");
    }

    private void Update()
    {
        if (GlobalSettings.BlocksInput) return;
        if (player == null || objective == null) return;
        if (Input.GetKeyDown(KeyCode.Tab) && !BackpackUI.BlocksOtherInput && (map.activeSelf || Time.timeScale > 0))
        { map.SetActive(!map.activeSelf); MenuPause.SetPaused(this,map.activeSelf); }
        if (map.activeSelf && Input.GetKeyDown(KeyCode.Escape)) { map.SetActive(false); MenuPause.SetPaused(this,false); }
        UpdateFieldMap();
        canvas.enabled = !BackpackUI.BlocksOtherInput && ShopKeeper.currentShopKeeper == null;
        Nearest = null;
        float nearest = float.MaxValue;
        foreach (var n in nodes)
        {
            if (n == null || (n.completed && !n.Reusable)) continue;
            float distance = Vector2.Distance(player.position, n.transform.position);
            if (distance < n.range && distance < nearest) { nearest = distance; Nearest = n; }
        }
        bool finished=nodes.Any(n=>n.kind==WillowreachNode.Kind.Shortcut&&n.completed);
        objective.text = "柳湾边境  ·  " + Region(player.position) + "\n" + WeatherName(weather.currentWeather) + "  |  观测 " + SurveyCount + "/3\n" + (finished ? "渡桥已修复！可继续搜寻林地与遗迹宝箱" : SurveyCount < 3 ? "循路寻找三处观测站，重启边境航标" : "前往西北遗迹，修复返乡渡桥") + "\nTab 地图    B 背包    E 互动";
        if(pastoral!=null)objective.text += "\n"+pastoral.Journal;
        if(adventure!=null)objective.text += "\n"+adventure.Journal;
        if(kitchen!=null&&adventure!=null)objective.text+="\n帮厨 "+kitchen.successes+"/3 · 护货封蜡 "+adventure.cargoSeals;
        if(kitchen!=null&&kitchen.cooking)objective.text="窗口帮厨 · "+kitchen.Hint+"\n成功才扣食材；离开窗口取消。";
        prompt.text = Nearest == null || BackpackUI.BlocksOtherInput || map.activeSelf ? "" : "[ E ] " + Nearest.title + "\n" + Nearest.hint + (Nearest.kind == WillowreachNode.Kind.Survey || Nearest.kind == WillowreachNode.Kind.Pasture ? "\nF1 晴天   F2 雨天   F3 雪天" : "");
        if(kitchen!=null&&prompt.text.Length>0&&Nearest!=null&&Nearest.kind==WillowreachNode.Kind.Shop)prompt.text+="\n"+kitchen.Hint;
        if (Time.unscaledTime > messageUntil) toast.text = "";
        promptPanel.SetActive(prompt.text.Length>0);
        toastPanel.SetActive(toast.text.Length>0 && !map.activeSelf);
        objective.transform.parent.gameObject.SetActive(!map.activeSelf);
        if (Time.timeScale <= 0 || BackpackUI.BlocksOtherInput || map.activeSelf || ShopKeeper.currentShopKeeper!=null) return;
        if (Nearest != null && (Nearest.kind == WillowreachNode.Kind.Survey || Nearest.kind == WillowreachNode.Kind.Pasture))
        {
            if (Input.GetKeyDown(KeyCode.F1)) weather.SetWeather(WeatherCycle2D.Weather.Sunny);
            if (Input.GetKeyDown(KeyCode.F2)) weather.SetWeather(WeatherCycle2D.Weather.Rain);
            if (Input.GetKeyDown(KeyCode.F3)) weather.SetWeather(WeatherCycle2D.Weather.Snow);
        }
        if (Input.GetKeyDown(KeyCode.E) && Nearest != null) Nearest.TryInteract(this);
        // Soft recovery is an explicit rescue, not an invisible edge teleport during normal movement.
        if (Input.GetKeyDown(KeyCode.Home))
        { player.position = spawn; var rb=player.GetComponent<Rigidbody2D>(); if(rb!=null) rb.velocity=Vector2.zero; Notify("已返回村口。探索进度与物品保留。"); }
    }

    private void LateUpdate()
    {
        if (movement == null || river == null) return;
        Vector2 p = player.position;
        bool onBridge = bridges.Any(r => r.Contains(p)) || (shortcutBridge != null && shortcutBridge.activeInHierarchy && shortcutBridgeArea.Contains(p));
        movement.SetWaterState(river, !onBridge && river.GetComponent<PolygonCollider2D>().OverlapPoint(p), river.swimSpeedMultiplier);
    }
    private void OnDisable() { MenuPause.SetPaused(this,false); if(movement!=null && river!=null) movement.SetWaterState(river,false,1); }
    public void Notify(string text) { if(toast!=null) toast.text=text; messageUntil=Time.unscaledTime+5; }
    public void OpenFieldMap() { if(map!=null) { map.SetActive(true); MenuPause.SetPaused(this,true); } }
    private string Region(Vector2 p) => p.y > 10 ? (p.x < 0 ? "风雪遗迹" : "旧矿场") : (p.x < -13 && p.y < 0 ? "柳湾村" : p.x > 10 && p.y < -7 ? "蘑菇林" : "柳湾河道");

    private RectTransform Box(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 position, Color color, Sprite sprite=null)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(Image)); go.transform.SetParent(parent,false);
        var r=(RectTransform)go.transform; r.anchorMin=r.anchorMax=anchor; r.anchoredPosition=position; r.sizeDelta=size;
        var image=go.GetComponent<Image>(); image.sprite=sprite; image.type=sprite!=null&&sprite.border.sqrMagnitude>0?Image.Type.Sliced:Image.Type.Simple; image.color=color; image.raycastTarget=false; return r;
    }
    private Text Label(Transform parent,string content,Vector2 size,Vector2 anchor,Vector2 position,int fontSize,TextAnchor align=TextAnchor.MiddleCenter)
    {
        var go=new GameObject("Label",typeof(RectTransform),typeof(Text)); go.transform.SetParent(parent,false);
        var r=(RectTransform)go.transform; r.anchorMin=r.anchorMax=anchor; r.anchoredPosition=position; r.sizeDelta=size;
        var t=go.GetComponent<Text>(); t.font=font;t.fontSize=fontSize;t.text=content;t.alignment=align;t.color=new Color(.22f,.18f,.15f);t.raycastTarget=false;return t;
    }
    private void BuildUI()
    {
        var go=new GameObject("Willowreach HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);
        canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=8;
        var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
        // Keep the journal below the original health/EXP and quickbar HUD.
        var panel=Box("Travel Journal",go.transform,new Vector2(480,285),new Vector2(1,0),new Vector2(-265,185),Color.white,journalSprite != null ? journalSprite : panelSprite);
        objective=Label(panel,"",new Vector2(390,225),new Vector2(.5f,.5f),new Vector2(0,-8),21,TextAnchor.MiddleLeft);
        promptPanel=Box("Interaction Backing",go.transform,new Vector2(860,165),new Vector2(.5f,0),new Vector2(0,195),Color.white,journalSprite != null ? journalSprite : panelSprite).gameObject;
        prompt=Label(promptPanel.transform,"",new Vector2(730,115),new Vector2(.5f,.5f),Vector2.zero,25);
        toastPanel=Box("Message Backing",go.transform,new Vector2(1080,80),new Vector2(.5f,1),new Vector2(0,-300),Color.white,journalSprite != null ? journalSprite : panelSprite).gameObject;
        toast=Label(toastPanel.transform,"",new Vector2(960,52),new Vector2(.5f,.5f),Vector2.zero,25);
        BuildFieldMap(go.transform);
        AddJournalTitles(panel);
        map.SetActive(false);
    }
}
