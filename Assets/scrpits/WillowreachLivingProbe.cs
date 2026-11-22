#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class WillowreachLivingProbe : MonoBehaviour
{
    [MenuItem("Tools/RPG/Scene Expansion/Check Living Gameplay In Play")]
    public static void Run()
    {
        if(!EditorApplication.isPlaying)return;
        var world=FindObjectOfType<WillowreachWorld>();if(world==null||world.GetComponent<WillowreachPastoral>()==null)return;
        new GameObject("Living world validation").AddComponent<WillowreachLivingProbe>().StartCoroutineRoutine();
    }
    private void StartCoroutineRoutine() => StartCoroutine(Validate());
    private IEnumerator Validate()
    {
        var lines=new List<string>();Action<bool,string> check=(ok,name)=>lines.Add((ok?"PASS ":"FAIL ")+name);
        var world=FindObjectOfType<WillowreachWorld>();var game=world.GetComponent<WillowreachPastoral>();var nav=world.GetComponent<WillowreachNavigation>();
        var inv=InventortyManager.Instance;var slots=inv.itemSlots;
        var items=slots.Select(s=>s.itemSO).ToArray();var quantities=slots.Select(s=>s.quantity).ToArray();int gold=inv.gold;
        bool flute=game.fluteCrafted;var done=game.pastures.Select(p=>p.completed).ToArray();var colors=game.pastures.Select(p=>p.indicator.color).ToArray();
        var positions=game.sheep.Select(s=>s.transform.position).ToArray();var following=game.sheep.Select(s=>s.following).ToArray();
        var playerPosition=world.player.position;var weather=world.weather.currentWeather;float scale=Time.timeScale;
        GameObject pickup=null;
        try
        {
            foreach(var slot in slots){slot.itemSO=null;slot.quantity=0;slot.UpdateUI();}
            game.fluteCrafted=false;
            check(!game.Craft()&&!game.fluteCrafted,"Insufficient bones cannot craft or consume items");
            inv.AddItem(game.bone,5);
            check(game.Craft()&&game.fluteCrafted&&inv.GetItemQuantity(game.bone)==2,"Flute crafting consumes exactly three bones");
            check(!game.Craft()&&inv.GetItemQuantity(game.bone)==2,"Flute cannot charge bones a second time");
            foreach(var pasture in game.pastures)pasture.completed=false;
            foreach(var s in game.sheep){s.Follow(false);s.transform.position=new Vector3(0,-20,0);}
            var first=game.pastures[0];world.weather.SetWeather(first.requiredWeather);
            check(!game.Claim(first)&&!first.completed&&inv.gold==gold,"Pasture cannot pay without a sheep nearby");
            for(int i=0;i<game.pastures.Length;i++)
            {
                var pasture=game.pastures[i];game.sheep[0].transform.position=pasture.transform.position;
                world.weather.SetWeather((WeatherCycle2D.Weather)(((int)pasture.requiredWeather+1)%3));
                check(!game.Claim(pasture)&&!pasture.completed,"Wrong weather retains pasture reward: "+pasture.title);
                world.weather.SetWeather(pasture.requiredWeather);int before=inv.gold;
                check(game.Claim(pasture)&&inv.gold==before+12+(i==2?15:0),"Sheep and matching weather grant bounded reward: "+pasture.title);
                int paid=inv.gold;check(!game.Claim(pasture)&&inv.gold==paid,"Pasture cannot pay twice: "+pasture.title);
            }
            check(inv.gold==gold+51,"Three pastures and final bonus total exactly 51 gold");

            var bone=world.GetComponentsInChildren<Loot>().First(l=>l.itemSO==game.bone);
            pickup=Instantiate(bone.gameObject,world.player.position,Quaternion.identity);var loot=pickup.GetComponent<Loot>();loot.itemSO=game.bone;loot.quantity=1;
            int beforeBones=inv.GetItemQuantity(game.bone);Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return null;
            check(inv.GetItemQuantity(game.bone)==beforeBones+1&&loot.quantity==0,"Walking over a bone invokes actual trigger pickup");
            Destroy(pickup);pickup=null;
            var food=world.nodes.First(n=>n.kind==WillowreachNode.Kind.Supply&&n.item!=null&&n.item.name=="pumpkin").item;
            foreach(var slot in slots){slot.itemSO=food;slot.quantity=food.stackSize;slot.UpdateUI();}
            pickup=Instantiate(bone.gameObject,world.player.position,Quaternion.identity);loot=pickup.GetComponent<Loot>();loot.itemSO=game.bone;loot.quantity=1;Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();yield return new WaitForFixedUpdate();yield return null;
            check(loot!=null&&loot.quantity==1&&inv.GetItemQuantity(game.bone)==0,"Full backpack retains bone and exact quantity on ground");
            Destroy(pickup);pickup=null;

            var tree=world.GetComponentsInChildren<WillowreachFlipbook>().First(t=>t.name=="Pine grove");
            var sprite=tree.GetComponent<SpriteRenderer>().sprite;var trunk=tree.GetComponent<BoxCollider2D>().bounds;
            yield return new WaitForSeconds(.23f);
            check(tree.GetComponent<SpriteRenderer>().sprite!=sprite&&tree.GetComponent<BoxCollider2D>().bounds==trunk,"Tree frame advances while trunk collision stays fixed");

            var sheep=game.sheep[0];sheep.Graze(0);sheep.transform.position=new Vector3(-8,1,0);
            world.player.position=new Vector3(-8,14,0);sheep.Follow(true);Physics2D.SyncTransforms();
            var firstSheepSprite=sheep.GetComponent<SpriteRenderer>().sprite;bool animated=false,safe=true,crossed=false;
            float deadline=Time.time+18;Time.timeScale=4;
            while(Time.time<deadline&&sheep.transform.position.y<12.3f)
            {
                safe &= nav.Safe(sheep.transform.position);
                crossed |= world.river.GetComponent<PolygonCollider2D>().OverlapPoint(sheep.transform.position);
                animated |= sheep.Moving&&sheep.GetComponent<SpriteRenderer>().sprite!=firstSheepSprite;
                yield return null;
            }
            check(safe&&crossed&&sheep.transform.position.y>=12.3f,"Live sheep crosses river on bridge without entering water or rails");
            check(animated,"Moving sheep actually advances walking animation");
            sheep.Follow(false);check(!sheep.following,"Whistle release returns sheep to roaming");
        }
        finally
        {
            if(pickup!=null)Destroy(pickup);
            for(int i=0;i<slots.Length;i++){slots[i].itemSO=items[i];slots[i].quantity=quantities[i];slots[i].UpdateUI();}
            inv.gold=gold;if(inv.goldText!=null)inv.goldText.text=gold.ToString();
            game.fluteCrafted=flute;
            for(int i=0;i<game.pastures.Length;i++){game.pastures[i].completed=done[i];game.pastures[i].indicator.color=colors[i];}
            for(int i=0;i<game.sheep.Length;i++){game.sheep[i].transform.position=positions[i];game.sheep[i].Follow(following[i]);}
            world.player.position=playerPosition;world.weather.SetWeather(weather);Time.timeScale=scale;
            Directory.CreateDirectory("Logs");File.WriteAllLines("Logs/WillowreachLivingPlay.txt",lines);Debug.Log(string.Join("\n",lines));Destroy(gameObject);
        }
    }
    [MenuItem("Tools/RPG/Scene Expansion/Preview Living Sheep In Play")]
    public static void PreviewSheep()
    {var world=FindObjectOfType<WillowreachWorld>();if(EditorApplication.isPlaying&&world!=null){world.player.position=new Vector3(-16,-19,0);world.weather.SetWeather(WeatherCycle2D.Weather.Sunny);}}
    [MenuItem("Tools/RPG/Scene Expansion/Preview Bone Pickup In Play")]
    public static void PreviewBone()
    {var world=FindObjectOfType<WillowreachWorld>();if(!EditorApplication.isPlaying||world==null)return;var bone=world.GetComponentsInChildren<Loot>().FirstOrDefault();if(bone!=null)world.player.position=bone.transform.position+Vector3.down*1.5f;}
}

#endif

