using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Persistent Objects")]
    public GameObject[] persisentObjects;

    [Header("History Trackers")]
    public LocationHistoryTracker LoacationHistoyrTracker;
    public DialogueHistoryTracker DialogueHistoryTracker;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            CleanUpAndDestroy();
            return;
        }

        Instance = this;

        // 优先使用 Inspector 中绑定的组件，
        // 没有绑定时查找当前对象上的组件。
        if (LoacationHistoyrTracker == null)
        {
            LoacationHistoyrTracker =
                GetComponent<LocationHistoryTracker>();
        }

        if (DialogueHistoryTracker == null)
        {
            DialogueHistoryTracker =
                GetComponent<DialogueHistoryTracker>();
        }

        DontDestroyOnLoad(gameObject);
        MarkPersisentObject();
    }

    private void MarkPersisentObject()
    {
        if (persisentObjects == null)
            return;

        foreach (GameObject obj in persisentObjects)
        {
            if (obj != null)
            {
                DontDestroyOnLoad(obj);
            }
        }
    }

    private void CleanUpAndDestroy()
    {
        if (persisentObjects != null)
        {
            foreach (GameObject obj in persisentObjects)
            {
                if (obj == null || obj == gameObject)
                    continue;

                // 避免销毁已由主 GameManager 保留的共享对象。
                if (Instance != null &&
                    Instance.persisentObjects != null &&
                    System.Array.IndexOf(
                        Instance.persisentObjects, obj) >= 0)
                {
                    continue;
                }

                if (Instance != null &&
                    obj == Instance.gameObject)
                {
                    continue;
                }

                Destroy(obj);
            }
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}