using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class BackpackChecks
{
    [MenuItem("Tools/RPG/Check Backpack In Play Mode %#F5")]
    public static void Run()
    {
        if (!EditorApplication.isPlaying) { Debug.LogWarning("Run backpack checks in Play mode."); return; }
        var report=new List<string>();
        var inv=InventortyManager.Instance;
        if(inv == null) return;
        var ui=inv.GetComponent<BackpackUI>();
        if (!ui.IsOpen)
        {
            ui.SetOpen(true);
            EditorApplication.delayCall += Run;
            return;
        }
        var items=new ItemSO[inv.itemSlots.Length]; var counts=new int[items.Length];
        for(int i=0;i<items.Length;i++) { items[i]=inv.itemSlots[i].itemSO; counts[i]=inv.itemSlots[i].quantity; }
        var pumpkin=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/pumpkin.asset");
        var mushroom=AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Mushroom.asset");
        var healingFood=UnityEngine.Object.Instantiate(mushroom);
        healingFood.currentHealth=3;
        var stats=StatsManager.Instance;
        int oldHealth=stats.currentHealth, oldGold=inv.gold;
        var oldLoot=new HashSet<Loot>(UnityEngine.Object.FindObjectsOfType<Loot>());
        try
        {
            Check(items.Length==30,"30 shared slots available",report);
            Clear(inv);
            inv.AddItem(mushroom,70);
            Check(inv.GetItemQuantity(mushroom)==70 && inv.itemSlots[0].quantity==64 && inv.itemSlots[1].quantity==6,"Stack cap and overflow retain every item",report);
            Check(inv.MoveItem(0,5,true) && inv.itemSlots[0].quantity==32 && inv.itemSlots[5].quantity==32,"Split stack in half",report);
            Check(inv.MoveItem(1,5,false) && inv.itemSlots[5].quantity==38 && inv.itemSlots[1].itemSO==null,"Merge and clear empty source",report);
            inv.itemSlots[6].itemSO=pumpkin; inv.itemSlots[6].quantity=3;
            Check(inv.MoveItem(5,6,false) && inv.itemSlots[5].itemSO==pumpkin && inv.itemSlots[6].itemSO==mushroom,"Different items swap",report);
            Check(!inv.MoveItem(5,6,true) && inv.GetItemQuantity(pumpkin)==3,"Split rejects occupied different item",report);
            int total=inv.GetItemQuantity(mushroom);
            inv.SortBackpack();
            Check(inv.GetItemQuantity(mushroom)==total && inv.GetItemQuantity(pumpkin)==3 && inv.itemSlots[0].quantity==32,"Sort conserves quantities and quickbar",report);
            Clear(inv);
            foreach(var slot in inv.itemSlots) { slot.itemSO=mushroom; slot.quantity=64; }
            Check(!inv.CanAddItems(new Dictionary<ItemSO,int>{{pumpkin,1}}) && inv.AddItem(pumpkin,1)==1,"Full bag rejects reward without destroying overflow",report);
            Clear(inv); inv.AddItem(healingFood,3); stats.currentHealth=stats.maxHealth;
            inv.UseItem(inv.itemSlots[0]);
            Check(inv.GetItemQuantity(healingFood)==3,"Full health does not consume healing food",report);
            stats.currentHealth=Mathf.Max(1,stats.maxHealth-10);
            inv.UseItem(inv.itemSlots[0]);
            Check(inv.GetItemQuantity(healingFood)==2 && stats.currentHealth>stats.maxHealth-10,"Use food heals and consumes one",report);
            inv.DropLoot(inv.itemSlots[0]);
            Check(inv.GetItemQuantity(healingFood)==1 && UnityEngine.Object.FindObjectsOfType<Loot>().Length>oldLoot.Count,"Drop creates world loot and consumes one",report);
            inv.AddItem(pumpkin,4);
            ui.SetOpen(true); Canvas.ForceUpdateCanvases();
            var cells=ui.GetComponentsInChildren<BackpackCell>(true);
            // Canvas is a child of the shared inventory, not the UI component's own transform subtree distinction.
            Check(cells.Length==30 && ui.IsOpen && Time.timeScale==0f,"Backpack opens and pauses world",report);
            var cell=cells[0]; var position=RectTransformUtility.WorldToScreenPoint(null,cell.transform.position);
            var pointer=new PointerEventData(EventSystem.current) {position=position,button=PointerEventData.InputButton.Left,clickCount=1};
            var hits=new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer,hits);
            report.Add("Raycast position: "+position+"; screen "+Screen.width+"x"+Screen.height);
            foreach(var hit in hits) report.Add("Raycast hit: "+hit.gameObject.name+" depth "+hit.depth+" sorting "+hit.sortingOrder);
            Check(hits.Count>0 && hits[0].gameObject==cell.gameObject,"Item receives mouse raycast above decorative art",report);
            ExecuteEvents.Execute(cell.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Check(ui.SelectedIndex==0,"Pointer click selects correct shared slot",report);
            int sourceCount=inv.itemSlots[0].quantity;
            ExecuteEvents.Execute(cell.gameObject,pointer,ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(cells[8].gameObject,pointer,ExecuteEvents.dropHandler);
            ExecuteEvents.Execute(cell.gameObject,pointer,ExecuteEvents.endDragHandler);
            Check(inv.itemSlots[0].itemSO==null && inv.itemSlots[8].quantity==sourceCount,
                "UI drag handlers move shared item into storage",report);
            ui.SetOpen(false);
            Check(!ui.IsOpen && Time.timeScale>0f,"Closing restores world time",report);
        }
        catch(Exception e) { report.Add("FAIL: "+e); }
        finally
        {
            ui.SetOpen(false);
            for(int i=0;i<items.Length;i++) { inv.itemSlots[i].itemSO=items[i]; inv.itemSlots[i].quantity=counts[i]; inv.itemSlots[i].UpdateUI(); }
            stats.currentHealth=oldHealth; stats.UpdateHealthText(); inv.gold=oldGold;
            foreach(var loot in UnityEngine.Object.FindObjectsOfType<Loot>()) if(!oldLoot.Contains(loot)) UnityEngine.Object.Destroy(loot.gameObject);
            UnityEngine.Object.Destroy(healingFood);
            Directory.CreateDirectory("Logs"); File.WriteAllLines("Logs/BackpackChecks.txt",report);
            Debug.Log(string.Join("\n",report));
        }
    }

    [MenuItem("Tools/RPG/Preview Backpack In Play Mode")]
    public static void Preview()
    {
        if(!EditorApplication.isPlaying || InventortyManager.Instance==null) return;
        var inv=InventortyManager.Instance;
        inv.AddItem(AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/pumpkin.asset"),6);
        inv.AddItem(AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/Mushroom.asset"),20);
        inv.AddItem(AssetDatabase.LoadAssetAtPath<ItemSO>("Assets/scrpits/Inventory&Shop/ItemSOs/bone.asset"),8);
        inv.GetComponent<BackpackUI>().SetOpen(true);
    }

    private static void Clear(InventortyManager inv)
    {
        foreach(var slot in inv.itemSlots) { slot.itemSO=null; slot.quantity=0; slot.UpdateUI(); }
    }
    private static void Check(bool condition,string message,List<string> report)
    {
        if(!condition) throw new InvalidOperationException(message);
        report.Add("PASS: "+message);
    }
}
