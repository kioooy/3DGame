using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public class AddSceneToBuildTool : EditorWindow
{
    private string sceneName = "StylizedNatureLite_Demo";

    [MenuItem("Tools/Thêm Scene Vào Build Settings")]
    public static void ShowWindow()
    {
        GetWindow<AddSceneToBuildTool>("Add Scene to Build").Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Công Cụ Khắc Phục Lỗi Chuyển Scene", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        GUILayout.Label("Nếu bấm nút Play mà trò chơi không load được Scene, \nnguyên nhân có thể là do Scene đó chưa được thêm vào Build Settings.", EditorStyles.wordWrappedLabel);
        GUILayout.Space(10);
        
        sceneName = EditorGUILayout.TextField("Tên Scene cần thêm:", sceneName);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Thêm Scene '" + sceneName + "' vào Build Settings", GUILayout.Height(40)))
        {
            AddSceneToBuildSettings(sceneName);
        }
    }

    private void AddSceneToBuildSettings(string targetSceneName)
    {
        // 1. Tìm đường dẫn của Scene
        string[] guids = AssetDatabase.FindAssets(targetSceneName + " t:scene");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không tìm thấy Scene nào có tên '{targetSceneName}' trong dự án.\nHãy kiểm tra lại tên chính xác của Scene.", "OK");
            return;
        }

        string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);

        // 2. Kiểm tra xem Scene đã có trong Build Settings chưa
        var originalBuildScenes = EditorBuildSettings.scenes;
        foreach (var buildScene in originalBuildScenes)
        {
            if (buildScene.path == scenePath)
            {
                if (!buildScene.enabled)
                {
                    buildScene.enabled = true;
                    EditorBuildSettings.scenes = originalBuildScenes;
                    EditorUtility.DisplayDialog("Thành công", $"Scene '{targetSceneName}' đã có trong Build Settings nhưng bị tắt. Nay đã được TẮT -> BẬT lên.", "OK");
                }
                else
                {
                    EditorUtility.DisplayDialog("Thông báo", $"Scene '{targetSceneName}' vốn dĩ đã nằm sẵn trong Build Settings và đang được bật bình thường.\nLỗi không chuyển được Scene có thể do nguyên nhân khác.", "OK");
                }
                return;
            }
        }

        // 3. Thêm Scene mới vào Build Settings
        var newBuildScenes = new EditorBuildSettingsScene[originalBuildScenes.Length + 1];
        System.Array.Copy(originalBuildScenes, newBuildScenes, originalBuildScenes.Length);
        newBuildScenes[newBuildScenes.Length - 1] = new EditorBuildSettingsScene(scenePath, true);
        
        EditorBuildSettings.scenes = newBuildScenes;
        
        EditorUtility.DisplayDialog("Thành công!", $"Đã thêm Scene '{targetSceneName}' vào File > Build Settings.\nBạn có thể thử bấm Play lại để kiểm tra chuyển Scene.", "OK");
    }
}
