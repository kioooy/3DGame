using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool so sánh PickableStone vs PickableRock để tìm khác biệt
/// Menu: Tools/Compare Rock Prefabs
/// </summary>
public class CompareRockPrefabsTool : EditorWindow
{
    [MenuItem("Tools/Compare Rock Prefabs")]
    static void ShowWindow()
    {
        var window = GetWindow<CompareRockPrefabsTool>("Compare Prefabs");
        window.minSize = new Vector2(500, 400);
        window.Show();
    }
    
    void OnGUI()
    {
        GUILayout.Label("Compare Rock Prefabs", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tool này sẽ so sánh PickableStone (working) vs PickableRock (not working)",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔍 Compare Prefabs", GUILayout.Height(50)))
        {
            ComparePrefabs();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🔧 Fix PickableRock Prefab", GUILayout.Height(50)))
        {
            FixPickableRockPrefab();
        }
    }
    
    void ComparePrefabs()
    {
        GameObject pickableStone = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/PickableStone.prefab");
        GameObject pickableRock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/PickableRock.prefab");
        
        if (pickableStone == null)
        {
            Debug.LogError("❌ PickableStone.prefab not found!");
            return;
        }
        
        if (pickableRock == null)
        {
            Debug.LogError("❌ PickableRock.prefab not found!");
            return;
        }
        
        Debug.Log("=== Comparing PickableStone vs PickableRock ===\n");
        
        // Compare PickableItem
        var stonePickable = pickableStone.GetComponent<PickableItem>();
        var rockPickable = pickableRock.GetComponent<PickableItem>();
        
        Debug.Log("--- PickableItem Component ---");
        Debug.Log($"PickableStone: {(stonePickable != null ? "✓" : "✗")}");
        Debug.Log($"PickableRock: {(rockPickable != null ? "✓" : "✗")}");
        
        if (stonePickable != null)
        {
            Debug.Log($"  Stone ItemData: {(stonePickable.itemData != null ? stonePickable.itemData.itemName : "NULL")}");
            Debug.Log($"  Stone Quantity: {stonePickable.quantity}");
        }
        
        if (rockPickable != null)
        {
            Debug.Log($"  Rock ItemData: {(rockPickable.itemData != null ? rockPickable.itemData.itemName : "NULL")}");
            Debug.Log($"  Rock Quantity: {rockPickable.quantity}");
        }
        
        // Compare Collider
        var stoneCol = pickableStone.GetComponent<Collider>();
        var rockCol = pickableRock.GetComponent<Collider>();
        
        Debug.Log("\n--- Collider ---");
        Debug.Log($"PickableStone: {(stoneCol != null ? stoneCol.GetType().Name : "NONE")}");
        if (stoneCol != null) Debug.Log($"  Is Trigger: {stoneCol.isTrigger}");
        
        Debug.Log($"PickableRock: {(rockCol != null ? rockCol.GetType().Name : "NONE")}");
        if (rockCol != null) Debug.Log($"  Is Trigger: {rockCol.isTrigger}");
        
        // Compare Layer
        Debug.Log("\n--- Layer ---");
        Debug.Log($"PickableStone: {LayerMask.LayerToName(pickableStone.layer)} ({pickableStone.layer})");
        Debug.Log($"PickableRock: {LayerMask.LayerToName(pickableRock.layer)} ({pickableRock.layer})");
        
        // Compare hierarchy
        Debug.Log("\n--- Hierarchy ---");
        Debug.Log($"PickableStone children: {pickableStone.transform.childCount}");
        for (int i = 0; i < pickableStone.transform.childCount; i++)
        {
            Debug.Log($"  - {pickableStone.transform.GetChild(i).name}");
        }
        
        Debug.Log($"PickableRock children: {pickableRock.transform.childCount}");
        for (int i = 0; i < pickableRock.transform.childCount; i++)
        {
            Debug.Log($"  - {pickableRock.transform.GetChild(i).name}");
        }
        
        EditorUtility.DisplayDialog("Comparison Complete",
            "Check Console for detailed comparison!",
            "OK");
    }
    
    void FixPickableRockPrefab()
    {
        GameObject pickableStone = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/PickableStone.prefab");
        GameObject pickableRock = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/PickableRock.prefab");
        
        if (pickableStone == null || pickableRock == null)
        {
            EditorUtility.DisplayDialog("Error", "Prefabs not found!", "OK");
            return;
        }
        
        // Get Stone settings
        var stonePickable = pickableStone.GetComponent<PickableItem>();
        var stoneCol = pickableStone.GetComponent<Collider>();
        
        if (stonePickable == null)
        {
            EditorUtility.DisplayDialog("Error", "PickableStone has no PickableItem!", "OK");
            return;
        }
        
        // Apply to Rock prefab
        string prefabPath = "Assets/Prefabs/Items/PickableRock.prefab";
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabPath);
        
        // Ensure PickableItem
        var rockPickable = prefabRoot.GetComponent<PickableItem>();
        if (rockPickable == null)
        {
            rockPickable = prefabRoot.AddComponent<PickableItem>();
        }
        
        // Copy settings from Stone
        SerializedObject serializedRock = new SerializedObject(rockPickable);
        serializedRock.FindProperty("itemData").objectReferenceValue = stonePickable.itemData;
        serializedRock.FindProperty("quantity").intValue = stonePickable.quantity;
        serializedRock.FindProperty("autoRotate").boolValue = stonePickable.autoRotate;
        serializedRock.FindProperty("rotationSpeed").floatValue = stonePickable.rotationSpeed;
        serializedRock.ApplyModifiedProperties();
        
        // Ensure collider matches
        var rockCol = prefabRoot.GetComponent<Collider>();
        if (rockCol != null && stoneCol != null)
        {
            rockCol.isTrigger = stoneCol.isTrigger;
        }
        
        // Match layer
        prefabRoot.layer = pickableStone.layer;
        
        PrefabUtility.SaveAsPrefabAsset(prefabRoot, prefabPath);
        PrefabUtility.UnloadPrefabContents(prefabRoot);
        
        Debug.Log("✅ Fixed PickableRock prefab to match PickableStone!");
        
        EditorUtility.DisplayDialog("Success!",
            "✅ PickableRock prefab fixed!\n\n" +
            "Bây giờ spawn lại rocks để test!",
            "OK");
    }
}
