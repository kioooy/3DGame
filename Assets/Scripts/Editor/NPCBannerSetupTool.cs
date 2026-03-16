using UnityEngine;
using UnityEditor;

public class NPCBannerSetupTool : EditorWindow
{
    [MenuItem("Window/Quest System/Remove NPC Banners")]
    public static void RemoveBanners()
    {
        // Tìm và xóa tất cả các dải Banner đã tạo
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int count = 0;
        
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            
            if (obj.name == "NPC_Banner")
            {
                DestroyImmediate(obj);
                count++;
            }
        }

        Debug.Log($"Đã dọn dẹp và xóa {count} bảng tên NPC khỏi Scene!");
        
        if (!Application.isPlaying) 
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }
}
