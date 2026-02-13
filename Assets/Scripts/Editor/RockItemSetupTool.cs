using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool để setup PickableItem cho rocks trong scene
/// </summary>
public class RockItemSetupTool : EditorWindow
{
    [MenuItem("Tools/Setup Rock Items")]
    public static void ShowWindow()
    {
        GetWindow<RockItemSetupTool>("Rock Item Setup");
    }

    private ItemData rockItemData;
    private int quantity = 1;
    private Vector2 scrollPosition;

    void OnGUI()
    {
        GUILayout.Label("Rock Item Setup Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox("Tool này sẽ tìm tất cả rocks trong scene và setup PickableItem component cho chúng.", MessageType.Info);
        
        GUILayout.Space(10);
        
        GUILayout.Label("Settings", EditorStyles.boldLabel);
        rockItemData = (ItemData)EditorGUILayout.ObjectField("Rock ItemData", rockItemData, typeof(ItemData), false);
        quantity = EditorGUILayout.IntField("Quantity", quantity);

        if (rockItemData == null)
        {
            EditorGUILayout.HelpBox("Bạn cần tạo ItemData cho đá trước!\nVào menu: Tools → Create Item", MessageType.Warning);
        }

        GUILayout.Space(10);

        // Find all rocks in scene
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int rockCount = 0;
        int setupCount = 0;

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
        
        GUILayout.Label("Rocks in Scene:", EditorStyles.boldLabel);
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Rock") || obj.name.Contains("rock"))
            {
                rockCount++;
                PickableItem pickable = obj.GetComponent<PickableItem>();
                
                GUILayout.BeginHorizontal(EditorStyles.helpBox);
                
                if (pickable != null && pickable.itemData != null)
                {
                    GUI.color = Color.green;
                    GUILayout.Label($"✓ {obj.name}", GUILayout.Width(200));
                    GUILayout.Label($"ItemData: {pickable.itemData.itemName}", GUILayout.Width(150));
                    setupCount++;
                }
                else if (pickable != null && pickable.itemData == null)
                {
                    GUI.color = Color.yellow;
                    GUILayout.Label($"⚠ {obj.name}", GUILayout.Width(200));
                    GUILayout.Label("Missing ItemData!", GUILayout.Width(150));
                }
                else
                {
                    GUI.color = Color.red;
                    GUILayout.Label($"✗ {obj.name}", GUILayout.Width(200));
                    GUILayout.Label("No PickableItem", GUILayout.Width(150));
                }
                
                GUI.color = Color.white;
                
                if (GUILayout.Button("Select", GUILayout.Width(60)))
                {
                    Selection.activeGameObject = obj;
                }
                
                GUILayout.EndHorizontal();
            }
        }
        
        GUILayout.EndScrollView();

        GUILayout.Space(10);
        GUILayout.Label($"Total Rocks: {rockCount} | Setup Complete: {setupCount}", EditorStyles.boldLabel);
        
        GUILayout.Space(10);

        GUI.enabled = rockItemData != null;
        
        if (GUILayout.Button("Setup All Rocks", GUILayout.Height(40)))
        {
            SetupAllRocks();
        }
        
        GUI.enabled = true;

        GUILayout.Space(10);

        if (GUILayout.Button("Create Rock ItemData", GUILayout.Height(30)))
        {
            CreateRockItemData();
        }
    }

    void SetupAllRocks()
    {
        if (rockItemData == null)
        {
            EditorUtility.DisplayDialog("Error", "Vui lòng assign Rock ItemData trước!", "OK");
            return;
        }

        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int setupCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Rock") || obj.name.Contains("rock"))
            {
                // Add or get PickableItem component
                PickableItem pickable = obj.GetComponent<PickableItem>();
                if (pickable == null)
                {
                    pickable = obj.AddComponent<PickableItem>();
                }

                // Assign ItemData
                pickable.itemData = rockItemData;
                pickable.quantity = quantity;

                // Ensure collider exists and is trigger
                Collider col = obj.GetComponent<Collider>();
                if (col == null)
                {
                    // Add box collider if no collider exists
                    col = obj.AddComponent<BoxCollider>();
                }
                col.isTrigger = true;

                EditorUtility.SetDirty(obj);
                setupCount++;
            }
        }

        EditorUtility.DisplayDialog("Success", 
            $"Đã setup {setupCount} rocks thành công!\n\n" +
            $"ItemData: {rockItemData.itemName}\n" +
            $"Quantity: {quantity}", 
            "OK");
        
        Debug.Log($"✓ Setup {setupCount} rocks với ItemData: {rockItemData.itemName}");
    }

    void CreateRockItemData()
    {
        // Create ItemData asset
        ItemData newItem = ScriptableObject.CreateInstance<ItemData>();
        
        // Set default values for rock
        newItem.itemName = "Đá";
        newItem.description = "Một viên đá thông thường. Có thể dùng để chế tạo công cụ.";
        newItem.itemType = ItemType.Material;
        newItem.maxStackSize = 99;

        // Save asset
        string path = "Assets/ScriptableObjects/Items/Rock.asset";
        
        // Ensure directory exists
        string directory = System.IO.Path.GetDirectoryName(path);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        AssetDatabase.CreateAsset(newItem, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Select the created asset
        Selection.activeObject = newItem;
        EditorGUIUtility.PingObject(newItem);

        // Auto-assign to the field
        rockItemData = newItem;

        EditorUtility.DisplayDialog("Success", 
            $"Đã tạo Rock ItemData tại:\n{path}\n\n" +
            "Bạn có thể chỉnh sửa icon và các thuộc tính khác trong Inspector.", 
            "OK");
        
        Debug.Log($"✓ Created Rock ItemData at: {path}");
    }
}
