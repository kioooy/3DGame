using UnityEngine;
using UnityEditor;

public class QuestSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Quest UI")]
    public static void SetupQuestUI()
    {
        // 1. Tìm hoặc tạo QuestUIManager
        QuestUIManager manager = Object.FindFirstObjectByType<QuestUIManager>();
        if (manager == null)
        {
            GameObject go = new GameObject("QuestManager");
            manager = go.AddComponent<QuestUIManager>();
            Debug.Log("QuestSetupTool: Đã tạo QuestManager GameObject.");
        }

        // 2. Tạo Quest UI
        manager.CreateDefaultUI();

        // 3. Set singleton reference trong Editor
        QuestUIManager.Instance = manager;

        // 4. Đánh dấu dirty để Unity lưu thay đổi
        EditorUtility.SetDirty(manager);

        Debug.Log("QuestSetupTool: Quest UI đã được tạo thành công! Bấm J trong game để mở/đóng tab nhiệm vụ.");

        // Select object cho user dễ thấy
        Selection.activeGameObject = manager.gameObject;
    }
}
