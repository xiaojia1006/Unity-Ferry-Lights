using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Built from a serialized theme so the same UI works in scenes and player builds.
[DefaultExecutionOrder(-100)]
public class BackpackUI : MonoBehaviour
{
    private static BackpackUI current;
    private static int closedFrame = -1;
    public static bool BlocksOtherInput { get { return GlobalSettings.BlocksInput || (current != null && current.IsOpen) || closedFrame == Time.frameCount; } }
    private InventortyManager inventory;
    private BackpackTheme theme;
    private Canvas canvas;
    private GameObject window;
    private RectTransform portraitRoot;
    private readonly List<Image> portraitLayers = new List<Image>();
    private readonly List<BackpackCell> cells = new List<BackpackCell>();
    private SpriteRenderer[] playerSprites;
    private PlayerMovement player;
    private TMP_Text stats, capacity, description, status;
    private Image selectedIcon, dragIcon;
    private Button useButton, dropButton;
    private int selected = -1, filter, dragging = -1;
    private bool splitDrag;
    private float nextRefresh;
    public bool IsOpen { get { return window != null && window.activeSelf; } }
    public int SelectedIndex { get { return selected; } }

    private void Start()
    {
        inventory = GetComponent<InventortyManager>();
        if (inventory == null || InventortyManager.Instance != inventory) { enabled = false; return; }
        theme = Resources.Load<BackpackTheme>("BackpackTheme");
        if (theme == null) { Debug.LogError("BackpackTheme asset is missing."); enabled = false; return; }
        Build();
        current = this;
    }

    private void Update()
    {
        if (GlobalSettings.BlocksInput) return;
        if (Input.GetKeyDown(KeyCode.B)) SetOpen(!IsOpen);
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape)) { SetOpen(false); return; }
        if (Time.unscaledTime >= nextRefresh)
        {
            Refresh(); nextRefresh = Time.unscaledTime + 0.12f;
        }
        UpdatePortrait();
    }

    public void SetOpen(bool open)
    {
        if (window == null) return;
        // Don't cover an active shop, dialogue or skill menu with another pause owner.
        if (open && !IsOpen && Time.timeScale == 0f) return;
        window.SetActive(open);
        if (!open) closedFrame = Time.frameCount;
        MenuPause.SetPaused(this, open);
        EndDrag();
        if (open) { Refresh(); UpdatePortrait(); }
    }

    private void OnDisable()
    {
        MenuPause.SetPaused(this, false);
        if (window != null) window.SetActive(false);
        EndDrag();
    }

    private void OnDestroy()
    {
        if (canvas != null) Destroy(canvas.gameObject);
    }

    private RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false); rect.anchoredPosition = position; rect.sizeDelta = size;
        return rect;
    }

    private Image Art(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size, bool hit = false)
    {
        var image = Rect(name,parent,pos,size).gameObject.AddComponent<Image>();
        image.sprite = sprite; image.raycastTarget = hit;
        image.type = sprite != null && sprite.border.sqrMagnitude > 0 ? Image.Type.Sliced : Image.Type.Simple;
        image.pixelsPerUnitMultiplier = 1;
        return image;
    }

    private TMP_Text Text(string value, Transform parent, Vector2 pos, Vector2 size, float fontSize = 26)
    {
        var text = Rect(value,parent,pos,size).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = theme.font != null ? theme.font : TMP_Settings.defaultFontAsset;
        text.text = value; text.fontSize = fontSize;
        text.color = new Color32(36,37,40,255); text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false; text.enableWordWrapping = true;
        return text;
    }

    private Button Action(string label, Transform parent, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
    {
        var image = Art(label,parent,theme.button,pos,size,true);
        image.pixelsPerUnitMultiplier = 3;
        var button = image.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        button.onClick.AddListener(action);
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Text(label,image.transform,Vector2.zero,size - new Vector2(16,8),25);
        return button;
    }

    private void Build()
    {
        var root = new GameObject("Backpack Canvas", typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        root.transform.SetParent(transform,false);
        canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.overrideSorting = true; canvas.sortingOrder = 100;
        var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920,1080); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        Action("BAG [B]", root.transform,new Vector2(-780,-465),new Vector2(220,64),()=>SetOpen(!IsOpen));
        var overlay = Art("Backpack Window",root.transform,null,Vector2.zero,new Vector2(5000,5000),true);
        overlay.color = new Color(0.02f,0.04f,0.07f,0.65f); window = overlay.gameObject;
        var group = window.AddComponent<CanvasGroup>(); group.interactable = true; group.blocksRaycasts = true;
        var layout = Rect("Backpack Layout",window.transform,Vector2.zero,new Vector2(1490,860));
        // The source scroll includes transparent margins outside the parchment.
        Art("Player Scroll",layout,theme.parchment,new Vector2(-525,0),new Vector2(560,980),true);
        Art("Inventory Scroll",layout,theme.parchment,new Vector2(215,0),new Vector2(1200,980),true);
        var left = Rect("Player Panel",layout,new Vector2(-525,0),new Vector2(400,840));
        var right = Rect("Inventory Panel",layout,new Vector2(215,0),new Vector2(1040,840));
        Art("Player Ribbon",left.transform,theme.ribbon,new Vector2(0,340),new Vector2(360,96));
        Text("ADVENTURER",left.transform,new Vector2(0,340),new Vector2(310,56),34);
        Art("Bag Ribbon",right.transform,theme.ribbon,new Vector2(0,340),new Vector2(600,96));
        Text("BACKPACK",right.transform,new Vector2(0,340),new Vector2(500,56),40);
        Action("X",right.transform,new Vector2(455,345),new Vector2(64,64),()=>SetOpen(false));
        var portraitFrame = Art("Portrait Frame",left.transform,theme.slot,new Vector2(0,70),new Vector2(310,390));
        portraitFrame.gameObject.AddComponent<RectMask2D>();
        portraitRoot = Rect("Player Preview",portraitFrame.transform,Vector2.zero,new Vector2(240,300));
        stats = Text("",left.transform,new Vector2(0,-210),new Vector2(300,160),28);
        Text("YOUR CHARACTER",left.transform,new Vector2(0,-60),new Vector2(280,45),22);
        Text("B / ESC  CLOSE",left.transform,new Vector2(0,-350),new Vector2(300,46),24);
        Action("ALL",right.transform,new Vector2(-375,265),new Vector2(160,54),()=>SetFilter(0));
        Action("USABLE",right.transform,new Vector2(-185,265),new Vector2(180,54),()=>SetFilter(1));
        Action("MATERIALS",right.transform,new Vector2(30,265),new Vector2(210,54),()=>SetFilter(2));
        capacity = Text("",right.transform,new Vector2(330,265),new Vector2(260,48),24);
        for (int i = 0; i < inventory.itemSlots.Length; i++)
        {
            int col = i % 6, row = i / 6;
            var background = Art("Bag Slot " + (i+1),right.transform,theme.slot,new Vector2(-412+col*103,165-row*102),new Vector2(94,94),true);
            var cell = background.gameObject.AddComponent<BackpackCell>();
            cell.owner = this; cell.index = i; cell.background = background;
            cell.icon = Art("Item Icon",background.transform,null,Vector2.zero,new Vector2(98,98)); cell.icon.preserveAspect = true;
            cell.quantity = Text("",background.transform,new Vector2(8,-28),new Vector2(70,28),24);
            cell.quantity.color = Color.white;
            var shadow = cell.quantity.gameObject.AddComponent<Shadow>(); shadow.effectDistance = new Vector2(1,-1);
            Text(i < 5 ? "Q"+(i+1) : (i+1).ToString(),background.transform,new Vector2(-24,29),new Vector2(40,24),16);
            cells.Add(cell);
        }
        var detail = Art("Item Details",right.transform,theme.slot,new Vector2(335,-35),new Vector2(272,490));
        detail.color = new Color(1,1,1,0.8f);
        selectedIcon = Art("Selected Item",detail.transform,null,new Vector2(0,170),new Vector2(145,145)); selectedIcon.preserveAspect = true;
        description = Text("SELECT AN ITEM",detail.transform,new Vector2(0,-10),new Vector2(226,240),24);
        useButton = Action("USE",detail.transform,new Vector2(0,-174),new Vector2(208,55),UseSelected);
        dropButton = Action("DROP 1",right.transform,new Vector2(335,-318),new Vector2(210,55),DropSelected);
        Action("SORT BAG",right.transform,new Vector2(-350,-340),new Vector2(210,55),()=>{ inventory.SortBackpack(); selected=-1; Refresh(); });
        Text("DRAG: MOVE / MERGE    SHIFT + DRAG: SPLIT",right.transform,new Vector2(-10,-340),new Vector2(445,62),19);
        status = Text("FIRST 5 SLOTS SHARE YOUR QUICK BAR",right.transform,new Vector2(0,-387),new Vector2(910,35),19);
        dragIcon = Art("Dragged Item",root.transform,null,Vector2.zero,new Vector2(78,78)); dragIcon.preserveAspect = true;
        dragIcon.gameObject.SetActive(false);
        window.SetActive(false);
    }

    public static bool IsUsable(ItemSO item)
    {
        return item != null && (item.currentHealth > 0 || item.maxHealth > 0 || item.damage > 0 || item.speed > 0);
    }

    private void SetFilter(int value) { filter=value; selected=-1; Refresh(); }

    public void Select(int index, PointerEventData.InputButton button, int clicks)
    {
        if (dragging >= 0) return;
        selected=index; Refresh();
        if (button == PointerEventData.InputButton.Right) DropSelected();
        else if (button == PointerEventData.InputButton.Left && clicks == 2) UseSelected();
    }

    private InventorySlot Selected()
    {
        return selected >= 0 && selected < inventory.itemSlots.Length ? inventory.itemSlots[selected] : null;
    }

    private void UseSelected()
    {
        var slot=Selected(); if (slot == null || !IsUsable(slot.itemSO)) return;
        int before=slot.quantity; inventory.UseItem(slot); Refresh();
        status.text=slot.quantity < before ? "ITEM USED" : "NO EFFECT - CHECK YOUR HEALTH";
    }

    private void DropSelected()
    {
        var slot=Selected(); if (slot == null || slot.itemSO == null) return;
        int before=slot.quantity; inventory.DropLoot(slot); Refresh();
        status.text=slot.quantity < before ? "DROPPED NEAR YOUR CHARACTER" : "CANNOT DROP HERE";
    }

    public void BeginDrag(int index, PointerEventData data)
    {
        if (data.button != PointerEventData.InputButton.Left) return;
        var slot=inventory.itemSlots[index]; if (slot == null || slot.itemSO == null) return;
        dragging=index; splitDrag=Input.GetKey(KeyCode.LeftShift)||Input.GetKey(KeyCode.RightShift);
        dragIcon.sprite=slot.itemSO.icon; dragIcon.gameObject.SetActive(true); Drag(data);
    }

    public void Drag(PointerEventData data)
    {
        if (dragging < 0) return;
        Vector2 local;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,data.position,null,out local);
        dragIcon.rectTransform.anchoredPosition=local;
    }

    public void Drop(int target)
    {
        if (dragging < 0) return;
        bool moved=inventory.MoveItem(dragging,target,splitDrag); selected=target;
        EndDrag(); Refresh(); status.text=moved ? "ITEM MOVED" : "NO ROOM IN THIS STACK";
    }

    public void EndDrag() { dragging=-1; if (dragIcon != null) dragIcon.gameObject.SetActive(false); }

    public void Refresh()
    {
        int occupied=0;
        for(int i=0;i<cells.Count;i++)
        {
            var slot=inventory.itemSlots[i]; var cell=cells[i];
            bool has=slot != null && slot.itemSO != null && slot.quantity > 0;
            if(has) occupied++;
            bool match=!has || filter==0 || (filter==1 ? IsUsable(slot.itemSO) : !IsUsable(slot.itemSO));
            cell.icon.gameObject.SetActive(has); if(has) cell.icon.sprite=slot.itemSO.icon;
            cell.icon.color=new Color(1,1,1,match?1:0.18f);
            cell.quantity.text=has?slot.quantity.ToString():"";
            cell.background.color=i==selected ? new Color(1,0.84f,0.42f) : Color.white;
        }
        capacity.text=occupied+" / "+cells.Count+" SLOTS";
        var chosen=Selected(); var item=chosen != null ? chosen.itemSO : null;
        selectedIcon.gameObject.SetActive(item != null);
        if(item != null)
        {
            selectedIcon.sprite=item.icon;
            string effects="";
            if(item.currentHealth>0) effects+="\nHEAL +"+item.currentHealth;
            if(item.maxHealth>0) effects+="\nMAX HP +"+item.maxHealth;
            if(item.damage>0) effects+="\nDAMAGE +"+item.damage;
            if(item.speed>0) effects+="\nSPEED +"+item.speed;
            if(item.duration>0) effects+="\n"+item.duration+" SECONDS";
            description.text=item.itemName+"\n\n"+item.itemDescription+effects+"\n\nSTACK "+chosen.quantity+" / "+Mathf.Max(1,item.stackSize);
            description.enableAutoSizing=true; description.fontSizeMin=16; description.fontSizeMax=24;
        }
        else description.text="SELECT AN ITEM\n\nDOUBLE CLICK TO USE\nRIGHT CLICK TO DROP 1";
        useButton.interactable=IsUsable(item); dropButton.interactable=item != null;
        var s=StatsManager.Instance; var exp=FindObjectOfType<ExpManager>();
        if(s != null) stats.text="Lv."+(exp != null ? exp.level : 1)+"\nHP "+s.currentHealth+" / "+s.maxHealth+"\nDAMAGE "+s.damage+"   SPEED "+s.speed+"\nGOLD "+inventory.gold;
    }

    private void UpdatePortrait()
    {
        if(player == null)
        {
            player=FindObjectOfType<PlayerMovement>(); if(player == null) return;
            playerSprites=player.GetComponentsInChildren<SpriteRenderer>(true);
            System.Array.Sort(playerSprites,(a,b)=>a.sortingOrder.CompareTo(b.sortingOrder));
            foreach(var image in portraitLayers) if(image != null) Destroy(image.gameObject);
            portraitLayers.Clear();
            foreach(var sprite in playerSprites)
                portraitLayers.Add(Art("Player "+sprite.name,portraitRoot,null,Vector2.zero,Vector2.one));
        }
        Bounds bounds=new Bounds(); bool found=false;
        foreach(var source in playerSprites)
        {
            if(source == null || !source.enabled || !source.gameObject.activeInHierarchy || source.sprite == null) continue;
            if(!found) { bounds=source.bounds; found=true; } else bounds.Encapsulate(source.bounds);
        }
        if(!found) return;
        float scale=2.3f*Mathf.Min(220/Mathf.Max(0.1f,bounds.size.x),270/Mathf.Max(0.1f,bounds.size.y));
        for(int i=0;i<playerSprites.Length;i++)
        {
            var source=playerSprites[i]; var image=portraitLayers[i];
            bool visible=source != null && source.enabled && source.gameObject.activeInHierarchy && source.sprite != null;
            image.gameObject.SetActive(visible); if(!visible) continue;
            image.sprite=source.sprite; image.color=source.color;
            image.rectTransform.sizeDelta=new Vector2(source.bounds.size.x,source.bounds.size.y)*scale;
            image.rectTransform.anchoredPosition=(Vector2)(source.bounds.center-bounds.center)*scale;
            image.rectTransform.localScale=new Vector3((source.flipX ^ (source.transform.lossyScale.x<0))?-1:1,source.flipY?-1:1,1);
        }
    }
}

public class BackpackCell : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    public BackpackUI owner;
    public int index;
    public Image background, icon;
    public TMP_Text quantity;
    public void OnPointerClick(PointerEventData e) { if(!e.dragging) owner.Select(index,e.button,e.clickCount); }
    public void OnBeginDrag(PointerEventData e) { owner.BeginDrag(index,e); }
    public void OnDrag(PointerEventData e) { owner.Drag(e); }
    public void OnEndDrag(PointerEventData e) { owner.EndDrag(); }
    public void OnDrop(PointerEventData e) { owner.Drop(index); }
}
