using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Tự động tạo BackgroundMusicManager và gán 3 bài nhạc đã tạo sẵn.
/// Menu: Window > Audio > Add Minecraft Music to Scene
/// </summary>
public class AutoAddMusicTool
{
    [MenuItem("Window/Audio/Add Minecraft Music to Scene")]
    public static void AddMusicToScene()
    {
        // 1. Xoá manager cũ nếu có
        BackgroundMusicManager old = Object.FindFirstObjectByType<BackgroundMusicManager>();
        if (old != null)
        {
            Undo.DestroyObjectImmediate(old.gameObject);
        }

        // 2. Tạo mới
        GameObject obj = new GameObject("BackgroundMusicManager");
        Undo.RegisterCreatedObjectUndo(obj, "Add Music Manager");

        AudioSource src = obj.AddComponent<AudioSource>();
        src.volume = 0f;
        src.loop = false;
        src.playOnAwake = false;
        src.spatialBlend = 0f;

        BackgroundMusicManager mgr = obj.AddComponent<BackgroundMusicManager>();
        mgr.minSilenceTime = 60f;
        mgr.maxSilenceTime = 180f;
        mgr.maxVolume = 0.4f;
        mgr.fadeDuration = 3f;

        // 3. Load 3 tracks đã tạo sẵn
        string[] trackNames = { "calm_sweden", "wet_hands", "living_mice" };
        var clips = new System.Collections.Generic.List<AudioClip>();

        foreach (var name in trackNames)
        {
            // Tìm theo guid (format wav)
            string[] guids = AssetDatabase.FindAssets(name + " t:AudioClip");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    clips.Add(clip);
                    Debug.Log($"AutoAddMusicTool: Loaded '{name}' from {path}");
                }
            }
            else
            {
                Debug.LogWarning($"AutoAddMusicTool: Không tìm thấy '{name}.wav' trong Assets/Audio/Music/. Hãy chờ Unity import xong (xoay loader góc phải dưới).");
            }
        }

        // Gán SerializedProperty để Unity lưu đúng cách
        SerializedObject so = new SerializedObject(mgr);
        SerializedProperty tracksProp = so.FindProperty("musicTracks");
        tracksProp.arraySize = clips.Count;
        for (int i = 0; i < clips.Count; i++)
            tracksProp.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(obj);
        EditorSceneManager.MarkSceneDirty(obj.scene);
        Selection.activeGameObject = obj;

        if (clips.Count == 3)
        {
            Debug.Log("AutoAddMusicTool: ✅ Đã tạo BackgroundMusicManager với 3 bài nhạc Minecraft! Nhấn Ctrl+S lưu scene.");
        }
        else if (clips.Count > 0)
        {
            Debug.LogWarning($"AutoAddMusicTool: Chỉ tìm được {clips.Count}/3 bài. Hãy chờ Unity import xong rồi chạy lại tool.");
        }
        else
        {
            Debug.LogError("AutoAddMusicTool: Không tìm thấy bài nào! Hãy chờ Unity import file WAV xong (loading bar dưới cùng màn hình) rồi chạy lại.");
        }
    }
}
