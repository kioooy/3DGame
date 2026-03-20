using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Script tự động xóa các EventSystem và InputModule thừa lúc Runtime.
/// Đảm bảo trong Scene luôn chỉ có duy nhất 1 EventSystem, ngăn chặn lỗi "There can be only one active Event System".
/// Script này tự động chạy, không cần kéo thả vào Scene.
/// </summary>
public class EventSystemCleaner : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeCleaner()
    {
        GameObject cleanerObj = new GameObject("EventSystemCleaner_Auto");
        Object.DontDestroyOnLoad(cleanerObj);
        cleanerObj.AddComponent<EventSystemCleaner>();
        
        // Gọi dọn dẹp ngay lần đầu load
        CleanUpEventSystems();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CleanUpEventSystems();
    }

    private float _checkTimer = 0f;

    void Update()
    {
        // Quét mỗi giây 1 lần để nhỡ có Quests UI hoặc Minigame Prefab nào đó Spawn ra mang theo EventSystem
        _checkTimer += Time.deltaTime;
        if (_checkTimer >= 1f) 
        {
            _checkTimer = 0f;
            CleanUpEventSystems();
        }
    }

    public static void CleanUpEventSystems()
    {
        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        
        if (eventSystems.Length > 1)
        {
            Debug.Log($"[EventSystemCleaner] Phát hiện {eventSystems.Length} EventSystems. Đang dọn dẹp để chỉ giữ lại 1...");
            
            // Ưu tiên giữ EventSystem.current nếu nó tồn tại và đang active. 
            // Nếu không, giữ cái đầu tiên.
            EventSystem mainSystem = EventSystem.current != null ? EventSystem.current : eventSystems[0];
            
            foreach (EventSystem es in eventSystems)
            {
                if (es != mainSystem && es != null)
                {
                    // Hủy GameObject chứa EventSystem đó nếu nó là Game Object chuyên dụng chỉ chứa EventSystem
                    if (es.gameObject.name.Contains("EventSystem") && es.GetComponents<Component>().Length <= 4) 
                    {
                        Object.Destroy(es.gameObject);
                    }
                    else
                    {
                        // Nếu nó bám vào 1 Canvas hay obj quan trọng quá thì chỉ hủy Component thôi
                        BaseInputModule module = es.GetComponent<BaseInputModule>();
                        if (module != null) Object.Destroy(module);
                        Object.Destroy(es);
                    }
                }
            }
        }
    }
}
