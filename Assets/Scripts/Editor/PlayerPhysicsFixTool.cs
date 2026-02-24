using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool tự động fix Player settings để tránh physics issues
/// Menu: Tools/Fix Player Physics
/// </summary>
public class PlayerPhysicsFixTool : EditorWindow
{
    [MenuItem("Tools/Fix Player Physics")]
    static void ShowWindow()
    {
        var window = GetWindow<PlayerPhysicsFixTool>("Fix Player Physics");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Fix Player Physics", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tool này sẽ tự động fix:\n\n" +
            "✅ Set groundLayer = Default (chỉ detect terrain)\n" +
            "✅ Set itemLayer = Everything (detect tất cả items)\n" +
            "✅ Fix tất cả rocks trong scene về layer Default",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        var player = FindFirstObjectByType<PlayerController>();
        
        if (player == null)
        {
            EditorGUILayout.HelpBox("❌ Không tìm thấy Player!", MessageType.Error);
            return;
        }
        
        // Show current settings
        SerializedObject serializedPlayer = new SerializedObject(player);
        var groundLayer = serializedPlayer.FindProperty("groundLayer");
        var itemLayer = serializedPlayer.FindProperty("itemLayer");
        
        GUILayout.Label("Current Settings:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Ground Layer", LayerMaskToString(groundLayer.intValue));
        EditorGUILayout.LabelField("Item Layer", LayerMaskToString(itemLayer.intValue));
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔧 Auto Fix All", GUILayout.Height(50)))
        {
            AutoFix();
        }
    }
    
    void AutoFix()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            EditorUtility.DisplayDialog("Error", "Không tìm thấy Player!", "OK");
            return;
        }
        
        SerializedObject serializedPlayer = new SerializedObject(player);
        
        // Set groundLayer = Default only
        var groundLayer = serializedPlayer.FindProperty("groundLayer");
        groundLayer.intValue = LayerMask.GetMask("Default");
        
        // Set itemLayer = Everything
        var itemLayer = serializedPlayer.FindProperty("itemLayer");
        itemLayer.intValue = -1; // Everything
        
        serializedPlayer.ApplyModifiedProperties();
        EditorUtility.SetDirty(player);
        
        Debug.Log("✅ Fixed Player settings:");
        Debug.Log($"  groundLayer = Default");
        Debug.Log($"  itemLayer = Everything");
        
        // Fix all rocks to Default layer AND fix colliders
        var rocks = FindObjectsByType<PickableItem>(FindObjectsSortMode.None);
        int fixedRocks = 0;
        int fixedColliders = 0;
        
        foreach (var rock in rocks)
        {
            bool changed = false;
            
            // Fix layer
            if (rock.gameObject.layer != 0) // Not Default
            {
                rock.gameObject.layer = 0; // Default
                changed = true;
                fixedRocks++;
            }
            
            // Fix collider - ensure it's a trigger
            var col = rock.GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                changed = true;
                fixedColliders++;
                Debug.Log($"Set {rock.gameObject.name} collider to trigger");
            }
            
            if (changed)
            {
                EditorUtility.SetDirty(rock.gameObject);
            }
        }
        
        Debug.Log($"✅ Fixed {fixedRocks} rocks to Default layer");
        Debug.Log($"✅ Fixed {fixedColliders} rock colliders");
        
        EditorUtility.DisplayDialog("Success!",
            $"✅ Fixed Player Physics!\n\n" +
            $"• groundLayer = Default\n" +
            $"• itemLayer = Everything\n" +
            $"• Fixed {fixedRocks} rock layers\n" +
            $"• Fixed {fixedColliders} rock colliders\n\n" +
            "Chạy game để test!",
            "OK");
    }
    
    string LayerMaskToString(int layerMask)
    {
        if (layerMask == -1) return "Everything";
        if (layerMask == 0) return "Nothing";
        
        string result = "";
        for (int i = 0; i < 32; i++)
        {
            if ((layerMask & (1 << i)) != 0)
            {
                string layerName = LayerMask.LayerToName(i);
                if (!string.IsNullOrEmpty(layerName))
                {
                    if (result.Length > 0) result += ", ";
                    result += layerName;
                }
            }
        }
        return string.IsNullOrEmpty(result) ? "Custom" : result;
    }
}
