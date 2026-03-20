using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor Tool: Setup nhạc nền Minecraft-style vào Scene hiện tại.
/// Menu: Window > Audio > Setup Background Music
/// </summary>
public class BackgroundMusicSetupTool
{
    [MenuItem("Window/Audio/Setup Background Music (Minecraft Style)")]
    public static void SetupBackgroundMusic()
    {
        // 1. Kiểm tra đã có manager chưa
        BackgroundMusicManager existing = Object.FindFirstObjectByType<BackgroundMusicManager>();
        if (existing != null)
        {
            Debug.Log("BackgroundMusicSetupTool: BackgroundMusicManager đã tồn tại.");
            Selection.activeGameObject = existing.gameObject;
            EditorGUIUtility.PingObject(existing.gameObject);
            return;
        }

        // 2. Tạo GameObject mới
        GameObject musicObj = new GameObject("BackgroundMusicManager");

        // 3. Thêm AudioSource (cấu hình 2D ambient)
        AudioSource src = musicObj.AddComponent<AudioSource>();
        src.volume = 0f;
        src.loop = false;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        src.priority = 64;

        // 4. Thêm script manager
        BackgroundMusicManager manager = musicObj.AddComponent<BackgroundMusicManager>();
        manager.minSilenceTime = 60f;
        manager.maxSilenceTime = 180f;
        manager.maxVolume = 0.4f;
        manager.fadeDuration = 3f;

        // 5. Tự động tìm và gán các file nhạc trong Assets/Resources/Music_BGM
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Resources/Music_BGM" });
        if (guids.Length > 0)
        {
            AudioClip[] clips = new AudioClip[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Debug.Log($"BackgroundMusicSetupTool: Tìm thấy nhạc: {path}");
            }
            manager.musicTracks = clips;
            Debug.Log($"BackgroundMusicSetupTool: Đã gán {clips.Length} track nhạc.");
        }
        else
        {
            Debug.LogWarning("BackgroundMusicSetupTool: Chưa có file nhạc trong Assets/Resources/Music_BGM.\n" +
                             "Hãy đặt nhạc vào thư mục: Assets/Resources/Music_BGM/\n" +
                             "Sau đó chạy lại tool này hoặc kéo file vào Inspector.");
        }

        // 6. Mark dirty & select
        EditorUtility.SetDirty(musicObj);
        EditorSceneManager.MarkSceneDirty(musicObj.scene);
        Selection.activeGameObject = musicObj;

        Debug.Log("BackgroundMusicSetupTool: ✅ Đã tạo BackgroundMusicManager!\n" +
                  "Bước tiếp theo: Nhạc sẽ được nhận tự động từ Assets/Resources/Music_BGM/.");
    }
}
