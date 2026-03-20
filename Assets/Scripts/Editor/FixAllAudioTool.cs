using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Tool fix toàn bộ Audio: AudioListener, nhạc nền, tiếng bước chân.
/// Menu: Window > Audio > Fix All Audio
/// </summary>
public class FixAllAudioTool
{
    [MenuItem("Window/Audio/Fix All Audio (Listener + Music + Footsteps)")]
    public static void FixAll()
    {
        int fixCount = 0;

        // ─── 1. FIX AUDIO LISTENER ───────────────────────────────────────────
        AudioListener[] listeners = Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        if (listeners.Length == 0)
        {
            // Thêm vào Camera chính
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.gameObject.AddComponent<AudioListener>();
                EditorUtility.SetDirty(mainCam.gameObject);
                Debug.Log("FixAllAudioTool: ✅ Đã thêm AudioListener vào Main Camera.");
                fixCount++;
            }
            else
            {
                // Tìm bất kỳ camera nào
                Camera anyCam = Object.FindFirstObjectByType<Camera>();
                if (anyCam != null)
                {
                    anyCam.gameObject.AddComponent<AudioListener>();
                    EditorUtility.SetDirty(anyCam.gameObject);
                    Debug.Log("FixAllAudioTool: ✅ Đã thêm AudioListener vào Camera.");
                    fixCount++;
                }
                else
                {
                    Debug.LogWarning("FixAllAudioTool: Không tìm thấy Camera! Vui lòng thêm AudioListener thủ công.");
                }
            }
        }
        else if (listeners.Length > 1)
        {
            // Giữ lại cái đầu, xoá các cái còn lại
            for (int i = 1; i < listeners.Length; i++)
            {
                Undo.DestroyObjectImmediate(listeners[i]);
                fixCount++;
            }
            Debug.Log($"FixAllAudioTool: ✅ Đã xoá {listeners.Length - 1} AudioListener thừa.");
        }
        else
        {
            Debug.Log("FixAllAudioTool: AudioListener OK.");
        }

        // ─── 2. ADD/FIX BACKGROUND MUSIC MANAGER ─────────────────────────────
        BackgroundMusicManager bgm = Object.FindFirstObjectByType<BackgroundMusicManager>();
        if (bgm == null)
        {
            GameObject obj = new GameObject("BackgroundMusicManager");
            Undo.RegisterCreatedObjectUndo(obj, "Add Music Manager");
            AudioSource src = obj.AddComponent<AudioSource>();
            src.playOnAwake  = false;
            src.spatialBlend = 0f;
            bgm = obj.AddComponent<BackgroundMusicManager>();
            bgm.minSilenceTime = 60f;
            bgm.maxSilenceTime = 180f;
            bgm.maxVolume = 0.4f;
            bgm.fadeDuration = 3f;
            Debug.Log("FixAllAudioTool: ✅ Tạo BackgroundMusicManager mới.");
            fixCount++;
        }

        // Gán tracks nhạc nếu chưa có
        SerializedObject bso = new SerializedObject(bgm);
        SerializedProperty tracksProp = bso.FindProperty("musicTracks");
        if (tracksProp.arraySize == 0)
        {
            string[] musicNames = { "calm_sweden", "wet_hands", "living_mice" };
            var clips = new System.Collections.Generic.List<AudioClip>();
            foreach (var name in musicNames)
            {
                string[] guids = AssetDatabase.FindAssets(name + " t:AudioClip");
                if (guids.Length > 0)
                    clips.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0])));
            }
            tracksProp.arraySize = clips.Count;
            for (int i = 0; i < clips.Count; i++)
                tracksProp.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
            bso.ApplyModifiedProperties();
            Debug.Log($"FixAllAudioTool: ✅ Gán {clips.Count} track nhạc vào BackgroundMusicManager.");
            fixCount++;
        }

        // ─── 3. WIRE FOOTSTEP CLIPS TO PLAYER ────────────────────────────────
        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            SerializedObject pso = new SerializedObject(pc);
            SerializedProperty clipsProp = pso.FindProperty("footstepClips");

            if (clipsProp != null && clipsProp.arraySize == 0)
            {
                var footClips = new System.Collections.Generic.List<AudioClip>();
                for (int i = 1; i <= 4; i++)
                {
                    string[] guids = AssetDatabase.FindAssets($"footstep_{i} t:AudioClip");
                    if (guids.Length > 0)
                        footClips.Add(AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0])));
                }
                clipsProp.arraySize = footClips.Count;
                for (int i = 0; i < footClips.Count; i++)
                    clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = footClips[i];
                pso.ApplyModifiedProperties();
                Debug.Log($"FixAllAudioTool: ✅ Gán {footClips.Count} tiếng bước chân vào PlayerController.");
                fixCount++;
            }
            else if (clipsProp != null && clipsProp.arraySize > 0)
            {
                Debug.Log("FixAllAudioTool: Footstep clips đã được gán.");
            }
        }
        else
        {
            Debug.LogWarning("FixAllAudioTool: Không tìm thấy PlayerController trong scene!");
        }

        // ─── Save ─────────────────────────────────────────────────────────────
        if (fixCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"FixAllAudioTool: ✅✅ Hoàn thành! Đã sửa {fixCount} vấn đề. Hãy nhấn Ctrl+S để lưu!");
        }
        else
        {
            Debug.Log("FixAllAudioTool: Tất cả Audio đã OK rồi!");
        }
    }
}
