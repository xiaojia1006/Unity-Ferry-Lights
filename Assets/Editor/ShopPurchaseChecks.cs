using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class ShopPurchaseChecks
{
    private const string Pending = "RPG.ShopChecks.Pending";
    private static double readyAt;
    static ShopPurchaseChecks()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state != PlayModeStateChange.EnteredPlayMode || !SessionState.GetBool(Pending,false)) return;
            SessionState.SetBool(Pending,false);
            readyAt = EditorApplication.timeSinceStartup + 1;
            EditorApplication.update += Wait;
        };
    }
    [MenuItem("Tools/RPG/Check Shop Purchases %#F8")]
    public static void Start()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play mode before running isolated shop checks."); return; }
        SessionState.SetBool(Pending,true);
        EditorApplication.EnterPlaymode();
    }
    private static void Wait()
    {
        if (EditorApplication.timeSinceStartup < readyAt) return;
        EditorApplication.update -= Wait;
        Run();
    }
    private static void Run()
    {
        var lines = new List<string>();
        try
        {
            var inventory = InventortyManager.Instance;
            var keeper = UnityEngine.Object.FindObjectsOfType<ShopKeeper>().First(k => k.shopManager != null);
            typeof(ShopKeeper).GetMethod("OpenShop",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(keeper,null);
            foreach (var entry in inventory.itemSlots) { entry.itemSO=null; entry.quantity=0; entry.UpdateUI(); }
            inventory.gold = 100;
            Canvas.ForceUpdateCanvases();
            var slots = keeper.shopManager.GetComponentsInChildren<ShopSlot>().Where(s => s.itemSO != null).ToArray();
            Check(slots.Length >= 2,"Shop contains mushroom and pumpkin",lines);
            foreach (var slot in slots)
            {
                var button = slot.GetComponent<Button>();
                lines.Add(slot.itemSO.name+" button="+button+" interactable="+(button != null && button.IsInteractable()));
                if (button != null)
                    for (int i=0;i<button.onClick.GetPersistentEventCount();i++)
                        lines.Add("Binding: "+button.onClick.GetPersistentTarget(i)+" / "+button.onClick.GetPersistentMethodName(i));
                var rect = slot.GetComponent<RectTransform>();
                var canvas = slot.GetComponentInParent<Canvas>();
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                foreach (float x in new[] {-0.3f,0f,0.3f})
                {
                    var pointer = new PointerEventData(EventSystem.current) { button=PointerEventData.InputButton.Left,
                        position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center+new Vector2(rect.rect.width*x,0))) };
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointer,hits);
                    lines.Add(slot.itemSO.name+" x="+x+" hit="+(hits.Count>0 ? PathOf(hits[0].gameObject.transform) : "none"));
                    int gold=inventory.gold, quantity=inventory.GetItemQuantity(slot.itemSO);
                    if(hits.Count>0) ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
                    Check(inventory.gold==gold-slot.price && inventory.GetItemQuantity(slot.itemSO)==quantity+1,
                        slot.itemSO.name+" click x="+x+" buys exactly one and deducts "+slot.price,lines);
                }
            }
            var mushroom = AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Mushroom.asset");
            var mushroomSlot = slots.First(s=>s.itemSO==mushroom);
            int before = inventory.gold;
            inventory.gold=0;
            int count=inventory.GetItemQuantity(mushroom);
            mushroomSlot.OnBuyButtonClicked();
            Check(inventory.gold==0 && inventory.GetItemQuantity(mushroom)==count,"Insufficient gold leaves money and items unchanged",lines);
            inventory.gold=before;
            foreach(var entry in inventory.itemSlots) { entry.itemSO=mushroom; entry.quantity=mushroom.stackSize; entry.UpdateUI(); }
            mushroomSlot.OnBuyButtonClicked();
            Check(inventory.gold==before,"Full inventory does not charge gold",lines);
            inventory.itemSlots[0].quantity--;
            mushroomSlot.OnBuyButtonClicked();
            Check(inventory.gold==before-mushroomSlot.price && inventory.itemSlots[0].quantity==mushroom.stackSize,
                "Purchase stacks into available mushroom stack",lines);
        }
        catch(Exception error) { lines.Add("FAIL: "+error); }
        finally
        {
            Directory.CreateDirectory("Logs");
            File.WriteAllLines("Logs/ShopPurchaseChecks.txt",lines);
            EditorApplication.ExitPlaymode();
        }
    }
    private static string PathOf(Transform t) => t.parent == null ? t.name : PathOf(t.parent)+"/"+t.name;
    private static void Check(bool ok,string message,List<string> lines) => lines.Add((ok?"PASS: ":"FAIL: ")+message);
}
