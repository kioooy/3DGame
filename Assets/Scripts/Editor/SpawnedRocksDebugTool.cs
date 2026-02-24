using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool debug và fix rocks spawn từ RockSpawnerTool
/// Menu: Tools/Debug Spawned Rocks
/// </summary>
public class SpawnedRocksDebugTool : EditorWindow
{
    [MenuItem("Tools/Debug Spawned Rocks")]
    static void ShowWindow()
    {
        var window = GetWindow<SpawnedRocksDebugTool>("Debug Spawned Rocks");
        window.minSize = new Vector2(450, 400);
        window.Show();
    }
    
    private Vector2 scrollPos;
    
    void OnGUI()
    {
        GUILayout.Label("Debug Spawned Rocks", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tool này sẽ kiểm tra và fix tất cả rocks spawn từ RockSpawnerTool",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔍 Check All Spawned Rocks", GUILayout.Height(40)))
        {
            CheckSpawnedRocks();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("🔧 Fix All Spawned Rocks", GUILayout.Height(50)))
        {
            FixAllSpawnedRocks();
        }
        
        GUILayout.Space(5);
        
        if (GUILayout.Button("🗑️ Delete All Spawned Rocks"))
        {
            DeleteAllSpawnedRocks();
        }
    }
    
    void CheckSpawnedRocks()
    {
        GameObject rocksParent = GameObject.Find("SpawnedRocks");
        if (rocksParent == null)
        {
            Debug.LogWarning("Không tìm thấy SpawnedRocks parent!");
            EditorUtility.DisplayDialog("Info", "Không tìm thấy rocks đã spawn", "OK");
            return;
        }
        
        int totalRocks = rocksParent.transform.childCount;
        int missingPickable = 0;
        int missingItemData = 0;
        int missingCollider = 0;
        int notTrigger = 0;
        
        Debug.Log($"=== Checking {totalRocks} spawned rocks ===");
        
        for (int i = 0; i < rocksParent.transform.childCount; i++)
        {
            GameObject rock = rocksParent.transform.GetChild(i).gameObject;
            
            // Check PickableItem
            var pickable = rock.GetComponent<PickableItem>();
            if (pickable == null)
            {
                Debug.LogError($"❌ {rock.name}: Missing PickableItem component!");
                missingPickable++;
                continue;
            }
            
            // Check ItemData
            if (pickable.itemData == null)
            {
                Debug.LogError($"❌ {rock.name}: PickableItem has no ItemData!");
                missingItemData++;
            }
            
            // Check Collider
            var col = rock.GetComponent<Collider>();
            if (col == null)
            {
                Debug.LogError($"❌ {rock.name}: Missing Collider!");
                missingCollider++;
            }
            else if (!col.isTrigger)
            {
                Debug.LogWarning($"⚠️ {rock.name}: Collider is not trigger!");
                notTrigger++;
            }
        }
        
        Debug.Log($"\n=== Summary ===");
        Debug.Log($"Total rocks: {totalRocks}");
        Debug.Log($"Missing PickableItem: {missingPickable}");
        Debug.Log($"Missing ItemData: {missingItemData}");
        Debug.Log($"Missing Collider: {missingCollider}");
        Debug.Log($"Not trigger: {notTrigger}");
        
        string message = $"Total: {totalRocks} rocks\n\n";
        if (missingPickable > 0) message += $"❌ Missing PickableItem: {missingPickable}\n";
        if (missingItemData > 0) message += $"❌ Missing ItemData: {missingItemData}\n";
        if (missingCollider > 0) message += $"❌ Missing Collider: {missingCollider}\n";
        if (notTrigger > 0) message += $"⚠️ Not trigger: {notTrigger}\n";
        
        if (missingPickable == 0 && missingItemData == 0 && missingCollider == 0 && notTrigger == 0)
        {
            message += "\n✅ All rocks are OK!";
        }
        else
        {
            message += "\nClick 'Fix All' to fix issues!";
        }
        
        EditorUtility.DisplayDialog("Check Results", message, "OK");
    }
    
    void FixAllSpawnedRocks()
    {
        GameObject rocksParent = GameObject.Find("SpawnedRocks");
        if (rocksParent == null)
        {
            EditorUtility.DisplayDialog("Error", "Không tìm thấy SpawnedRocks!", "OK");
            return;
        }
        
        // Find Stone ItemData
        string[] guids = AssetDatabase.FindAssets("Stone t:ItemData");
        if (guids.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "Không tìm thấy Stone ItemData!", "OK");
            return;
        }
        
        string path = AssetDatabase.GUIDToAssetPath(guids[0]);
        ItemData stoneData = AssetDatabase.LoadAssetAtPath<ItemData>(path);
        
        int fixedCount = 0;
        int totalRocks = rocksParent.transform.childCount;
        
        for (int i = 0; i < totalRocks; i++)
        {
            GameObject rock = rocksParent.transform.GetChild(i).gameObject;
            bool changed = false;
            
            // Ensure PickableItem
            var pickable = rock.GetComponent<PickableItem>();
            if (pickable == null)
            {
                // Need collider first
                var col = rock.GetComponent<Collider>();
                if (col == null)
                {
                    col = rock.AddComponent<BoxCollider>();
                }
                col.isTrigger = true;
                
                pickable = rock.AddComponent<PickableItem>();
                changed = true;
                Debug.Log($"Added PickableItem to {rock.name}");
            }
            
            // Assign ItemData
            if (pickable.itemData == null)
            {
                SerializedObject serializedPickable = new SerializedObject(pickable);
                serializedPickable.FindProperty("itemData").objectReferenceValue = stoneData;
                serializedPickable.FindProperty("quantity").intValue = 1;
                serializedPickable.ApplyModifiedProperties();
                changed = true;
                Debug.Log($"Assigned ItemData to {rock.name}");
            }
            
            // Ensure collider is trigger
            var collider = rock.GetComponent<Collider>();
            if (collider != null && !collider.isTrigger)
            {
                collider.isTrigger = true;
                changed = true;
                Debug.Log($"Set {rock.name} collider to trigger");
            }
            
            if (changed)
            {
                EditorUtility.SetDirty(rock);
                fixedCount++;
            }
        }
        
        Debug.Log($"✅ Fixed {fixedCount}/{totalRocks} rocks!");
        
        EditorUtility.DisplayDialog("Success!",
            $"✅ Fixed {fixedCount} rocks!\n\n" +
            "Bây giờ có thể nhặt được tất cả rocks!",
            "OK");
    }
    
    void DeleteAllSpawnedRocks()
    {
        GameObject rocksParent = GameObject.Find("SpawnedRocks");
        if (rocksParent != null)
        {
            int count = rocksParent.transform.childCount;
            if (EditorUtility.DisplayDialog("Confirm",
                $"Xóa tất cả {count} rocks?",
                "Yes", "Cancel"))
            {
                DestroyImmediate(rocksParent);
                Debug.Log($"✅ Deleted {count} rocks");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "Không có rocks để xóa", "OK");
        }
    }
}
