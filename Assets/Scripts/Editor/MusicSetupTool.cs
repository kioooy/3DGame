using UnityEngine;
using UnityEditor;

public class MusicSetupTool : EditorWindow
{
    [MenuItem("Window/Quest System/3. Setup Background Music")]
    public static void AddBackgroundMusic()
    {
        // Kiểm tra xem đã có Manager trong scene chưa
        BackgroundMusicManager existingManager = FindFirstObjectByType<BackgroundMusicManager>();
        
        if (existingManager == null)
        {
            GameObject bgmObj = new GameObject("BackgroundMusicManager");
            BackgroundMusicManager bgManager = bgmObj.AddComponent<BackgroundMusicManager>();
            AudioSource audioSource = bgmObj.GetComponent<AudioSource>();
            
            // Tìm file MEDIT.mp3 trong thư mục Assets/Audio
            AudioClip meditClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/MEDIT.mp3");
            
            if (meditClip != null)
            {
                audioSource.clip = meditClip;
                audioSource.loop = true;          // Lặp lại nhạc
                audioSource.playOnAwake = true;   // Phát ngay khi game bắt đầu
                audioSource.volume = 0.5f;        // Để volume 50% tránh quá ồn
                Debug.Log($"[MusicSetupTool] Đã tự động gắn nhạc nền MEDIT.mp3 vào game!");
                
                Selection.activeGameObject = bgmObj; // Chọn luôn để user dễ thấy
            }
            else
            {
                Debug.LogWarning("[MusicSetupTool] Không tìm thấy file MEDIT.mp3 tại Assets/Audio/MEDIT.mp3. Hãy tự thiết lập thủ công nhé.");
            }
        }
        else
        {
            Debug.Log("[MusicSetupTool] Scene đã có sẵn BackgroundMusicManager rồi.");
        }
    }
}
