using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool debug để kiểm tra tại sao không nhặt được đá
/// Menu: Tools/Debug Pickup Issue
/// </summary>
public class PickupDebugTool : EditorWindow
{
    [MenuItem("Tools/Debug Pickup Issue")]
    static void ShowWindow()
    {
        var window = GetWindow<PickupDebugTool>("Pickup Debug");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Pickup Debug Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tool này sẽ kiểm tra:\n" +
            "• PlayerController settings\n" +
            "• PickableRock prefab\n" +
            "• Layer settings\n" +
            "• Collider settings",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔍 Check Player Settings", GUILayout.Height(40)))
        {
            CheckPlayerSettings();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("🔍 Check PickableRock Prefab", GUILayout.Height(40)))
        {
            CheckRockPrefab();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("🔍 Check All Rocks in Scene", GUILayout.Height(40)))
        {
            CheckRocksInScene();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔧 Auto Fix All Issues", GUILayout.Height(50)))
        {
            AutoFixIssues();
        }
    }
    
    void CheckPlayerSettings()
    {
        var player = FindFirstObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("❌ PlayerController not found!");
            return;
        }
        
        SerializedObject serializedPlayer = new SerializedObject(player);
        
        var interactionRange = serializedPlayer.FindProperty("interactionRange");
        var itemLayer = serializedPlayer.FindProperty("itemLayer");
        
        Debug.Log("=== Player Settings ===");
        Debug.Log($"Interaction Range: {interactionRange.floatValue}");
        Debug.Log($"Item Layer Mask: {itemLayer.intValue}");
        
        if (itemLayer.intValue == 0)
        {
            Debug.LogWarning("⚠️ Item Layer Mask is NOTHING! Player cannot detect items!");
        }
        else
        {
            Debug.Log($"✓ Item Layer Mask is set to: {LayerMaskToString(itemLayer.intValue)}");
        }
    }
    
    void CheckRockPrefab()
    {
        GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/PickableRock.prefab");
        
        if (rockPrefab == null)
        {
            Debug.LogError("❌ PickableRock prefab not found at Assets/Prefabs/Items/PickableRock.prefab");
            return;
        }
        
        Debug.Log("=== PickableRock Prefab ===");
        
        var pickable = rockPrefab.GetComponent<PickableItem>();
        if (pickable == null)
        {
            Debug.LogError("❌ PickableItem component missing!");
        }
        else
        {
            Debug.Log($"✓ PickableItem component exists");
            Debug.Log($"  ItemData: {(pickable.itemData != null ? pickable.itemData.itemName : "NULL")}");
            Debug.Log($"  Quantity: {pickable.quantity}");
        }
        
        var collider = rockPrefab.GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError("❌ Collider missing!");
        }
        else
        {
            Debug.Log($"✓ Collider exists: {collider.GetType().Name}");
            Debug.Log($"  Is Trigger: {collider.isTrigger}");
        }
        
        Debug.Log($"Layer: {LayerMask.LayerToName(rockPrefab.layer)} ({rockPrefab.layer})");
        
        Selection.activeObject = rockPrefab;
    }
    
    void CheckRocksInScene()
    {
        var rocks = FindObjectsByType<PickableItem>(FindObjectsSortMode.None);
        
        Debug.Log($"=== Found {rocks.Length} PickableItems in scene ===");
        
        foreach (var rock in rocks)
        {
            Debug.Log($"\n{rock.gameObject.name}:");
            Debug.Log($"  Position: {rock.transform.position}");
            Debug.Log($"  Layer: {LayerMask.LayerToName(rock.gameObject.layer)} ({rock.gameObject.layer})");
            Debug.Log($"  ItemData: {(rock.itemData != null ? rock.itemData.itemName : "NULL")}");
            
            var col = rock.GetComponent<Collider>();
            if (col != null)
            {
                Debug.Log($"  Collider: {col.GetType().Name}, Trigger: {col.isTrigger}");
            }
            else
            {
                Debug.LogError($"  ❌ NO COLLIDER!");
            }
        }
    }
    
    void AutoFixIssues()
    {
        if (!EditorUtility.DisplayDialog("Auto Fix",
            "Tự động fix các vấn đề:\n\n" +
            "• Set Player itemLayer = Everything\n" +
            "• Fix all rocks in scene\n" +
            "• Ensure colliders\n\n" +
            "Continue?",
            "Yes", "Cancel"))
        {
            return;
        }
        
        int fixedCount = 0;
        
        // Fix Player
        var player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            SerializedObject serializedPlayer = new SerializedObject(player);
            var itemLayer = serializedPlayer.FindProperty("itemLayer");
            itemLayer.intValue = -1; // Everything
            serializedPlayer.ApplyModifiedProperties();
            EditorUtility.SetDirty(player);
            Debug.Log("✓ Fixed Player itemLayer to Everything");
            fixedCount++;
        }
        
        // Fix all rocks
        var rocks = FindObjectsByType<PickableItem>(FindObjectsSortMode.None);
        foreach (var rock in rocks)
        {
            bool changed = false;
            
            // Ensure collider
            var col = rock.GetComponent<Collider>();
            if (col == null)
            {
                rock.gameObject.AddComponent<BoxCollider>();
                Debug.Log($"✓ Added BoxCollider to {rock.gameObject.name}");
                changed = true;
            }
            
            // Ensure ItemData
            if (rock.itemData == null)
            {
                string[] guids = AssetDatabase.FindAssets("Stone t:ItemData");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    ItemData stoneData = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                    
                    SerializedObject serializedRock = new SerializedObject(rock);
                    serializedRock.FindProperty("itemData").objectReferenceValue = stoneData;
                    serializedRock.ApplyModifiedProperties();
                    
                    Debug.Log($"✓ Assigned Stone ItemData to {rock.gameObject.name}");
                    changed = true;
                }
            }
            
            if (changed)
            {
                EditorUtility.SetDirty(rock);
                fixedCount++;
            }
        }
        
        Debug.Log($"=== ✅ Fixed {fixedCount} issues! ===");
        
        EditorUtility.DisplayDialog("Success",
            $"✅ Fixed {fixedCount} issues!\n\n" +
            "Hãy test lại pickup!",
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
        return result;
    }
}
