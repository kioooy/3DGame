using UnityEngine;
using UnityEditor;

public class TutorialSetupTool
{
    [MenuItem("Quest System/5. Setup Tutorial Manager")]
    public static void ShowWindow()
    {
        TutorialManager existingManager = Object.FindFirstObjectByType<TutorialManager>();
        
        if (existingManager != null)
        {
            EditorUtility.DisplayDialog("Tutorial Setup", "Đã tìm thấy một TutorialManager trong Scene: " + existingManager.gameObject.name, "OK");
            Selection.activeGameObject = existingManager.gameObject;
            return;
        }

        GameObject go = new GameObject("TutorialManager");
        go.AddComponent<TutorialManager>();
        
        Undo.RegisterCreatedObjectUndo(go, "Create Tutorial Manager");
        Selection.activeGameObject = go;
        
        Debug.Log("<color=green>[TutorialSetup]</color> Đã tạo thành công đối tượng TutorialManager trong Scene.");
    }
    
    [MenuItem("Quest System/Dev Tools/Reset Tutorial Status")]
    public static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("HasSeenTutorial");
        PlayerPrefs.Save();
        Debug.Log("<color=yellow>[TutorialSetup]</color> Đã XÓA ghi nhớ Tutorial! Bảng hướng dẫn sẽ hiển thị lại khi bạn vào Game.");
    }
}
