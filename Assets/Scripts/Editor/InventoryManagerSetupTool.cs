using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool tự động tạo và setup InventoryManager trong scene
/// </summary>
public class InventoryManagerSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Inventory Manager")]
    public static void ShowWindow()
    {
        GetWindow<InventoryManagerSetupTool>("Inventory Manager Setup");
    }

    private int inventorySize = 32;

    void OnGUI()
    {
        GUILayout.Label("Inventory Manager Setup Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // Check if already exists
        InventoryManager existing = FindFirstObjectByType<InventoryManager>();
        if (existing != null)
        {
            EditorGUILayout.HelpBox($"InventoryManager đã tồn tại trên GameObject: {existing.gameObject.name}", MessageType.Info);
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Select Existing InventoryManager"))
            {
                Selection.activeGameObject = existing.gameObject;
            }
            
            if (GUILayout.Button("Delete and Recreate", GUILayout.Width(150)))
            {
                if (EditorUtility.DisplayDialog("Confirm Delete", 
                    "Bạn có chắc muốn xóa InventoryManager hiện tại và tạo mới?\n\nCảnh báo: Dữ liệu inventory trong scene sẽ bị mất!", 
                    "Yes, Delete", "Cancel"))
                {
                    DestroyImmediate(existing.gameObject);
                    CreateInventoryManager();
                }
            }
            GUILayout.EndHorizontal();
            
            return;
        }

        EditorGUILayout.HelpBox("InventoryManager chưa tồn tại trong scene.\n\nTool này sẽ tạo một GameObject với component InventoryManager.", MessageType.Warning);
        
        GUILayout.Space(10);
        
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        inventorySize = EditorGUILayout.IntSlider("Inventory Size (Slots)", inventorySize, 5, 50);
        
        GUILayout.Space(20);

        if (GUILayout.Button("Create Inventory Manager", GUILayout.Height(40)))
        {
            CreateInventoryManager();
        }
    }

    void CreateInventoryManager()
    {
        // Create GameObject
        GameObject managerObj = new GameObject("InventoryManager");
        
        // Add InventoryManager component
        InventoryManager manager = managerObj.AddComponent<InventoryManager>();
        
        // Set inventory size using SerializedObject
        SerializedObject serializedManager = new SerializedObject(manager);
        serializedManager.FindProperty("inventorySize").intValue = inventorySize;
        serializedManager.ApplyModifiedProperties();
        
        // Select the created object
        Selection.activeGameObject = managerObj;
        
        Debug.Log($"✓ InventoryManager đã được tạo thành công với {inventorySize} slots!");
        
        EditorUtility.DisplayDialog("Success", 
            $"InventoryManager đã được tạo thành công!\n\n" +
            $"GameObject: InventoryManager\n" +
            $"Inventory Size: {inventorySize} slots\n\n" +
            "GameObject này sẽ tự động DontDestroyOnLoad.", 
            "OK");
    }
}
