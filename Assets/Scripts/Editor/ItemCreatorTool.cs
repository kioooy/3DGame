using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor tool để tạo sample pickable items nhanh chóng
/// Window > Inventory > Create Sample Items
/// </summary>
public class ItemCreatorTool : EditorWindow
{
    [MenuItem("Window/Inventory/Create Sample Items")]
    static void ShowWindow()
    {
        GetWindow<ItemCreatorTool>("Item Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("Sample Item Creator", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Tool này sẽ tạo các ItemData ScriptableObjects và Prefabs mẫu.", MessageType.Info);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Create All Sample Items", GUILayout.Height(40)))
        {
            CreateAllSampleItems();
        }
        
        EditorGUILayout.Space();
        GUILayout.Label("Individual Items:", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Create Stone (Đá)", GUILayout.Height(30)))
        {
            CreateStoneItem();
        }
        
        if (GUILayout.Button("Create Wood (Gỗ)", GUILayout.Height(30)))
        {
            CreateWoodItem();
        }
        
        if (GUILayout.Button("Create Gold (Vàng)", GUILayout.Height(30)))
        {
            CreateGoldItem();
        }
        
        if (GUILayout.Button("Create Apple (Táo)", GUILayout.Height(30)))
        {
            CreateAppleItem();
        }
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Setup Item Layer", GUILayout.Height(30)))
        {
            SetupItemLayer();
        }
    }

    void CreateAllSampleItems()
    {
        CreateStoneItem();
        CreateWoodItem();
        CreateGoldItem();
        CreateAppleItem();
        SetupItemLayer();
        
        EditorUtility.DisplayDialog("Success", "Đã tạo tất cả sample items!", "OK");
    }

    void CreateStoneItem()
    {
        CreateItem("Stone", "Đá", ItemType.Resource, new Color(0.5f, 0.5f, 0.5f), 99, "Đá thô có thể dùng để chế tạo công cụ.");
    }

    void CreateWoodItem()
    {
        CreateItem("Wood", "Gỗ", ItemType.Resource, new Color(0.6f, 0.4f, 0.2f), 99, "Gỗ từ cây, nguyên liệu cơ bản để xây dựng.");
    }

    void CreateGoldItem()
    {
        CreateItem("Gold", "Vàng", ItemType.Material, new Color(1f, 0.84f, 0f), 50, "Kim loại quý hiếm, dùng để chế tạo vật phẩm cao cấp.");
    }

    void CreateAppleItem()
    {
        CreateItem("Apple", "Táo", ItemType.Consumable, new Color(1f, 0.2f, 0.2f), 20, "Táo tươi, ăn để hồi máu.");
    }

    void CreateItem(string id, string displayName, ItemType type, Color color, int maxStack, string description)
    {
        // Create directories
        System.IO.Directory.CreateDirectory("Assets/Resources/Items");
        System.IO.Directory.CreateDirectory("Assets/Prefabs/Items");
        
        // Create ItemData ScriptableObject
        ItemData itemData = ScriptableObject.CreateInstance<ItemData>();
        itemData.itemName = displayName;
        itemData.itemType = type;
        itemData.maxStackSize = maxStack;
        itemData.description = description;
        
        string itemDataPath = $"Assets/Resources/Items/{id}.asset";
        AssetDatabase.CreateAsset(itemData, itemDataPath);
        
        // Create 3D prefab
        GameObject prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prefab.name = $"Pickable{id}";
        prefab.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        // Set color
        Renderer renderer = prefab.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        renderer.material = mat;
        
        // Add PickableItem component
        PickableItem pickable = prefab.AddComponent<PickableItem>();
        pickable.itemData = itemData;
        pickable.quantity = 1;
        pickable.autoRotate = true;
        pickable.rotationSpeed = 50f;
        
        // Set layer to Item (layer 8)
        prefab.layer = 8;
        
        // Save prefab
        string prefabPath = $"Assets/Prefabs/Items/Pickable{id}.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
        
        // Update ItemData with prefab reference
        itemData.worldModelPrefab = savedPrefab;
        EditorUtility.SetDirty(itemData);
        
        DestroyImmediate(prefab);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log($"✅ Đã tạo item: {displayName} ({id})");
    }

    void SetupItemLayer()
    {
        // Add "Item" layer to layer 8 if not exists
        SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");
        
        SerializedProperty layer8 = layers.GetArrayElementAtIndex(8);
        if (string.IsNullOrEmpty(layer8.stringValue))
        {
            layer8.stringValue = "Item";
            tagManager.ApplyModifiedProperties();
            Debug.Log("✅ Đã tạo Layer 'Item' (Layer 8)");
        }
        else if (layer8.stringValue != "Item")
        {
            Debug.LogWarning($"Layer 8 đã được sử dụng cho '{layer8.stringValue}'. Vui lòng tự tạo layer 'Item' và assign vào PlayerController.");
        }
        else
        {
            Debug.Log("Layer 'Item' đã tồn tại.");
        }
    }
}
