using UnityEngine;
using System.Collections.Generic;

public static class ArrowCrafting
{
    public static ItemSO Ammo => Resources.Load<ItemSO>("Items/bone_arrow");
    public static ItemSO Bone => Resources.Load<ItemSO>("Items/arrow_bone");
    public static int Count => InventortyManager.Instance!=null?InventortyManager.Instance.GetItemQuantity(Ammo):0;
    public static bool IsMaterial(ItemSO item)=>item!=null&&Bone!=null&&item.InventoryMaterial==Bone.InventoryMaterial;
    public static bool SpendOne()
    {
        var inv=InventortyManager.Instance;if(inv==null||Ammo==null)return false;
        foreach(var slot in inv.itemSlots)if(slot!=null&&slot.itemSO==Ammo&&slot.quantity>0){slot.quantity--;if(slot.quantity==0)slot.itemSO=null;slot.UpdateUI();SaveSystem.Checkpoint();return true;}return false;
    }
    public static bool Craft()
    {
        var inv=InventortyManager.Instance;if(inv==null||Ammo==null||Bone==null)return false;
        var bone=System.Array.Find(inv.itemSlots,s=>s!=null&&IsMaterial(s.itemSO)&&s.quantity>0);
        if(bone==null){Notify("需要1个骨头才能制作5支骨箭。");return false;}
        // Capacity simulation includes the slot freed by consuming the last bone.
        var item=bone.itemSO;int quantity=bone.quantity;bone.quantity--;if(bone.quantity==0)bone.itemSO=null;
        bool fits=inv.CanAddItems(new Dictionary<ItemSO,int>{{Ammo,5}});
        if(!fits){bone.itemSO=item;bone.quantity=quantity;Notify("背包空间不足，骨头未消耗。");return false;}
        bone.UpdateUI();inv.AddItem(Ammo,5);SaveSystem.Checkpoint();Notify("制作完成：骨头1 → 骨箭5，现有箭矢 "+Count+" 支。");return true;
    }
    public static void Notify(string text)
    {
        var rpg2=Object.FindObjectOfType<RPG2Adventure>();if(rpg2!=null){rpg2.Notify(text);return;}
        var rpg1=Object.FindObjectOfType<RPG1Activities>();if(rpg1!=null){rpg1.Notify(text);return;}
        var willow=Object.FindObjectOfType<WillowreachWorld>();if(willow!=null)willow.Notify(text);
    }
}
