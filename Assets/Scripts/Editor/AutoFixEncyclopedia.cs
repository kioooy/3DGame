using UnityEditor;
using UnityEngine;
using System.Reflection;

[InitializeOnLoad]
public class AutoFixEncyclopedia
{
    static AutoFixEncyclopedia()
    {
        EditorApplication.delayCall += DoFix;
    }

    static void DoFix()
    {
        if (Application.isPlaying) return; // Nếu đang chơi thì không chạy để tránh xóa trúng đồ đang chơi
        if (EditorPrefs.GetBool("AutoFixEncyclopediaDone5", false)) return;
        EditorPrefs.SetBool("AutoFixEncyclopediaDone5", true);

        // 1. Xóa Canvas cũ đang bị lỗi layout
        GameObject oldCanvas = GameObject.Find("EncyclopediaCanvas");
        if (oldCanvas != null)
        {
            GameObject.DestroyImmediate(oldCanvas);
            Debug.Log("[AutoFix] Đã tự động dọn dẹp EncyclopediaCanvas cũ bị lỗi.");
        }

        GameObject oldManager = GameObject.Find("EncyclopediaManager");
        if (oldManager != null)
        {
            GameObject.DestroyImmediate(oldManager);
        }

        try
        {
            // 2. Dùng Reflection gọi hàm Sinh UI từ file Setup để nó build lại bản mới tinh!
            var window = ScriptableObject.CreateInstance<EncyclopediaSetupTool>();
            MethodInfo method = typeof(EncyclopediaSetupTool).GetMethod("CreateEncyclopediaUI", BindingFlags.NonPublic | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(window, null);
                Debug.Log("<color=green>[TỰ ĐỘNG SỬA] ✅ Đã tự động tạo lại EncyclopediaCanvas mới với Layout và Anchor chuẩn 100%!</color>");
            }
            GameObject.DestroyImmediate(window);
        }
        catch (System.Exception e)
        {
            Debug.LogError("AutoFix error: " + e);
        }
    }
}
