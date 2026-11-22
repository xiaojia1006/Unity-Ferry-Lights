using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// RPG1 only: presentation and interaction for the authored frontier. No memory-stone gameplay.
public sealed class RPG1Activities : MonoBehaviour
{
    public Transform[] posts;
    public Sprite parchment,ribbon,chart,button;
    public Rect worldRect;
    public ItemSO gold,experience;
    public RPG1Frontier frontier;
    private Transform player;
    private Font font;
    private Canvas canvas;
    private GameObject atlas,promptPanel,toastPanel;
    private Text journal,prompt,toast,mapDetails;
    private readonly List<RectTransform> you=new List<RectTransform>();
    private readonly List<Text> pins=new List<Text>();
    private ShopKeeper[] shops;
    private float noticeUntil;
    private static RPG1Activities current;
    private static int mapClosedFrame=-1;
    public static bool BlocksMenuInput => (current!=null&&current.atlas!=null&&current.atlas.activeSelf)||mapClosedFrame==Time.frameCount;
    public string Status => frontier!=null?frontier.Journal:"场景建设中";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSession(){current=null;mapClosedFrame=-1;}
    private void Start()
    {
        current=this;var p=FindObjectOfType<PlayerMovement>();if(p!=null)player=p.transform;
        font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","SimHei","Arial"},20);
        foreach(var label in GetComponentsInChildren<TextMesh>()){label.font=font;label.GetComponent<MeshRenderer>().sharedMaterial=font.material;label.GetComponent<MeshRenderer>().sortingOrder=31001;var backing=label.transform.Find("Parchment sign backing");if(backing!=null)backing.GetComponent<SpriteRenderer>().sortingOrder=31000;}
        BuildUI();Notify("风鸣伐木场：恢复东西供能，开启北部工事。E互动，Tab地图，B背包。");
    }
    private void OnDisable(){MenuPause.SetPaused(this,false);if(current==this)current=null;}
    private void Update()
    {
        if (GlobalSettings.BlocksInput) return;
        if(player==null||canvas==null)return;
        bool blocked=BackpackUI.BlocksOtherInput||ShopKeeper.currentShopKeeper!=null;
        if(blocked&&atlas.activeSelf)CloseMap();
        if(Input.GetKeyDown(KeyCode.Tab)&&!blocked&&(atlas.activeSelf||Time.timeScale>0)){if(atlas.activeSelf)CloseMap();else{atlas.SetActive(true);MenuPause.SetPaused(this,true);}}
        if(atlas.activeSelf&&Input.GetKeyDown(KeyCode.Escape))CloseMap();
        canvas.enabled=!blocked&&(Time.timeScale>0||atlas.activeSelf);
        foreach(var mark in you)mark.anchoredPosition=MapPoint(player.position,mark.parent.GetComponent<RectTransform>().sizeDelta);
        journal.text="风鸣伐木场\n"+Status+"\nE 互动 · Tab 地图";
        for(int i=0;i<pins.Count;i++)pins[i].color=frontier!=null&&frontier.Done(i%posts.Length)?new Color(.18f,.47f,.25f):new Color(.36f,.23f,.15f);
        toastPanel.SetActive(Time.unscaledTime<noticeUntil);
        if(blocked||atlas.activeSelf||Time.timeScale<=0||frontier==null){promptPanel.SetActive(false);return;}
        int near=Nearest(player.position);
        if(near<0)
        {
            bool shop=NearShop();
            promptPanel.SetActive(shop);
            if(shop)prompt.text="E 商店";
            return;
        }
        promptPanel.SetActive(true);
        prompt.text=(near==6?"K ":"E ")+frontier.Caption(near)+(near==1||near==2?" · 按住2秒":"");
        if(Input.GetKeyDown(KeyCode.E))Interact(near);
    }
    // The merchants had no HUD hint at all, so E looked broken next to them.
    private bool NearShop()
    {
        if(shops==null)shops=FindObjectsOfType<ShopKeeper>();
        for(int i=0;i<shops.Length;i++)if(shops[i]!=null&&Vector2.Distance(player.position,shops[i].transform.position)<1.9f)return true;
        return false;
    }
    public int Nearest(Vector2 at)
    {
        int best=-1;float distance=1.65f;
        for(int i=0;i<posts.Length;i++)
        {
            if(posts[i]==null)continue;float d=Vector2.Distance(at,posts[i].position);
            if(d>=distance)continue;
            bool blocked=Physics2D.LinecastAll(at,posts[i].position,1<<7).Any(h=>h.collider!=null&&!h.collider.isTrigger);
            if(!blocked){distance=d;best=i;}
        }
        return best;
    }
    public void Interact(int index){if(frontier!=null&&index>=0&&index<posts.Length)frontier.Interact(index);}
    public void Notify(string message){if(toast==null)return;toast.text=message;noticeUntil=Time.unscaledTime+5;}
    private void CloseMap(){atlas.SetActive(false);mapClosedFrame=Time.frameCount;MenuPause.SetPaused(this,false);}
    private Vector2 MapPoint(Vector2 p,Vector2 size)=>new Vector2((Mathf.InverseLerp(worldRect.xMin,worldRect.xMax,p.x)-.5f)*size.x,(Mathf.InverseLerp(worldRect.yMin,worldRect.yMax,p.y)-.5f)*size.y);
    // The baked atlas is a real picture of the field, so it must keep the world's aspect instead of being stretched flat.
    private Vector2 ChartSize(float height){float aspect=worldRect.height>0f?worldRect.width/worldRect.height:1.39f;return new Vector2(Mathf.Round(height*aspect),height);}
    private RectTransform Box(string name,Transform parent,Vector2 size,Vector2 anchor,Vector2 pos,Sprite art)
    {var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.sizeDelta=size;r.anchorMin=r.anchorMax=anchor;r.anchoredPosition=pos;var image=go.GetComponent<Image>();image.sprite=art;image.type=art!=null&&art.border.sqrMagnitude>0?Image.Type.Sliced:Image.Type.Simple;image.color=Color.white;image.raycastTarget=false;return r;}
    private Text Label(Transform parent,string value,Vector2 size,Vector2 position,int points,TextAnchor align=TextAnchor.MiddleCenter)
    {var go=new GameObject("Label",typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var r=(RectTransform)go.transform;r.sizeDelta=size;r.anchorMin=r.anchorMax=Vector2.one*.5f;r.anchoredPosition=position;var t=go.GetComponent<Text>();t.font=font;t.text=value;t.fontSize=points;t.alignment=align;t.alignByGeometry=true;t.color=new Color(.24f,.19f,.14f);t.raycastTarget=false;return t;}
    private void Map(Transform parent,Vector2 size)
    {
        var r=Box("Actual terrain",parent,size,Vector2.one*.5f,Vector2.zero,chart);
        if(size.x>500)
        {
            // Same marker language as the RPG2 map: flat parchment plate, hard outline, dark character.
            for(int i=0;i<posts.Length;i++)
            {
                int index=i;
                var badge=Box("Destination "+i,r,new Vector2(56,36),Vector2.one*.5f,MapPoint(posts[i].position,size),null);
                var plate=badge.GetComponent<Image>();plate.color=new Color32(240,228,179,245);plate.raycastTarget=true;
                var outline=badge.gameObject.AddComponent<Outline>();outline.effectColor=new Color32(83,73,52,255);outline.effectDistance=new Vector2(2,-2);
                var t=Label(badge,new[]{"营","西","东","门","箱","战","宝"}[i],new Vector2(54,32),Vector2.zero,19);pins.Add(t);
                var select=badge.gameObject.AddComponent<Button>();select.navigation=new Navigation{mode=Navigation.Mode.None};
                select.onClick.AddListener(()=>{mapDetails.text=posts[index].name+" · 距离 "+Vector2.Distance(player.position,posts[index].position).ToString("0.0")+"米\n"+(frontier!=null?frontier.Hint(index):"E 互动")+"\nTab / Esc 关闭地图";});
            }
        }
        if(size.x>500)
        {
            foreach(var shop in FindObjectsOfType<ShopKeeper>().Where(s=>s.gameObject.scene==gameObject.scene))Label(r,"商",new Vector2(28,24),MapPoint(shop.transform.position,size),20).color=new Color(.6f,.32f,.1f);
            foreach(var patch in GetComponentsInChildren<RPG1Harvest>())Label(r,"采",new Vector2(24,24),MapPoint(patch.transform.position,size),18).color=new Color(.15f,.4f,.18f);
            foreach(var sheep in GetComponentsInChildren<RPG1LostSheep>())Label(r,"羊",new Vector2(24,24),MapPoint(sheep.home,size),18);
            foreach(var chest in GetComponentsInChildren<RPG1ChestSession>())Label(r,"宝",new Vector2(24,24),MapPoint(chest.transform.position,size),18).color=new Color(.6f,.32f,.1f);
        }
        var mark=Label(r,"◆",new Vector2(28,28),Vector2.zero,size.x>500?26:20);mark.color=new Color(.85f,.15f,.13f);you.Add(mark.rectTransform);
    }
    private void BuildUI()
    {
        var go=new GameObject("RPG1 Field HUD",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));go.transform.SetParent(transform,false);canvas=go.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=7;var scale=go.GetComponent<CanvasScaler>();scale.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scale.referenceResolution=new Vector2(1920,1080);scale.matchWidthOrHeight=.5f;
        var mini=Box("Mini map",go.transform,new Vector2(360,268),new Vector2(1,0),new Vector2(-200,170),parchment);
        var title=Box("Ribbon",mini,new Vector2(300,45),Vector2.one*.5f,new Vector2(0,111),ribbon);Label(title,frontier!=null?"风鸣伐木场 · 小地图":"林间营地 · 小地图",new Vector2(265,34),new Vector2(0,5),22);
        var chartRoot=new GameObject("Chart area",typeof(RectTransform)).GetComponent<RectTransform>();chartRoot.SetParent(mini,false);chartRoot.anchoredPosition=new Vector2(0,-10);Map(chartRoot,ChartSize(185));
        Label(mini,frontier!=null?"◆ 玩家  西/东 供能  战 工事  Tab地图":"◆ 玩家   委 委托   商 商人   Tab 展开",new Vector2(320,25),new Vector2(0,-112),16);
        var journalBox=Box("Activity journal",go.transform,new Vector2(360,165),new Vector2(1,0),new Vector2(-200,390),parchment);journal=Label(journalBox,"",new Vector2(300,125),Vector2.zero,21,TextAnchor.MiddleLeft);
        promptPanel=Box("Interaction",go.transform,new Vector2(650,62),new Vector2(.5f,0),new Vector2(0,170),parchment).gameObject;prompt=Label(promptPanel.transform,"",new Vector2(590,40),Vector2.zero,23);
        toastPanel=Box("Notification",go.transform,new Vector2(980,86),new Vector2(.5f,1),new Vector2(0,-95),parchment).gameObject;toast=Label(toastPanel.transform,"",new Vector2(900,65),Vector2.zero,23);
        atlas=Box("RPG1 Atlas",go.transform,new Vector2(1320,900),Vector2.one*.5f,Vector2.zero,parchment).gameObject;atlas.GetComponent<Image>().raycastTarget=true;
        var banner=Box("Atlas ribbon",atlas.transform,new Vector2(620,76),new Vector2(.5f,1),new Vector2(0,-65),ribbon);Label(banner,frontier!=null?"风鸣伐木场 · 探索地图":"林间营地 · 探索地图",new Vector2(550,54),new Vector2(0,8),32);
        var area=new GameObject("Map area",typeof(RectTransform)).GetComponent<RectTransform>();area.SetParent(atlas.transform,false);area.anchoredPosition=new Vector2(0,40);Map(area,ChartSize(600));
        mapDetails=Label(atlas.transform,"点击地图标记查看玩法说明",new Vector2(1140,125),new Vector2(0,-340),22);
        if(frontier!=null)mapDetails.text="西：清除守卫，恢复供能。东：推木箱上金色压板，启动装置。\n门：双供能开门。战：唤醒北部守卫。箱：卡住可复位。营：恢复生命和通关结算。\n金色预警区随后变红并造成伤害，可绕行。点击地图标记查看详情，Tab / Esc 关闭。";
        var close=Box("Close",atlas.transform,new Vector2(96,54),new Vector2(1,1),new Vector2(-92,-72),button);close.GetComponent<Image>().pixelsPerUnitMultiplier=2f;close.GetComponent<Image>().raycastTarget=true;var b=close.gameObject.AddComponent<Button>();b.navigation=new Navigation{mode=Navigation.Mode.None};b.onClick.AddListener(CloseMap);Label(close,"关闭",new Vector2(88,42),Vector2.zero,22);atlas.SetActive(false);
    }
}
