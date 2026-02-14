using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool để tìm và xóa TẤT CẢ Audio Listeners trong scene
/// Sau đó chỉ thêm 1 listener vào Main Camera
/// </summary>
public class AudioListenerCleanupTool : EditorWindow
{
    [MenuItem("Tools/Fix Audio Listener Spam")]
    static void ShowWindow()
    {
        var window = GetWindow<AudioListenerCleanupTool>("Audio Listener Cleanup");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Audio Listener Cleanup Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tool này sẽ:\n" +
            "1. Tìm TẤT CẢ Audio Listeners trong scene\n" +
            "2. XÓA tất cả listeners\n" +
            "3. Chỉ thêm 1 listener vào Main Camera\n\n" +
            "Điều này sẽ fix TRIỆT ĐỂ vấn đề spam warning.",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        // Show current status
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        if (listeners.Length > 1)
        {
            GUI.color = Color.red;
            EditorGUILayout.HelpBox($"⚠️ Tìm thấy {listeners.Length} Audio Listeners!", MessageType.Error);
            GUI.color = Color.white;
            
            GUILayout.Space(5);
            EditorGUILayout.LabelField("Listeners found on:", EditorStyles.boldLabel);
            
            foreach (var listener in listeners)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {listener.gameObject.name}");
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = listener.gameObject;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        else if (listeners.Length == 1)
        {
            GUI.color = Color.green;
            EditorGUILayout.HelpBox($"✅ OK: Chỉ có 1 Audio Listener", MessageType.Info);
            GUI.color = Color.white;
            
            EditorGUILayout.LabelField($"On: {listeners[0].gameObject.name}");
        }
        else
        {
            GUI.color = Color.yellow;
            EditorGUILayout.HelpBox("⚠️ Không có Audio Listener nào!", MessageType.Warning);
            GUI.color = Color.white;
        }
        
        GUILayout.Space(20);
        
        if (GUILayout.Button("🔧 FIX: Remove All & Add to Main Camera", GUILayout.Height(40)))
        {
            CleanupAudioListeners();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Setup Audio Listener Manager (Prevent Future Issues)"))
        {
            SetupAudioListenerManager();
        }
    }
    
    void CleanupAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        
        Debug.Log($"[AudioListenerCleanup] Found {listeners.Length} Audio Listeners");
        
        // Remove ALL listeners
        foreach (var listener in listeners)
        {
            Debug.Log($"[AudioListenerCleanup] Removing Audio Listener from: {listener.gameObject.name}");
            DestroyImmediate(listener);
        }
        
        // Add ONE listener to Main Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            AudioListener newListener = mainCam.gameObject.AddComponent<AudioListener>();
            Debug.Log($"[AudioListenerCleanup] ✅ Added Audio Listener to Main Camera");
            
            EditorUtility.DisplayDialog("Success", 
                "✅ Audio Listener cleanup complete!\n\n" +
                "• Removed all old listeners\n" +
                "• Added 1 listener to Main Camera\n\n" +
                "Warning should be gone now!", 
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", 
                "❌ Cannot find Main Camera!\n\n" +
                "Please make sure you have a camera tagged as 'MainCamera'", 
                "OK");
        }
        
        // Refresh
        Repaint();
    }
    
    void SetupAudioListenerManager()
    {
        var existing = FindFirstObjectByType<AudioListenerManager>();
        if (existing != null)
        {
            Debug.Log("AudioListenerManager already exists");
            Selection.activeGameObject = existing.gameObject;
            EditorUtility.DisplayDialog("Already Exists", 
                "AudioListenerManager already exists in the scene!", 
                "OK");
            return;
        }
        
        GameObject managerObj = new GameObject("AudioListenerManager");
        managerObj.AddComponent<AudioListenerManager>();
        
        Undo.RegisterCreatedObjectUndo(managerObj, "Create AudioListenerManager");
        
        Debug.Log("✅ Created AudioListenerManager");
        Selection.activeGameObject = managerObj;
        
        EditorUtility.DisplayDialog("Success", 
            "✅ AudioListenerManager created!\n\n" +
            "This will automatically prevent duplicate Audio Listeners in the future.", 
            "OK");
    }
}
