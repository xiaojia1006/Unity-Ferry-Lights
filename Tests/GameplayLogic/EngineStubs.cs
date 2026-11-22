// Minimal engine substitutes for isolated logic tests; never included in Unity Assets.
using System;
using System.Collections;
namespace UnityEngine
{
    public class Object { public static void Destroy(Object o) {} }
    public class ScriptableObject : Object {}
    public class MonoBehaviour : Object
    {
        public bool enabled = true;
        public bool isActiveAndEnabled => enabled;
        public Transform transform = new Transform();
        public GameObject gameObject = new GameObject();
        protected T Instantiate<T>(T value, Vector3 position, Quaternion rotation) where T : new() => new T();
        protected T GetComponentInParent<T>() where T : class => null;
        protected T GetComponent<T>() where T : class => null;
        protected T FindObjectOfType<T>() where T : class => null;
        protected void StartCoroutine(IEnumerator routine) {}
    }
    public class GameObject : Object { public void SetActive(bool value) {} }
    public class Transform : Object { public Vector3 position; public Vector3 localScale = new Vector3(1,1,1); }
    public class Sprite : Object {}
    public struct Vector3
    {
        public float x,y,z;
        public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; }
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);
    }
    public struct Quaternion { public static Quaternion identity => new Quaternion(); }
    public class CanvasGroup : Object { public float alpha; public bool blocksRaycasts, interactable; }
    public static class Time { public static float timeScale = 1; }
    public static class Mathf
    {
        public static int Max(int a,int b) => Math.Max(a,b);
        public static float Max(float a,float b) => Math.Max(a,b);
        public static int Min(int a,int b) => Math.Min(a,b);
        public static int RoundToInt(float x) => (int)Math.Round(x);
    }
    public class WaitForSeconds { public WaitForSeconds(float s) {} }
    public static class Debug { public static void LogWarning(object value) {} }
    public enum KeyCode { K }
    public static class Input { public static bool GetKeyDown(KeyCode key) => false; }
    public class SerializeField : Attribute {}
    public class Header : Attribute { public Header(string text) {} }
    public class TextArea : Attribute {}
    public class CreateAssetMenu : Attribute { public string fileName, menuName; }
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    public class RuntimeInitializeOnLoadMethod : Attribute { public RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType type) {} }
}
namespace UnityEngine.Serialization { public class FormerlySerializedAs : Attribute { public FormerlySerializedAs(string text) {} } }
namespace UnityEngine.UI
{
    public class Image { public UnityEngine.Sprite sprite; public UnityEngine.GameObject gameObject = new UnityEngine.GameObject(); }
    public class Slider { public float maxValue,value; }
}
namespace UnityEngine.EventSystems
{
    public interface IPointerClickHandler { void OnPointerClick(PointerEventData data); }
    public class PointerEventData { public enum InputButton { Left, Right } public InputButton button; }
}
namespace TMPro { public class TMP_Text { public string text; } }
public class PlayerMovement : UnityEngine.MonoBehaviour {}
public class PlayerHealth { public void ChangeHealth(int amount) { StatsManager.Instance.UpdateHealth(amount); } }
public class Loot
{
    public static event Func<ItemSO,int,int> OnItemLooted;
    public void Initialize(ItemSO item,int quantity) {}
}
public class ActorSO : UnityEngine.Object {}
public class LocationSO : UnityEngine.Object {}
public class History { public bool HasVisited(LocationSO location) => false; public bool HasSpokenWith(ActorSO actor) => false; }
public class GameManager
{
    public static GameManager Instance;
    public History LoacationHistoyrTracker, DialogueHistoryTracker;
}
public class StatsManager
{
    public static StatsManager Instance;
    public int currentHealth,maxHealth,speed,damage;
    public void UpdateHealth(int amount) { currentHealth=Math.Max(0,Math.Min(maxHealth,currentHealth+amount)); }
    public void UpdateMaxHealth(int amount) { maxHealth+=amount; }
    public void UpdateSpeed(int amount) { speed+=amount; }
    public void UpdateDamage(int amount) { damage+=amount; }
}
public class ShopKeeper { public static event Action<ShopManager,bool> OnShopStateChanged; }
public class ShopSlot
{
    public UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
    public ItemSO itemSO; public int price;
    public void Initialize(ItemSO item,int value) { itemSO=item;price=value; }
}
public class Enemy_Health { public static event Action<int> OnMonsterDefeated; }
public class QuestObjectiveSlot
{
    public UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
    public void RefreshObjectives(string description,string progress,bool complete) {}
}
public class QuestRewardSlot
{
    public UnityEngine.GameObject gameObject = new UnityEngine.GameObject();
    public void DisplayReward(UnityEngine.Sprite icon,int quantity) {}
}
public class QuestLogSlot { public QuestLogUI questLogUI; public void SetQues(QuestSo quest) {} }
