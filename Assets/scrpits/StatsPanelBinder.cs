using UnityEngine;
using UnityEngine.SceneManagement;

// Compatibility refresh for the authored stats panels only. Never rewrite arbitrary world/UI text.
public sealed class StatsPanelBinder : MonoBehaviour
{
    public static bool Enabled=true;
    static StatsPanelBinder active;
    float nextApply;
    StatsUI[] panels;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot(){SceneManager.sceneLoaded-=OnSceneLoaded;SceneManager.sceneLoaded+=OnSceneLoaded;}
    static void OnSceneLoaded(Scene scene,LoadSceneMode mode)
    {
        if(!Enabled)return;
        if(active!=null)Destroy(active.gameObject);
        var go=new GameObject("Stats panel binder");SceneManager.MoveGameObjectToScene(go,scene);
        active=go.AddComponent<StatsPanelBinder>();active.panels=Object.FindObjectsOfType<StatsUI>(true);active.Apply();
    }
    void Apply(){if(panels==null)return;foreach(var panel in panels)if(panel!=null)panel.UpdataAllStats();}
    void Update(){if(Time.unscaledTime<nextApply)return;nextApply=Time.unscaledTime+.15f;Apply();}
}
