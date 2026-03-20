using UnityEngine;

/// <summary>
/// Script mạnh hơn để fix Audio Listener spam
/// Tự động disable duplicate listeners mỗi frame
/// </summary>
public class AudioListenerManager : MonoBehaviour
{
    private static AudioListenerManager _instance;
    
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    void LateUpdate()
    {
        // Chạy mỗi frame để đảm bảo chỉ có 1 listener active
        EnsureSingleAudioListener();
    }
    
    void EnsureSingleAudioListener()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        if (listeners.Length <= 1)
            return;
        
        // Tìm Main Camera listener
        Camera mainCam = Camera.main;
        AudioListener mainListener = mainCam?.GetComponent<AudioListener>();
        
        // Disable tất cả listeners khác
        foreach (AudioListener listener in listeners)
        {
            if (listener != mainListener && listener.enabled)
            {
                listener.enabled = false;
                // Không log để tránh spam
            }
        }
        
        // Đảm bảo main camera listener được enable
        if (mainListener != null && !mainListener.enabled)
        {
            mainListener.enabled = true;
        }
    }
}
