using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class RPG2Adventure
{
    private Canvas ui;
    private GameObject menu,mapPage,questPage;
    private TMP_Text toastText,promptText,tracked,questTitle,questBody,questProgress,questReward,questActionText,mapSummary;
    private TMP_Text[] questNames;
    private Image[] rewardIcons;
    private TMP_Text[] rewardAmounts;
    private Button questAction;
    private RectTransform mapPlayer,miniPlayer;
    private readonly List<Canvas> hiddenCanvases=new List<Canvas>();
    private readonly List<RectTransform> mapPins=new List<RectTransform>();
    private int selectedQuest;
    private bool showingMap;
    private float toastUntil;
    private static readonly Color Ink=new Color32(62,54,43,255);
    private RectTransform UIRect(string name,Transform parent,Vector2 size,Vector2 position)
    {
        var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.sizeDelta=size;r.anchoredPosition=position;return r;
    }
    private Image UIBox(string name,Transform parent,Vector2 size,Vector2 position,Sprite sprite,Color? tint=null)
    {
        var i=UIRect(name,parent,size,position).gameObject.AddComponent<Image>();i.sprite=sprite;i.type=sprite!=null&&sprite.border.sqrMagnitude>0?Image.Type.Sliced:Image.Type.Simple;i.color=tint??Color.white;i.raycastTarget=false;i.pixelsPerUnitMultiplier=2;return i;
    }
    private TMP_Text UILabel(string name,string value,Transform parent,Vector2 size,Vector2 position,int fontSize,TextAlignmentOptions align=TextAlignmentOptions.Center)
    {
        var t=UIRect(name,parent,size,position).gameObject.AddComponent<TextMeshProUGUI>();t.font=font;t.fontSize=fontSize;t.color=Ink;t.text=value;t.alignment=align;t.raycastTarget=false;t.richText=false;return t;
    }
    private Button UIButton(string label,Transform parent,Vector2 size,Vector2 pos,UnityEngine.Events.UnityAction action)
    {
        var i=UIBox(label,parent,size,pos,button);i.raycastTarget=true;var b=i.gameObject.AddComponent<Button>();b.targetGraphic=i;b.onClick.AddListener(action);
        b.navigation=new Navigation{mode=Navigation.Mode.None};UILabel("Label",label,i.transform,size-new Vector2(20,8),Vector2.zero,24);return b;
    }
    private Vector2 ChartPoint(Vector2 at,Vector2 size)=>new Vector2((at.x-bounds.center.x)/bounds.width*size.x,(at.y-bounds.center.y)/bounds.height*size.y);
    private void BuildUI()
    {
        var root=new GameObject("RPG2 Adventure UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);
        ui=root.GetComponent<Canvas>();ui.renderMode=RenderMode.ScreenSpaceOverlay;ui.sortingOrder=130;
        var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var mini=UIBox("Mini map parchment",root.transform,new Vector2(350,252),new Vector2(760,-370),parchment);
        UIBox("Mini map",mini.transform,new Vector2(310,189),new Vector2(0,-6),chart);
        UILabel("Mini map title","苇岸渡口 · Tab 地图",mini.transform,new Vector2(310,36),new Vector2(0,102),22);
        miniPlayer=UILabel("Player marker","◆",mini.transform,new Vector2(24,28),Vector2.zero,23).rectTransform;miniPlayer.GetComponent<TMP_Text>().color=new Color32(183,55,42,255);
        tracked=UILabel("Tracked objective","",root.transform,new Vector2(320,132),new Vector2(760,-143),22,TextAlignmentOptions.MidlineLeft);
        var trackBack=UIBox("Tracked parchment",root.transform,new Vector2(350,160),new Vector2(760,-143),parchment);trackBack.transform.SetSiblingIndex(tracked.transform.GetSiblingIndex());
        var promptBack=UIBox("Prompt parchment",root.transform,new Vector2(960,78),new Vector2(0,-365),parchment);
        promptText=UILabel("Interact prompt","",promptBack.transform,new Vector2(890,62),Vector2.zero,27);
        var noticeBack=UIBox("Notification parchment",root.transform,new Vector2(1240,96),new Vector2(0,280),parchment);
        toastText=UILabel("Notification","",noticeBack.transform,new Vector2(1150,76),Vector2.zero,25);
        UIButton("委托 [R]",root.transform,new Vector2(185,60),new Vector2(-80,-478),()=>OpenMenu(false));
        UIButton("地图 [Tab]",root.transform,new Vector2(185,60),new Vector2(130,-478),()=>OpenMenu(true));
        var dim=UIBox("Adventure menu",root.transform,new Vector2(5000,5000),Vector2.zero,null,new Color(.025f,.055f,.05f,.72f));dim.raycastTarget=true;menu=dim.gameObject;
        var panel=UIBox("Parchment journal",menu.transform,new Vector2(1640,950),Vector2.zero,parchment);
        UIBox("Journal ribbon",panel.transform,new Vector2(650,90),new Vector2(0,367),ribbon);
        UILabel("Journal title","苇岸渡口 · 冒险手册",panel.transform,new Vector2(600,65),new Vector2(0,373),38);
        UIButton("委托",panel.transform,new Vector2(155,54),new Vector2(-555,364),()=>OpenMenu(false));
        UIButton("地图",panel.transform,new Vector2(155,54),new Vector2(-375,364),()=>OpenMenu(true));
        UIButton("关闭",panel.transform,new Vector2(130,54),new Vector2(585,364),CloseMenu);
        questPage=UIRect("Quest page",panel.transform,new Vector2(1640,950),Vector2.zero).gameObject;
        var left=UIBox("Quest list scroll",questPage.transform,new Vector2(640,1060),new Vector2(-505,-10),questScroll??parchment);
        var right=UIBox("Quest details scroll",questPage.transform,new Vector2(1060,1060),new Vector2(300,-10),questScroll??parchment);
        UIBox("Quest list red ribbon",questPage.transform,new Vector2(625,150),new Vector2(-505,365),questRibbon??ribbon);
        UILabel("Quest list caption","委托日志",questPage.transform,new Vector2(490,90),new Vector2(-505,375),48);
        UIBox("Quest name red ribbon",questPage.transform,new Vector2(1000,150),new Vector2(300,270),questRibbon??ribbon);
        questNames=new TMP_Text[4];
        for(int i=0;i<4;i++)
        {int index=i;var b=UIButton(QuestTitle(i),questPage.transform,new Vector2(470,90),new Vector2(-505,170-i*118),()=>{selectedQuest=index;RefreshQuest();});questNames[i]=b.GetComponentInChildren<TMP_Text>();questNames[i].fontSize=26;}
        UILabel("Side story note","支线：钓鱼、护送小羊\n靠近物体按 E 互动",questPage.transform,new Vector2(470,82),new Vector2(-505,-325),24);
        questTitle=UILabel("Quest name","",questPage.transform,new Vector2(850,95),new Vector2(300,280),42);
        UIBox("Quest description paper",questPage.transform,new Vector2(925,365),new Vector2(300,20),questPaper??parchment);
        questBody=UILabel("Quest description","",questPage.transform,new Vector2(850,152),new Vector2(300,109),27,TextAlignmentOptions.TopLeft);
        questProgress=UILabel("Quest objectives","",questPage.transform,new Vector2(810,160),new Vector2(310,-63),29,TextAlignmentOptions.TopLeft);
        UILabel("Reward title","任务奖励",questPage.transform,new Vector2(800,55),new Vector2(300,-198),31);
        UIBox("Reward blue tray",questPage.transform,new Vector2(940,124),new Vector2(300,-277),rewardSlot??button);
        rewardIcons=new Image[3];rewardAmounts=new TMP_Text[3];
        for(int i=0;i<3;i++)
        {
            float x=75+i*235;
            rewardIcons[i]=UIBox("Reward icon "+i,questPage.transform,new Vector2(76,76),new Vector2(x,-273),null);
            rewardIcons[i].preserveAspect=true;
            rewardAmounts[i]=UILabel("Reward quantity "+i,"",questPage.transform,new Vector2(120,55),new Vector2(x+82,-280),31);
            rewardAmounts[i].color=Color.white;var outline=rewardAmounts[i].gameObject.AddComponent<Outline>();outline.effectColor=Ink;outline.effectDistance=new Vector2(2,-2);
        }
        questReward=UILabel("Quest reward names","",questPage.transform,new Vector2(880,38),new Vector2(300,-354),21);
        questAction=UIButton("接取委托",questPage.transform,new Vector2(285,65),new Vector2(540,-407),()=>Accept(selectedQuest));questActionText=questAction.GetComponentInChildren<TMP_Text>();
        UILabel("Quest controls","R / E 关闭",questPage.transform,new Vector2(260,42),new Vector2(-105,-407),23);
        questPage.transform.SetAsLastSibling();
        mapPage=UIRect("Map page",panel.transform,new Vector2(1300,680),new Vector2(0,-28)).gameObject;
        var area=UIBox("Actual authored terrain atlas",mapPage.transform,new Vector2(1230,750),new Vector2(0,5),chart);
        // Keep the atlas to its real 64:39 world aspect ratio.
        area.rectTransform.sizeDelta=new Vector2(1150,701);area.rectTransform.anchoredPosition=new Vector2(0,0);
        for(int i=0;i<points.Length;i++)
        {
            var p=points[i];if(p.kind==RPG2Point.Kind.Mushroom||p.kind==RPG2Point.Kind.Pumpkin||p.kind==RPG2Point.Kind.Timber)continue;
            int index=i;var pos=ChartPoint(p.transform.position,area.rectTransform.sizeDelta);
            if(p.kind==RPG2Point.Kind.Board)pos+=new Vector2(45,24);
            if(p.kind==RPG2Point.Kind.Camp)pos+=new Vector2(-28,-16);
            var b=UIButton(PinCaption(p),area.transform,new Vector2(70,38),pos,()=>{mapSummary.text=points[index].title+"："+points[index].description;});
            var plate=b.GetComponent<Image>();plate.sprite=null;plate.color=new Color32(240,228,179,245);
            var outline=plate.gameObject.AddComponent<Outline>();outline.effectColor=new Color32(83,73,52,255);outline.effectDistance=new Vector2(2,-2);
            var label=b.GetComponentInChildren<TMP_Text>();label.fontSize=21;label.rectTransform.sizeDelta=new Vector2(68,36);label.enableWordWrapping=false;
            mapPins.Add(b.GetComponent<RectTransform>());
        }
        mapPlayer=UILabel("Your position","◆",area.transform,new Vector2(30,35),Vector2.zero,31).rectTransform;mapPlayer.GetComponent<TMP_Text>().color=new Color32(192,54,37,255);mapPlayer.SetAsLastSibling();
        mapSummary=UILabel("Map legend","◆ 你的位置 · 点击地点查看说明 · 桥需先修复 · R 委托 / Tab 关闭",mapPage.transform,new Vector2(1260,70),new Vector2(0,-397),22);
        menu.SetActive(false);RefreshQuest();
    }
    private string PinCaption(RPG2Point p)
    {
        if(p.kind==RPG2Point.Kind.Beacon)return new[]{"水灯","林灯","遗灯"}[p.number];
        if(p.kind==RPG2Point.Kind.Camp)return "营火";
        if(p.kind==RPG2Point.Kind.Board)return "委托";
        if(p.kind==RPG2Point.Kind.Bridge)return "渡桥";
        if(p.kind==RPG2Point.Kind.Treasure)return "宝藏";
        if(p.kind==RPG2Point.Kind.Fishing)return "钓鱼";
        if(p.kind==RPG2Point.Kind.Sheep)return "小羊";
        if(p.kind==RPG2Point.Kind.Inn)return "旅舍";
        return "路标";
    }
    public void Notify(string value){if(toastText==null)return;toastText.text=value;toastUntil=Time.unscaledTime+5.5f;}
    public void OpenMenu(bool map)
    {
        if(ui==null||BackpackUI.BlocksOtherInput||ShopKeeper.currentShopKeeper!=null)return;
        if(!MenuOpen)
        {
            hiddenCanvases.Clear();
            foreach(var canvas in FindObjectsOfType<Canvas>().Where(c=>c!=ui&&c.enabled&&c.isRootCanvas&&c.renderMode==RenderMode.ScreenSpaceOverlay)){hiddenCanvases.Add(canvas);canvas.enabled=false;}
        }
        showingMap=map;menu.SetActive(true);mapPage.SetActive(map);questPage.SetActive(!map);MenuPause.SetPaused(this,true);RefreshQuest();
        var panel=menu.transform.Find("Parchment journal");
        panel.Find("Journal ribbon").gameObject.SetActive(map);panel.Find("Journal title").gameObject.SetActive(map);
        panel.Find("委托").gameObject.SetActive(map);panel.Find("地图").gameObject.SetActive(map);panel.Find("关闭").SetAsLastSibling();
    }
    private void ToggleMenu(bool map){if(MenuOpen&&showingMap==map)CloseMenu();else OpenMenu(map);}
    public void CloseMenu()
    {
        if(menu!=null)menu.SetActive(false);MenuPause.SetPaused(this,false);
        foreach(var canvas in hiddenCanvases)if(canvas!=null)canvas.enabled=true;hiddenCanvases.Clear();
    }
    private void RefreshQuest()
    {
        if(questTitle==null)return;
        questTitle.text=QuestTitle(selectedQuest);questBody.text=QuestDescription(selectedQuest);questProgress.text=QuestProgress(selectedQuest);questReward.text=QuestReward(selectedQuest);
        var rewards=QuestRewards(selectedQuest).ToArray();
        for(int i=0;i<rewardIcons.Length;i++)
        {
            bool used=i<rewards.Length;rewardIcons[i].gameObject.SetActive(used);rewardAmounts[i].gameObject.SetActive(used);
            if(used){rewardIcons[i].sprite=RewardIcon(rewards[i].Key);rewardAmounts[i].text="× "+rewards[i].Value;}
        }
        questActionText.text=QuestDone(selectedQuest)?"已完成":accepted[selectedQuest]?"已接取 · 前往完成":"接取委托";questAction.interactable=!QuestDone(selectedQuest)&&!accepted[selectedQuest];
        for(int i=0;i<4;i++)questNames[i].text=(QuestDone(i)?"✓ ":accepted[i]?"· ":"")+QuestTitle(i);
    }
    private void RefreshUI(bool blocked)
    {
        if(ui==null)return;ui.enabled=!blocked;
        toastText.transform.parent.gameObject.SetActive(Time.unscaledTime<toastUntil&&!MenuOpen);
        miniPlayer.anchoredPosition=ChartPoint(Player.transform.position,new Vector2(310,189))+new Vector2(0,-6);
        mapPlayer.anchoredPosition=ChartPoint(Player.transform.position,new Vector2(1150,701));
        int current=Enumerable.Range(0,4).FirstOrDefault(i=>!QuestDone(i));tracked.text="苇岸渡口\n"+QuestTitle(current)+"\nR 查看目标 · Tab 地图";
        if(MenuOpen)RefreshQuest();
        promptText.transform.parent.gameObject.SetActive(!Busy&&(NearestIndex>=0||fishing!=null));
        if(fishing!=null)promptText.text=Time.time>=fishReady?"鱼儿咬钩！现在按 E 收竿":"等待水面的动静… 暂时不要收竿";
        else if(NearestIndex>=0)promptText.text=Hint(points[NearestIndex]);
    }
}
