using UnityEngine;
using UnityEditor;

public class PolishSetupTool
{
    [MenuItem("Quest System/5. Setup Pause & Save Menu")]
    public static void SetupPauseAndSave()
    {
        // 1. Setup PauseMenuManager
        PauseMenuManager pauseManager = Object.FindFirstObjectByType<PauseMenuManager>();
        if (pauseManager == null)
        {
            GameObject go = new GameObject("PauseMenuManager");
            go.AddComponent<PauseMenuManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create PauseMenu Manager");
            Debug.Log("<color=green>[PolishSetupTools]</color> Đã tạo PauseMenuManager.");
        }

        // 2. Setup SaveLoadManager
        SaveLoadManager saveManager = Object.FindFirstObjectByType<SaveLoadManager>();
        if (saveManager == null)
        {
            GameObject go = new GameObject("SaveLoadManager");
            go.AddComponent<SaveLoadManager>();
            Undo.RegisterCreatedObjectUndo(go, "Create SaveLoad Manager");
            Debug.Log("<color=green>[PolishSetupTools]</color> Đã tạo SaveLoadManager.");
        }

        EditorUtility.DisplayDialog("Setup Hoàn Tất", "Đã tích hợp thành công Pause Menu và Save/Load Manager vào Scene!", "OK");
    }
}
