using UnityEngine;

/// <summary>
/// Script tự động xóa duplicate Audio Listeners trong scene
/// Chỉ giữ lại Audio Listener trên Main Camera
/// </summary>
[ExecuteInEditMode]
public class AudioListenerFixer : MonoBehaviour
{
    private static bool _hasFixedThisSession = false;
    
    void Awake()
    {
        if (!_hasFixedThisSession)
        {
            FixDuplicateAudioListeners();
            _hasFixedThisSession = true;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Only fix in edit mode, not during play
        if (!Application.isPlaying)
        {
            FixDuplicateAudioListeners();
        }
    }
#endif

    void FixDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        if (listeners.Length <= 1)
            return;

        Debug.LogWarning($"[AudioListenerFixer] Tìm thấy {listeners.Length} Audio Listeners! Đang fix...");

        // Tìm Main Camera
        Camera mainCam = Camera.main;
        AudioListener mainListener = mainCam?.GetComponent<AudioListener>();

        int removedCount = 0;

        // Disable tất cả listeners trừ main camera
        foreach (AudioListener listener in listeners)
        {
            if (listener != mainListener)
            {
                Debug.Log($"[AudioListenerFixer] Xóa Audio Listener khỏi: {listener.gameObject.name}");
                
                if (Application.isPlaying)
                    listener.enabled = false;
                else
                    DestroyImmediate(listener);
                
                removedCount++;
            }
        }

        // Nếu main camera không có listener, thêm vào
        if (mainCam != null && mainListener == null)
        {
            mainCam.gameObject.AddComponent<AudioListener>();
            Debug.Log("[AudioListenerFixer] Đã thêm Audio Listener vào Main Camera");
        }
        
        if (removedCount > 0)
        {
            Debug.Log($"[AudioListenerFixer] ✅ Đã xóa {removedCount} duplicate Audio Listeners");
        }
    }
}
