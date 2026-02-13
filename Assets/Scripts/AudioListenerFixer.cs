using UnityEngine;

/// <summary>
/// Script tự động xóa duplicate Audio Listeners trong scene
/// Chỉ giữ lại Audio Listener trên Main Camera
/// </summary>
[ExecuteInEditMode]
public class AudioListenerFixer : MonoBehaviour
{
    void Awake()
    {
        FixDuplicateAudioListeners();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        FixDuplicateAudioListeners();
    }
#endif

    void FixDuplicateAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        if (listeners.Length <= 1)
            return;

        Debug.LogWarning($"Tìm thấy {listeners.Length} Audio Listeners! Đang fix...");

        // Tìm Main Camera
        Camera mainCam = Camera.main;
        AudioListener mainListener = mainCam?.GetComponent<AudioListener>();

        // Disable tất cả listeners trừ main camera
        foreach (AudioListener listener in listeners)
        {
            if (listener != mainListener)
            {
                Debug.Log($"Xóa Audio Listener khỏi: {listener.gameObject.name}");
                
                if (Application.isPlaying)
                    listener.enabled = false;
                else
                    DestroyImmediate(listener);
            }
        }

        // Nếu main camera không có listener, thêm vào
        if (mainCam != null && mainListener == null)
        {
            mainCam.gameObject.AddComponent<AudioListener>();
            Debug.Log("Đã thêm Audio Listener vào Main Camera");
        }
    }
}
