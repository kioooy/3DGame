using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class FixMissingScriptsTool : EditorWindow
{
    [MenuItem("Tools/Sửa Lỗi Missing Script trong Scene Hiện Tại (Auto Fix)")]
    public static void FixMissingScripts()
    {
        var activeScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = activeScene.GetRootGameObjects();
        int totalRemoved = 0;
        
        foreach (var go in rootObjects)
        {
            totalRemoved += RecursivelyRemoveMissing(go);
        }
        
        Debug.Log($"[FixMissingScripts] Đã xóa {totalRemoved} Missing Scripts từ Scene {activeScene.name}");
        if (totalRemoved > 0)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            EditorUtility.DisplayDialog("Hoàn tất", $"Đã xóa {totalRemoved} missing scripts.\nBây giờ bạn có thể bấm Play bình thường. Hãy nhấn Ctrl+S để lưu Scene lại nhé!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Hoàn tất", "Không tìm thấy Missing Script nào ở trên các GameObject trong Scene này.\nLỗi có thể nằm ở các Button có sự kiện trỏ tới file bị xóa.", "OK");
        }
    }

    private static int RecursivelyRemoveMissing(GameObject go)
    {
        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        
        foreach (Transform child in go.transform)
        {
            count += RecursivelyRemoveMissing(child.gameObject);
        }
        
        return count;
    }
}
