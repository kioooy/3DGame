using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Tool tự động gán SFX cho Caro và Vật Tay.
/// Menu: Window > Quest System > Setup Minigames FX
/// </summary>
public class MinigameUIFixTool
{
    [MenuItem("Window/Quest System/Setup Minigames FX")]
    public static void SetupMinigames()
    {
        int fixCount = 0;

        // --- 1. Load Audio Clips ---
        AudioClip clickSFX = LoadAudio("sfx_ui_click");
        AudioClip correctSFX = LoadAudio("sfx_ui_correct");
        AudioClip wrongSFX = LoadAudio("sfx_ui_wrong");
        AudioClip winSFX = LoadAudio("sfx_ui_win");
        AudioClip loseSFX = LoadAudio("sfx_ui_lose");
        
        AudioClip caroBGM = LoadAudio("sfx_bgm_caro");
        AudioClip armBGM = LoadAudio("sfx_bgm_armwrestling");

        if (clickSFX == null && winSFX == null)
        {
            Debug.LogError("MinigameUIFixTool: Không tìm thấy file âm thanh sfx_*.wav nào trong project! Đợi một lát cho Unity import xong.");
            return;
        }

        // --- 2. Xử lý CaroGameManager ---
        CaroGameManager caro = Object.FindAnyObjectByType<CaroGameManager>(FindObjectsInactive.Include);
        if (caro == null)
        {
            GameObject caroGo = new GameObject("CaroGameManager");
            caro = caroGo.AddComponent<CaroGameManager>();
            Debug.Log("MinigameUIFixTool: Đã tạo mới CaroGameManager do chưa có trong scene.");
        }
        
        if (caro != null)
        {
            SerializedObject so = new SerializedObject(caro);
            so.FindProperty("clickSFX").objectReferenceValue = clickSFX;
            so.FindProperty("winSFX").objectReferenceValue = winSFX;
            so.FindProperty("loseSFX").objectReferenceValue = loseSFX;
            so.FindProperty("bgmClip").objectReferenceValue = caroBGM;
            so.ApplyModifiedProperties();
            
            EditorUtility.SetDirty(caro);
            Debug.Log("MinigameUIFixTool: ✅ Đã gán âm thanh cho CaroGameManager.");
            fixCount++;
        }

        // --- 3. Xử lý ArmWrestlingManager ---
        ArmWrestlingManager arm = Object.FindAnyObjectByType<ArmWrestlingManager>(FindObjectsInactive.Include);
        if (arm == null)
        {
            GameObject armGo = new GameObject("ArmWrestlingManager");
            arm = armGo.AddComponent<ArmWrestlingManager>();
            Debug.Log("MinigameUIFixTool: Đã tạo mới ArmWrestlingManager do chưa có trong scene.");
        }

        if (arm != null)
        {
            SerializedObject so = new SerializedObject(arm);
            so.FindProperty("correctSFX").objectReferenceValue = correctSFX;
            so.FindProperty("wrongSFX").objectReferenceValue = wrongSFX;
            so.FindProperty("winSFX").objectReferenceValue = winSFX;
            so.FindProperty("loseSFX").objectReferenceValue = loseSFX;
            so.FindProperty("bgmClip").objectReferenceValue = armBGM;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(arm);
            Debug.Log("MinigameUIFixTool: ✅ Đã gán âm thanh cho ArmWrestlingManager.");
            fixCount++;
        }

        // 4. Save
        if (fixCount > 0)
        {
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log("MinigameUIFixTool: ✅✅ Cài đặt hoàn tất! Bấm Ctrl+S để lưu Scene.");
        }
    }

    private static AudioClip LoadAudio(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:AudioClip");
        if (guids.Length > 0)
        {
            return AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        return null;
    }
}
