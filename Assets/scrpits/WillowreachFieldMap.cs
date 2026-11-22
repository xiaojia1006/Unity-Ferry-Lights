using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class WillowreachWorld
{
    private static readonly Vector2 ChartSize=new Vector2(930,636);
    private readonly List<WillowreachNode> destinations=new List<WillowreachNode>();
    private readonly List<Text> destinationLabels=new List<Text>();
    private readonly List<Text> destinationNumbers=new List<Text>();
    private Text mapStatus,mapDetails;
    private WillowreachNode selectedDestination;
    private RectTransform waypoint;
    private RectTransform shortcutMark;
    private static Vector2 ChartPoint(Vector2 p)=>new Vector2(Mathf.Clamp(p.x/76,-.5f,.5f)*ChartSize.x,Mathf.Clamp(p.y/52,-.5f,.5f)*ChartSize.y);
    private void AddJournalTitles(RectTransform panel)
    {
        var ribbon=Box("Journal ribbon",panel,new Vector2(260,48),new Vector2(.5f,1),new Vector2(0,-17),Color.white,ribbonSprite);
        var journalTitle=Label(ribbon,"旅行手记",new Vector2(220,38),Vector2.one*.5f,new Vector2(0,5),22);
        journalTitle.alignByGeometry=true;
        objective.resizeTextForBestFit=true;objective.resizeTextMinSize=18;objective.resizeTextMaxSize=21;
    }
    private Button MapButton(string name,Transform parent,Vector2 size,Vector2 position,System.Action action)
    {
        var theme=Resources.Load<BackpackTheme>("BackpackTheme");
        var rect=Box(name,parent,size,Vector2.one*.5f,position,Color.white,theme!=null?theme.slot:panelSprite);
        var img=rect.GetComponent<Image>();img.pixelsPerUnitMultiplier=4;img.raycastTarget=true;var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=img;button.onClick.AddListener(()=>action());return button;
    }
    private string MapCaption(WillowreachNode n)
    {
        if(n.kind==WillowreachNode.Kind.Craft)return "骨笛工坊";
        if(n.kind==WillowreachNode.Kind.Shortcut)return "返乡渡桥";
        if(n.kind==WillowreachNode.Kind.Adventure)return n.name.StartsWith("Resonance rune")?"共鸣符石":n.title;
        return n.title.Replace(" · 查看地图","").Replace("天气观测站","观测站");
    }
    private Color PinColor(WillowreachNode n)=>n.completed?new Color(.29f,.45f,.33f):n.kind==WillowreachNode.Kind.Survey?new Color(.69f,.38f,.18f):n.kind==WillowreachNode.Kind.Shop?new Color(.24f,.43f,.53f):new Color(.47f,.32f,.46f);
    private void BuildFieldMap(Transform parent)
    {
        map=Box("Field Map",parent,new Vector2(1480,920),Vector2.one*.5f,Vector2.zero,Color.white,journalSprite!=null?journalSprite:panelSprite).gameObject;
        var mapImage=map.GetComponent<Image>();mapImage.raycastTarget=true;
        var heading=Box("Atlas ribbon",map.transform,new Vector2(650,76),new Vector2(.5f,1),new Vector2(0,-62),Color.white,ribbonSprite);
        var atlasTitle=Label(heading,"柳湾边境 · 探索地图",new Vector2(570,54),Vector2.one*.5f,new Vector2(0,8),32);
        atlasTitle.alignByGeometry=true;
        var close=MapButton("Close map",map.transform,new Vector2(86,58),new Vector2(640,385),()=>{map.SetActive(false);MenuPause.SetPaused(this,false);});
        Label(close.transform,"关闭",new Vector2(75,40),Vector2.one*.5f,Vector2.zero,21);
        var chart=Box("Terrain",map.transform,ChartSize,Vector2.one*.5f,new Vector2(-210,-5),Color.white,mapSprite);
        Label(chart,"风 雪 遗 迹",new Vector2(220,32),Vector2.one*.5f,ChartPoint(new Vector2(-19,23)),24);
        Label(chart,"晴 光 矿 场",new Vector2(220,32),Vector2.one*.5f,ChartPoint(new Vector2(22,23)),24);
        Label(chart,"柳 湾 村",new Vector2(180,32),Vector2.one*.5f,ChartPoint(new Vector2(-23,-24)),24);
        Label(chart,"雨 露 林 地",new Vector2(200,32),Vector2.one*.5f,ChartPoint(new Vector2(22,-24)),24);
        Label(chart,"北 ↑",new Vector2(70,50),new Vector2(1,1),new Vector2(-45,-32),22);
        // Distinct numbers preserve geographic positions without long labels colliding.
        foreach(var n in nodes.Where(n=>n.kind==WillowreachNode.Kind.Survey||n.kind==WillowreachNode.Kind.Shop||n.kind==WillowreachNode.Kind.Craft||n.kind==WillowreachNode.Kind.Shortcut||n.kind==WillowreachNode.Kind.Adventure))
        {
            if(n.name.StartsWith("Resonance rune")&&!n.name.EndsWith("1"))continue;
            destinations.Add(n);int number=destinations.Count;var destination=n;
            var pin=MapButton("Destination "+number,chart,new Vector2(40,38),ChartPoint(n.transform.position),()=>SelectDestination(destination));
            var pinLabel=Label(pin.transform,number.ToString(),new Vector2(32,30),Vector2.one*.5f,Vector2.zero,23);pinLabel.fontStyle=FontStyle.Bold;pinLabel.color=PinColor(n);destinationNumbers.Add(pinLabel);
        }
        shortcutMark=Box("Shortcut bridge status",chart,new Vector2(26,26),Vector2.one*.5f,ChartPoint(shortcutBridgeArea.center),new Color(.5f,.2f,.18f));
        Label(shortcutMark,"×",new Vector2(26,26),Vector2.one*.5f,Vector2.zero,22).color=Color.white;
        waypoint=Box("Waypoint",chart,new Vector2(24,24),Vector2.one*.5f,Vector2.zero,new Color(.8f,.3f,.15f));
        waypoint.localRotation=Quaternion.Euler(0,0,45);waypoint.gameObject.SetActive(false);
        marker=Box("You",chart,new Vector2(24,24),Vector2.one*.5f,Vector2.zero,new Color(.13f,.39f,.48f));
        marker.localRotation=Quaternion.Euler(0,0,45);Box("Player center",marker,new Vector2(10,10),Vector2.one*.5f,Vector2.zero,new Color(1,.96f,.7f));
        var sidebar=Box("Destinations",map.transform,new Vector2(340,660),Vector2.one*.5f,new Vector2(465,0),new Color(1,1,1,.95f),journalSprite);
        Label(sidebar,"地 点 索 引",new Vector2(270,42),new Vector2(.5f,1),new Vector2(0,-34),26);
        for(int i=0;i<destinations.Count;i++)
        {
            var destination=destinations[i];int number=i+1;
            var row=MapButton("Index "+number,sidebar,new Vector2(275,32),new Vector2(0,245-i*34),()=>SelectDestination(destination));
            var text=Label(row.transform,"",new Vector2(250,36),Vector2.one*.5f,Vector2.zero,19,TextAnchor.MiddleLeft);destinationLabels.Add(text);
        }
        mapDetails=Label(sidebar,"点击地图编号或地点名称\n放置一个目的地标记",new Vector2(265,95),new Vector2(.5f,0),new Vector2(0,105),18,TextAnchor.UpperLeft);
        mapDetails.resizeTextForBestFit=true;mapDetails.resizeTextMinSize=15;mapDetails.resizeTextMaxSize=18;
        var clear=MapButton("Clear waypoint",sidebar,new Vector2(230,38),new Vector2(0,-275),()=>{selectedDestination=null;waypoint.gameObject.SetActive(false);});
        Label(clear.transform,"清除目的地",new Vector2(210,34),Vector2.one*.5f,Vector2.zero,19);
        mapStatus=Label(map.transform,"",new Vector2(1240,34),new Vector2(.5f,0),new Vector2(0,111),22);
        Label(map.transform,"◆ 蓝色：你的位置    编号：地点    绿色：已完成    红叉：待修渡桥",new Vector2(1300,34),new Vector2(.5f,0),new Vector2(0,77),20);
        Label(map.transform,"Tab / Esc 关闭地图   ·   点击地点标记目的地   ·   地图打开时暂停游戏",new Vector2(1300,32),new Vector2(.5f,0),new Vector2(0,44),20);
    }
    private void SelectDestination(WillowreachNode destination)
    {selectedDestination=destination;waypoint.anchoredPosition=ChartPoint(destination.transform.position);waypoint.gameObject.SetActive(true);waypoint.SetAsLastSibling();marker.SetAsLastSibling();}
    private void UpdateFieldMap()
    {
        if(marker==null)return;marker.anchoredPosition=ChartPoint(player.position);
        if(!map.activeSelf)return;
        mapStatus.text="当前位置："+Region(player.position)+"    天气："+WeatherName(weather.currentWeather)+"    天气观测："+SurveyCount+" / 3";
        for(int i=0;i<destinations.Count;i++)
        {var n=destinations[i];destinationLabels[i].text=(i+1).ToString("00")+"  "+MapCaption(n)+(n.completed?"  ✓":"");destinationNumbers[i].color=PinColor(n);}
        if(shortcutMark!=null)shortcutMark.gameObject.SetActive(shortcutBridge==null||!shortcutBridge.activeInHierarchy);
        if(selectedDestination!=null)
            mapDetails.text=MapCaption(selectedDestination)+"\n"+(selectedDestination.completed?"已完成 · ":"")+"直线距离 "+Vector2.Distance(player.position,selectedDestination.transform.position).ToString("0")+" 米\n"+selectedDestination.hint;
        else mapDetails.text="点击地图编号或地点名称\n放置一个目的地标记\n河流仅在桥梁处适合通行";
    }
}
