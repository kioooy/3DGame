using UnityEngine;
using UnityEditor;

public class PlayerAudioSetupTool : EditorWindow
{
    [MenuItem("Window/Audio/Setup Dế Mèn (Player) Audio")]
    public static void SetupPlayerAudio()
    {
        // 1. Tìm PlayerController trong Scene hiện tại
        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        
        if (players.Length == 0)
        {
            Debug.LogWarning("PlayerAudioSetupTool: KHÔNG tìm thấy PlayerController trong Scene này!");
            return;
        }

        string jumpAudioPath = "Assets/Audio 1/Footsteps - Essentials/Footsteps_Grass/Footsteps_Grass_Jump/Footsteps_Grass_Jump_Land_03.wav";
        AudioClip jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>(jumpAudioPath);

        string walkAudioPath = "Assets/Audio 1/Footsteps - Essentials/Footsteps_Grass/Footsteps_Grass_Walk/Footsteps_Walk_Grass_Mono_02.wav";
        AudioClip walkClip = AssetDatabase.LoadAssetAtPath<AudioClip>(walkAudioPath);

        string runAudioPath = "Assets/Audio 1/Footsteps - Essentials/Footsteps_Grass/Footsteps_Grass_Run/Footsteps_Grass_Run_03.wav";
        AudioClip runClip = AssetDatabase.LoadAssetAtPath<AudioClip>(runAudioPath);

        if (jumpClip == null || walkClip == null || runClip == null)
        {
            Debug.LogError($"PlayerAudioSetupTool: Thiếu một hoặc nhiều file âm thanh. Bạn hãy kiểm tra lại các đường dẫn.\nJump: {jumpAudioPath}\nWalk: {walkAudioPath}\nRun: {runAudioPath}");
            return;
        }

        int updatedCount = 0;

        foreach (var player in players)
        {
            // 3. Gán audio clip thông qua SerializedObject để áp dụng thay đổi và lưu vào hệ thống undo
            SerializedObject obj = new SerializedObject(player);
            SerializedProperty jumpClipProp = obj.FindProperty("jumpClip");
            
            if (jumpClipProp != null)
            {
                jumpClipProp.objectReferenceValue = jumpClip;
                
                // Mặc định âm lượng được đặt là 0.6f trong code, nhưng để chắc chắn ta cũng ghi lại
                SerializedProperty jumpVolProp = obj.FindProperty("jumpVolume");
                if(jumpVolProp != null)
                {
                    jumpVolProp.floatValue = 0.6f;
                }
                
                SerializedProperty walkClipProp = obj.FindProperty("walkClip");
                if (walkClipProp != null) walkClipProp.objectReferenceValue = walkClip;

                SerializedProperty runClipProp = obj.FindProperty("runClip");
                if (runClipProp != null) runClipProp.objectReferenceValue = runClip;

                obj.ApplyModifiedProperties();
                EditorUtility.SetDirty(player);
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            Debug.Log($"PlayerAudioSetupTool: ✅ Đã gán toàn bộ âm thanh (Nhảy, Đi bộ, Chạy) cho {updatedCount} Dế Mèn (PlayerController) trong Scene.");
        }
    }
}
