using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public string sceneToLoad;
    public Animator fadeAnim;
    public float fadeTime = 0.5f;
    public Vector2 newPlayerPosition;
    bool isLoading,armed;
    static bool transitionPending;
    Collider2D[] zones;
    float enableAfter;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetSession(){transitionPending=false;}
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Watch(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
    static void Loaded(Scene scene,LoadSceneMode mode){transitionPending=false;}
    void OnEnable(){armed=false;isLoading=false;enableAfter=Time.unscaledTime+.75f;zones=GetComponents<Collider2D>();}
    public bool IsArmed => armed;
    public bool ContainsPlayer(PlayerMovement player)
    {
        if(player==null||zones==null)return false;
        foreach(var body in player.GetComponentsInChildren<Collider2D>())
        {
            if(!body.enabled||!body.gameObject.activeInHierarchy)continue;
            foreach(var zone in zones)
                if(zone!=null&&zone.enabled&&zone.isTrigger&&zone.Distance(body).isOverlapped)return true;
        }
        return false;
    }
    void Update()
    {
        if(isLoading||transitionPending||Time.unscaledTime<enableAfter)return;
        // Start, spawn positioning and asynchronous save restoration must all settle first.
        if(SaveSystem.Instance!=null&&!SaveSystem.IsReady)return;
        var player=FindObjectOfType<PlayerMovement>();
        // An arrival inside an exit is deliberately inert until the player walks completely out.
        if(player!=null&&!ContainsPlayer(player))armed=true;
    }
    void OnTriggerEnter2D(Collider2D collision)
    {
        if(!armed||isLoading||transitionPending||Time.timeScale<=0||!collision.CompareTag("Player"))return;
        if(string.IsNullOrWhiteSpace(sceneToLoad)||sceneToLoad==SceneManager.GetActiveScene().name||!Application.CanStreamedLevelBeLoaded(sceneToLoad))return;
        armed=false;isLoading=true;transitionPending=true;
        SaveSystem.SaveNow();FishingRig2D.End();
        if(fadeAnim!=null)fadeAnim.Play("FadeToWhite");
        StartCoroutine(DelayFade());
    }
    IEnumerator DelayFade()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0,fadeTime));
        // Destination Start/SaveSystem owns arrival placement. Never move a persistent actor
        // through other source-scene triggers before unloading the source scene.
        SceneManager.LoadScene(sceneToLoad);
    }
    void OnDisable(){armed=false;if(isLoading)transitionPending=false;}
}
