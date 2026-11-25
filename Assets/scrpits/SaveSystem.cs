using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

// Versioned per-account snapshot. Restoration runs after scene Start initialization.
[DefaultExecutionOrder(1000)]
public sealed class SaveSystem : MonoBehaviour
{
    [Serializable] public class Slot {public string id;public int quantity;}
    [Serializable] public class Member {public string name,value;}
    [Serializable] public class State {public string id,type;public List<Member> members=new List<Member>();}
    [Serializable] public class Drop {public string id;public int quantity;public float x,y;}
    [Serializable] public class World {public List<Drop> drops=new List<Drop>();public string scene;public List<State> states=new List<State>();public float x,y;public int phase;public float phaseSeconds;}
    [Serializable] public class Data
    {
        public int version=1;public string account,scene;public int hp,maxHp,damage,speed,gold,level,xp,nextXp;public float range,knockback,stamina;
        public List<Slot> slots=new List<Slot>();public List<World> worlds=new List<World>();
    }
    public static SaveSystem Instance {get;private set;}
    Data data;string owner;bool ready;SaveItemCatalog catalog;float timer;
    const BindingFlags Fields=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
    static readonly Dictionary<Type,string[]> SavedFields=new Dictionary<Type,string[]>{
        {typeof(RPG2Adventure),new[]{"accepted","beaconSequence","<SuppliesDelivered>k__BackingField","<BridgeRepaired>k__BackingField","<BeaconCount>k__BackingField","<TreasureClaimed>k__BackingField","<SheepRescued>k__BackingField","dayIndex","fishToday","questsToday","goldAtDayStart"}},
        {typeof(RPG1Frontier),new[]{"west","east","won","paid"}},
        {typeof(WillowreachNode),new[]{"completed"}},
        {typeof(WillowreachAdventure),new[]{"deliveries","puzzleSolved","cargoSeals","ready","integrity"}},
        {typeof(WillowreachPastoral),new[]{"fluteCrafted"}},
        {typeof(TreasureChest),new[]{"isOpen"}},
        {typeof(WillowreachKitchen),new[]{"successes"}}
    };
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void Reset(){Instance=null;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)] static void Boot(){if(Instance==null)new GameObject("Account Save System").AddComponent<SaveSystem>();}
    void Awake(){if(Instance!=null){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);catalog=Resources.Load<SaveItemCatalog>("SaveItemCatalog");SceneManager.sceneLoaded+=Loaded;}
    public static string FilePath(string account)
    {using(var hash=SHA256.Create()){return Path.Combine(Application.persistentDataPath,"Saves",BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(account??"guest"))).Replace("-","").ToLowerInvariant()+".json");}}
    static Data Read(string account)
    {
        string path=FilePath(account);
        foreach(string candidate in new[]{path,path+".bak"})try{if(File.Exists(candidate)){var d=JsonUtility.FromJson<Data>(File.ReadAllText(candidate));if(d!=null&&d.version==1&&d.account==account&&d.worlds!=null&&d.slots!=null)return d;}}catch(Exception e){Debug.LogWarning("Save read: "+e.Message);}
        return null;
    }
    public static string ResumeScene(string fallback){var d=Read(LoginSession.Account);return d!=null&&d.scene!="Login"&&Application.CanStreamedLevelBeLoaded(d.scene)?d.scene:fallback;}
    void Loaded(Scene scene,LoadSceneMode mode){ready=false;StopAllCoroutines();if(scene.name=="Login"){data=null;owner=null;return;}StartCoroutine(Restore(scene));}
    IEnumerator Restore(Scene scene)
    {
        yield return null;yield return null;
        string account=LoginSession.Account??"guest";
        if(data==null||owner!=account){owner=account;data=Read(owner);typeof(RPG1Frontier).GetMethod("Reset",Fields).Invoke(null,null);}
        if(data==null)data=new Data{account=owner};
        foreach(var health in Components(scene).OfType<Enemy_Health>())health.preserveForRetry=true;
        var world=data.worlds.FirstOrDefault(w=>w.scene==scene.name);
        if(world!=null)
        {
            foreach(var c in Components(scene))
            {
                var saved=world.states.FirstOrDefault(s=>s.id==StableId(c.transform)&&s.type==c.GetType().Name);
                if(saved==null)continue;
                if(SavedFields.ContainsKey(c.GetType()))foreach(var member in saved.members)Set(c,member);
                RestoreSpecial(c,saved);
            }
            if(catalog!=null&&catalog.lootPrefab!=null&&world.drops!=null)
            {
                foreach(var root in scene.GetRootGameObjects())foreach(var loot in root.GetComponentsInChildren<Loot>(true)){loot.gameObject.SetActive(false);Destroy(loot.gameObject);}
                foreach(var drop in world.drops){var item=catalog.Find(drop.id);if(item==null||drop.quantity<=0)continue;var loot=Instantiate(catalog.lootPrefab,new Vector3(drop.x,drop.y,0),Quaternion.identity);loot.Initialize(item,drop.quantity);}
            }
            RestorePlayer(world);
            var n=DayNight2D.Instance;if(n!=null){n.SetPhase((DayNight2D.Phase)Mathf.Clamp(world.phase,0,3));typeof(DayNight2D).GetProperty("PhaseSeconds").GetSetMethod(true).Invoke(n,new object[]{world.phaseSeconds});n.Advance(0);}
        }
        else if(data.maxHp>0)RestorePlayer(null);
        foreach(var panel in FindObjectsOfType<StatsUI>())panel.UpdataAllStats();
        ready=true;timer=0;
        var main=FindObjectOfType<RPG2Adventure>();if(main!=null&&main.TreasureClaimed)main.Notify("本章完成 · 苇岸已安定，可继续探索或前往柳湾。进度已保存。");
    }
    static IEnumerable<Component> Components(Scene scene)
    {
        foreach(var root in scene.GetRootGameObjects())foreach(var c in root.GetComponentsInChildren<MonoBehaviour>(true))
            if(c!=null&&(SavedFields.ContainsKey(c.GetType())||c is RPG2Point||c is Enemy_Health||c is QuestManager))yield return c;
    }
    public static string StableId(Transform transform)
    {var identity=transform.GetComponent<SaveIdentity>();if(identity!=null&&!string.IsNullOrEmpty(identity.id))return identity.id;var parts=new List<string>();for(var t=transform;t!=null;t=t.parent)parts.Add(t.name+"["+t.GetSiblingIndex()+"]");parts.Reverse();return string.Join("/",parts);}
    static Member Capture(object c,string name)
    {
        var f=c.GetType().GetField(name,Fields);if(f==null)return null;object v=f.GetValue(c);
        if(v is bool[])return new Member{name=name,value=string.Join(",",((bool[])v).Select(b=>b?"1":"0"))};
        return new Member{name=name,value=Convert.ToString(v,System.Globalization.CultureInfo.InvariantCulture)};
    }
    static void Set(object c,Member m)
    {
        var f=c.GetType().GetField(m.name,Fields);if(f==null)return;
        try{if(f.FieldType==typeof(bool[])){var a=(bool[])f.GetValue(c);var b=m.value.Split(',');for(int i=0;i<a.Length&&i<b.Length;i++)a[i]=b[i]=="1";}else f.SetValue(c,Convert.ChangeType(m.value,f.FieldType,System.Globalization.CultureInfo.InvariantCulture));}catch(Exception e){Debug.LogWarning("Save field: "+e.Message);}
    }
    void RestoreSpecial(Component c,State state)
    {
        string value=state.members.FirstOrDefault(m=>m.name=="special")?.value;
        if(value==null)return;
        var qm=c as QuestManager;
        if(qm!=null&&catalog!=null&&catalog.quests!=null)
        {
            foreach(var entry in value.Split(';')){var pair=entry.Split(':');if(pair.Length!=2)continue;int index=Array.IndexOf(catalog.questIds,pair[0]);if(index<0)continue;var quest=catalog.quests[index];
                if(pair[1]=="done")((List<QuestSo>)typeof(QuestManager).GetField("completedQuests",Fields).GetValue(qm)).Add(quest);else qm.AcceptQuest(quest);}
        }
        var point=c as RPG2Point;if(point!=null)
        {
            var bits=value.Split(',');point.SetLit(bits[0]=="1");if(bits.Length>1&&bits[1]=="1")
            {var w=point.GetComponentInParent<RPG2Adventure>();if(w==null)w=FindObjectOfType<RPG2Adventure>();if(w!=null){var set=(HashSet<RPG2Point>)typeof(RPG2Adventure).GetField("collected",Fields).GetValue(w);set.Add(point);}var sr=point.GetComponent<SpriteRenderer>();if(sr!=null)sr.enabled=false;}
        }
        var enemy=c as Enemy_Health;if(enemy!=null&&int.TryParse(value,out int hp)){enemy.currenetHealth=Mathf.Clamp(hp,0,enemy.maxHealth);if(enemy.currenetHealth==0)enemy.gameObject.SetActive(false);}
        var adventure=c as RPG2Adventure;if(adventure!=null){adventure.bridge.SetActive(adventure.BridgeRepaired);adventure.SendMessage("RefreshQuest",SendMessageOptions.DontRequireReceiver);}
        var chest=c as TreasureChest;if(chest!=null&&chest.isOpen){var animator=chest.GetComponent<Animator>();if(animator!=null){animator.Play("Chest_Open",0,1);animator.Update(0);}}
        var frontier=c as RPG1Frontier;if(frontier!=null){frontier.ApplyState();if(frontier.finalChest!=null)frontier.finalChest.gameObject.SetActive(frontier.Completed);if(frontier.Completed)foreach(var guard in frontier.arena)if(guard!=null)guard.gameObject.SetActive(false);}
        var node=c as WillowreachNode;if(node!=null&&node.completed){if(node.unlock!=null)node.unlock.SetActive(false);if(node.activate!=null)node.activate.SetActive(true);if(node.indicator!=null){if(node.kind==WillowreachNode.Kind.Supply)node.indicator.enabled=false;else node.indicator.color=new Color(.65f,1,.75f);}}
    }
    void RestorePlayer(World world)
    {
        if(data.maxHp>0)foreach(var effects in FindObjectsOfType<Useltem>())effects.ClearTemporaryEffects();
        var stats=StatsManager.Instance;if(stats!=null&&data.maxHp>0){stats.maxHealth=data.maxHp;stats.currentHealth=Mathf.Clamp(data.hp,1,data.maxHp);stats.damage=data.damage;stats.speed=data.speed;stats.weaponRange=data.range;stats.knockbackForce=data.knockback;stats.UpdateHealthText();PlayerStats.ResetStamina();PlayerStats.DrainStamina(PlayerStats.StaminaMax-Mathf.Clamp(data.stamina,0,PlayerStats.StaminaMax));}
        var inventory=InventortyManager.Instance;if(inventory!=null&&catalog!=null&&data.maxHp>0)
        {
            inventory.gold=data.gold;if(inventory.goldText!=null)inventory.goldText.text=data.gold.ToString();
            for(int i=0;i<inventory.itemSlots.Length;i++){var slot=inventory.itemSlots[i];if(slot==null)continue;slot.itemSO=i<data.slots.Count?catalog.Find(data.slots[i].id):null;slot.quantity=slot.itemSO!=null?Mathf.Clamp(data.slots[i].quantity,0,Mathf.Max(1,slot.itemSO.stackSize)):0;slot.UpdateUI();}
        }
        var exp=FindObjectOfType<ExpManager>();if(exp!=null&&data.maxHp>0){exp.level=Mathf.Max(1,data.level);exp.currentExp=Mathf.Max(0,data.xp);exp.expToLevel=Mathf.Max(1,data.nextXp);exp.UpdataUI();}
        if(world!=null){var p=FindObjectOfType<PlayerMovement>();if(p!=null){p.transform.position=new Vector3(world.x,world.y,0);p.rb.position=p.transform.position;p.rb.velocity=Vector2.zero;}}
    }
    public static bool IsReady => Instance!=null&&Instance.ready;
    public static void ReloadFromDisk(){if(Instance==null)return;Instance.ready=false;Instance.data=null;SceneManager.LoadScene(SceneManager.GetActiveScene().name);}
    bool checkpoint;
    public static void Checkpoint(){if(Instance!=null&&Instance.ready)Instance.checkpoint=true;}
    void LateUpdate(){if(!checkpoint)return;checkpoint=false;SaveNow();}
    void Update(){if(!ready)return;timer+=Time.unscaledDeltaTime;if(timer>=2){timer=0;SaveNow();}}
    public static void SaveNow(){if(Instance!=null)Instance.Write();}
    void Write()
    {
        if(!ready||data==null||SceneManager.GetActiveScene().name=="Login")return;
        try
        {
            var scene=SceneManager.GetActiveScene();var world=new World{scene=scene.name};var p=FindObjectOfType<PlayerMovement>();if(p!=null){world.x=p.transform.position.x;world.y=p.transform.position.y;}
            var night=DayNight2D.Instance;if(night!=null){world.phase=(int)night.Current;world.phaseSeconds=night.PhaseSeconds;}
            foreach(var c in Components(scene))
            {
                var state=new State{id=StableId(c.transform),type=c.GetType().Name};
                if(SavedFields.TryGetValue(c.GetType(),out var fields))foreach(var field in fields){var m=Capture(c,field);if(m!=null)state.members.Add(m);}
                string special="";var qm=c as QuestManager;
                if(qm!=null&&catalog!=null&&catalog.quests!=null){var done=(List<QuestSo>)typeof(QuestManager).GetField("completedQuests",Fields).GetValue(qm);var entries=new List<string>();for(int i=0;i<catalog.quests.Length;i++)if(done.Contains(catalog.quests[i])||qm.IsQuestAccepted(catalog.quests[i]))entries.Add(catalog.questIds[i]+":"+(done.Contains(catalog.quests[i])?"done":"active"));special=string.Join(";",entries);}
                var point=c as RPG2Point;if(point!=null){var w=FindObjectOfType<RPG2Adventure>();var collected=w!=null?(HashSet<RPG2Point>)typeof(RPG2Adventure).GetField("collected",Fields).GetValue(w):null;special=(point.glow!=null&&point.glow.activeSelf?"1":"0")+","+(collected!=null&&collected.Contains(point)?"1":"0");}
                var enemy=c as Enemy_Health;if(enemy!=null)special=enemy.currenetHealth.ToString();
                state.members.Add(new Member{name="special",value=special});world.states.Add(state);
            }
            foreach(var root in scene.GetRootGameObjects())foreach(var drop in root.GetComponentsInChildren<Loot>())if(drop.HasUnclaimedContents&&catalog!=null)world.drops.Add(new Drop{id=catalog.Id(drop.itemSO),quantity=drop.quantity,x=drop.transform.position.x,y=drop.transform.position.y});
            data.worlds.RemoveAll(w=>w.scene==scene.name);data.worlds.Add(world);data.scene=scene.name;
            var stats=StatsManager.Instance;if(stats!=null){data.hp=stats.currentHealth;data.maxHp=stats.maxHealth;data.damage=stats.damage;data.speed=stats.speed;data.range=stats.weaponRange;data.knockback=stats.knockbackForce;data.stamina=PlayerStats.Stamina;foreach(var effects in FindObjectsOfType<Useltem>())effects.RemoveTemporaryFromSnapshot(data);}
            var inv=InventortyManager.Instance;if(inv!=null){data.gold=inv.gold;data.slots.Clear();foreach(var slot in inv.itemSlots)data.slots.Add(new Slot{id=slot!=null&&catalog!=null?catalog.Id(slot.itemSO):"",quantity=slot!=null?slot.quantity:0});}
            var exp=FindObjectOfType<ExpManager>();if(exp!=null){data.level=exp.level;data.xp=exp.currentExp;data.nextXp=exp.expToLevel;}
            string path=FilePath(owner);Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllText(path+".tmp",JsonUtility.ToJson(data,true));if(File.Exists(path))File.Replace(path+".tmp",path,path+".bak");else File.Move(path+".tmp",path);
        }catch(Exception e){Debug.LogError("Save failed: "+e.Message);}
    }
    void OnApplicationPause(bool paused){if(paused)SaveNow();}
    void OnApplicationQuit(){SaveNow();}
    void OnDestroy(){SceneManager.sceneLoaded-=Loaded;if(Instance==this)Instance=null;}
}
