using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool để fix tất cả PickableItem trong scene thiếu ItemData
/// Menu: Tools/Fix All Pickable Items
/// </summary>
public class PickableItemFixerTool : EditorWindow
{
    [MenuItem("Tools/Fix All Pickable Items")]
    static void ShowWindow()
    {
        var window = GetWindow<PickableItemFixerTool>("Pickable Item Fixer");
        window.minSize = new Vector2(400, 300);
        window.Show();
    }
    
    private Vector2 scrollPos;
    
    void OnGUI()
    {
        GUILayout.Label("Pickable Item Fixer Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tool này sẽ tìm tất cả PickableItem trong scene và tự động assign ItemData dựa trên tên GameObject.\n\n" +
            "Ví dụ:\n" +
            "• GameObject 'PickableStone' → Assign 'Stone.asset'\n" +
            "• GameObject 'Rock' → Assign 'Rock.asset'\n" +
            "• GameObject 'Wood' → Assign 'Wood.asset'",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔍 Scan Scene for Pickable Items", GUILayout.Height(40)))
        {
            ScanAndFixPickableItems();
        }
        
        GUILayout.Space(10);
        
        EditorGUILayout.LabelField("Manual Assignment:", EditorStyles.boldLabel);
        
        EditorGUILayout.HelpBox(
            "Nếu auto-fix không hoạt động, bạn có thể assign thủ công:\n\n" +
            "1. Chọn PickableStone trong Hierarchy\n" +
            "2. Inspector → PickableItem component\n" +
            "3. Kéo file 'Stone.asset' từ Resources/Items/ vào field 'Item Data'",
            MessageType.None
        );
    }
    
    void ScanAndFixPickableItems()
    {
        var pickableItems = FindObjectsByType<PickableItem>(FindObjectsSortMode.None);
        
        if (pickableItems.Length == 0)
        {
            EditorUtility.DisplayDialog("No Items Found", "Không tìm thấy PickableItem nào trong scene!", "OK");
            return;
        }
        
        int fixedCount = 0;
        int alreadyOkCount = 0;
        int failedCount = 0;
        
        foreach (var item in pickableItems)
        {
            if (item.itemData != null)
            {
                Debug.Log($"✓ {item.gameObject.name} already has ItemData: {item.itemData.itemName}");
                alreadyOkCount++;
                continue;
            }
            
            // Try to find matching ItemData
            ItemData foundData = TryFindItemData(item.gameObject.name);
            
            if (foundData != null)
            {
                // Assign using SerializedObject
                SerializedObject serializedItem = new SerializedObject(item);
                serializedItem.FindProperty("itemData").objectReferenceValue = foundData;
                serializedItem.ApplyModifiedProperties();
                
                EditorUtility.SetDirty(item);
                
                Debug.Log($"✓ Fixed {item.gameObject.name} → Assigned {foundData.itemName}");
                fixedCount++;
            }
            else
            {
                Debug.LogWarning($"⚠ Could not find ItemData for {item.gameObject.name}");
                failedCount++;
            }
        }
        
        string message = $"Scan complete!\n\n" +
                        $"✅ Fixed: {fixedCount}\n" +
                        $"✓ Already OK: {alreadyOkCount}\n" +
                        $"⚠ Failed: {failedCount}\n\n" +
                        $"Total items: {pickableItems.Length}";
        
        EditorUtility.DisplayDialog("Scan Complete", message, "OK");
        
        if (fixedCount > 0)
        {
            Debug.Log($"[PickableItemFixer] ✅ Fixed {fixedCount} items! Don't forget to save the scene.");
        }
    }
    
    ItemData TryFindItemData(string gameObjectName)
    {
        // Common mappings
        string[] possibleNames = new string[]
        {
            gameObjectName.ToLower(),
            gameObjectName.Replace("Pickable", "").ToLower(),
            gameObjectName.Replace("pickable", "").ToLower(),
            gameObjectName.Replace("Item", "").ToLower(),
            gameObjectName.Replace("item", "").ToLower(),
        };
        
        // Try to load from Resources/Items/
        foreach (var name in possibleNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            
            // Capitalize first letter
            string capitalizedName = char.ToUpper(name[0]) + name.Substring(1);
            
            ItemData data = Resources.Load<ItemData>($"Items/{capitalizedName}");
            if (data != null)
            {
                return data;
            }
        }
        
        // Try ScriptableObjects/Items/
        foreach (var name in possibleNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            
            string capitalizedName = char.ToUpper(name[0]) + name.Substring(1);
            
            // Search in project
            string[] guids = AssetDatabase.FindAssets($"{capitalizedName} t:ItemData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                ItemData data = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (data != null)
                {
                    return data;
                }
            }
        }
        
        return null;
    }
}
