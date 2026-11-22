using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;

public static class RegressionChecks
{
    private static int checks;
    private static void Check(bool condition, string name)
    {
        if (!condition) throw new Exception(name);
        Console.WriteLine("PASS: " + name);
        checks++;
    }
    private static void Set(object obj,string field,object value) => obj.GetType().GetField(field,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(obj,value);
    private static void Call(object obj,string method) => obj.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(obj,null);
    private static InventorySlot Slot(ItemSO item=null,int quantity=0) => new InventorySlot
    {
        itemSO=item,quantity=quantity,itemImage=new UnityEngine.UI.Image(),quantityText=new TMPro.TMP_Text()
    };
    public static void Main()
    {
        var herb = new ItemSO { stackSize=5 };
        var reward = new ItemSO { stackSize=2 };
        var other = new ItemSO { stackSize=2 };
        var inventory = new InventortyManager { itemSlots=new[] { Slot(herb,3), Slot() } };
        Call(inventory,"Awake");
        Check(InventortyManager.Instance==inventory,"Inventory singleton initializes");
        var objective = new QuestObjective { requiredAmount=3 };
        Set(objective,"target",herb);
        var quest = new QuestSo { objectives=new List<QuestObjective>{objective}, rewards=new List<QuestReward>{new QuestReward{itemSO=reward,quantity=2}} };
        var manager = new QuestManager();
        Check(!manager.TryCompleteQuest(quest),"Unaccepted quest cannot grant rewards");
        manager.AcceptQuest(quest);
        Check(manager.IsQuestComplete(quest),"Collection objective reads actual inventory");
        Check(manager.TryCompleteQuest(quest),"Complete quest grants rewards");
        Check(inventory.GetItemQuantity(reward)==2 && !manager.IsQuestAccepted(quest) && manager.GetCompleteQuests(quest),"Rewards and completed state agree");
        Check(!manager.TryCompleteQuest(quest) && inventory.GetItemQuantity(reward)==2,"Repeated claim cannot duplicate rewards");
        manager.AcceptQuest(quest);
        Check(!manager.IsQuestAccepted(quest),"Completed quest cannot be accepted again");

        quest = new QuestSo { objectives=new List<QuestObjective>{objective}, rewards=new List<QuestReward>{new QuestReward{itemSO=other,quantity=1}} };
        manager.AcceptQuest(quest);
        Check(!manager.TryCompleteQuest(quest) && manager.IsQuestAccepted(quest) && !manager.GetCompleteQuests(quest),"Full inventory keeps quest claimable without partial rewards");
        inventory.itemSlots[1]=Slot();
        Check(manager.TryCompleteQuest(quest) && inventory.GetItemQuantity(other)==1,"Reward claim succeeds after freeing space");
        inventory.itemSlots[1]=Slot();
        Check(!inventory.CanAddItems(new Dictionary<ItemSO,int>{{other,1},{reward,1}}),"Different rewards cannot reserve the same empty slot");
        Check(inventory.CanAddItems(new Dictionary<ItemSO,int>{{herb,2},{reward,2}}),"Capacity includes existing partial stacks");

        var slot=inventory.itemSlots[0];
        Set(slot,"inventortyManager",inventory);
        var shop=new ShopManager();
        Set(shop,"inventortyManager",inventory);
        Set(shop,"shopSlots",new[]{new ShopSlot{itemSO=reward,price=4}});
        typeof(InventorySlot).GetField("activeShop",BindingFlags.Static|BindingFlags.NonPublic).SetValue(null,shop);
        slot.OnPointerClick(new PointerEventData{button=PointerEventData.InputButton.Left});
        Check(slot.quantity==3 && inventory.gold==0,"Rejected sale preserves item and gold");
        Set(shop,"shopSlots",new[]{new ShopSlot{itemSO=herb,price=4}});
        slot.OnPointerClick(new PointerEventData{button=PointerEventData.InputButton.Left});
        Check(slot.quantity==2 && inventory.gold==4,"Accepted sale removes exactly one item");
        inventory.DropLoot(slot);
        Check(slot.quantity==2,"Missing drop configuration cannot delete inventory items");

        inventory.useItem=new Useltem();
        StatsManager.Instance=null;
        inventory.UseItem(slot);
        Check(slot.quantity==2,"Missing stats cannot consume an item without an effect");
        var stats=new StatsManager { currentHealth=20,maxHealth=50,speed=5,damage=1 };
        StatsManager.Instance=stats;
        inventory.UseItem(slot);
        Check(slot.quantity==2,"Material with no usable effects is not consumed");
        var pumpkin=new ItemSO { currentHealth=3,speed=3,damage=1,duration=5 };
        slot.itemSO=pumpkin;
        inventory.UseItem(slot);
        Check(slot.quantity==1 && stats.currentHealth==23 && stats.speed==8 && stats.damage==2,"Pumpkin consumes one item and applies configured effects");
        stats.currentHealth=stats.maxHealth;
        inventory.UseItem(slot);
        Check(slot.quantity==0 && stats.speed==11 && stats.damage==3,"Pumpkin can grant buffs at full health");
        slot.itemSO=new ItemSO { currentHealth=3 }; slot.quantity=1;
        inventory.UseItem(slot);
        Check(slot.quantity==1,"Full health blocks heal-only consumption");
        slot.itemSO=herb;

        var first=new UnityEngine.Object(); var second=new UnityEngine.Object();
        Time.timeScale=0.5f;
        MenuPause.SetPaused(first,true); MenuPause.SetPaused(second,true); MenuPause.SetPaused(first,false);
        Check(Time.timeScale==0,"Closing one menu keeps another menu paused");
        MenuPause.SetPaused(second,false);
        Check(Time.timeScale==0.5f,"Last menu restores previous time scale");
        MenuPause.SetPaused(first,true); MenuPause.SetPaused(first,true); MenuPause.SetPaused(first,false);
        Check(Time.timeScale==0.5f,"Repeated pause request does not leak a pause");

        var exp=new ExpManager { level=1,expToLevel=10,expGrowMultiplier=1.2f,expSlider=new UnityEngine.UI.Slider(),currentLeveText=new TMPro.TMP_Text() };
        int points=0; ExpManager.OnLevelUP += amount=>points+=amount;
        exp.GainExperience(35);
        Check(exp.level==3 && exp.currentExp==13 && exp.expToLevel==14 && points==2,"Large experience grant processes all levels and point events");
        exp.expToLevel=0; exp.expGrowMultiplier=0; exp.GainExperience(1);
        Check(exp.expToLevel>=1,"Invalid experience threshold cannot cause an infinite loop");

        var ui=new QuestLogUI();
        var accept=new CanvasGroup(); var complete=new CanvasGroup();
        Set(ui,"questManager",manager); Set(ui,"acceptCanvasGroup",accept); Set(ui,"completeCanvasGroup",complete);
        Call(ui,"OnEnable");
        var newQuest=new QuestSo { objectives=new List<QuestObjective>{objective} };
        QuestEvents.OnQuestOfferRequested(newQuest);
        Check(accept.alpha==1 && complete.alpha==0,"Quest offer shows accept rather than complete");
        ui.OnAcceptQuestClicked();
        Check(manager.IsQuestAccepted(newQuest) && complete.alpha==0,"Incomplete quest cannot show claim button");
        slot.quantity=3;
        QuestEvents.OnQuestTurnInRequested(newQuest);
        Check(accept.alpha==0 && complete.alpha==1,"Turn-in event shows completed accepted quest");
        Call(ui,"OnDisable");
        Console.WriteLine("Passed " + checks + " isolated gameplay logic checks.");
    }
}
