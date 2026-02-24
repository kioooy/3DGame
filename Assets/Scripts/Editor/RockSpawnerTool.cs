using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Tool để tạo pickable rock prefab và spawn nhiều đá trên terrain
/// Menu: Tools/Rock Spawner
/// </summary>
public class RockSpawnerTool : EditorWindow
{
    [MenuItem("Tools/Rock Spawner")]
    static void ShowWindow()
    {
        var window = GetWindow<RockSpawnerTool>("Rock Spawner");
        window.minSize = new Vector2(400, 600);
        window.Show();
    }
    
    private GameObject rockModel;
    private ItemData stoneItemData;
    private Terrain targetTerrain;
    
    private int spawnCount = 50;
    private float minScale = 0.8f;
    private float maxScale = 1.5f;
    private float randomRotation = 180f;
    private LayerMask terrainLayer;
    
    void OnGUI()
    {
        GUILayout.Label("Rock Spawner Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Tool này sẽ:\n" +
            "1. Tạo Pickable Rock Prefab từ model\n" +
            "2. Spawn nhiều đá ngẫu nhiên trên terrain\n" +
            "3. Tự động add PickableItem component",
            MessageType.Info
        );
        
        GUILayout.Space(10);
        
        // Step 1: Create Prefab
        GUILayout.Label("Step 1: Create Pickable Rock Prefab", EditorStyles.boldLabel);
        
        rockModel = (GameObject)EditorGUILayout.ObjectField("Rock Model", rockModel, typeof(GameObject), false);
        stoneItemData = (ItemData)EditorGUILayout.ObjectField("Stone ItemData", stoneItemData, typeof(ItemData), false);
        
        if (GUILayout.Button("🔨 Create Pickable Rock Prefab", GUILayout.Height(40)))
        {
            CreatePickableRockPrefab();
        }
        
        GUILayout.Space(20);
        
        // Step 2: Spawn Rocks
        GUILayout.Label("Step 2: Spawn Rocks on Terrain", EditorStyles.boldLabel);
        
        targetTerrain = (Terrain)EditorGUILayout.ObjectField("Target Terrain", targetTerrain, typeof(Terrain), true);
        
        spawnCount = EditorGUILayout.IntSlider("Spawn Count", spawnCount, 1, 500);
        
        GUILayout.Space(5);
        GUILayout.Label("Scale Variation", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        minScale = EditorGUILayout.FloatField("Min", minScale);
        maxScale = EditorGUILayout.FloatField("Max", maxScale);
        EditorGUILayout.EndHorizontal();
        
        randomRotation = EditorGUILayout.Slider("Random Rotation", randomRotation, 0f, 360f);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🌍 Spawn Rocks on Terrain", GUILayout.Height(40)))
        {
            SpawnRocksOnTerrain();
        }
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("🗑️ Clear All Spawned Rocks"))
        {
            ClearSpawnedRocks();
        }
    }
    
    void CreatePickableRockPrefab()
    {
        if (rockModel == null)
        {
            EditorUtility.DisplayDialog("Error", "Vui lòng chọn Rock Model!", "OK");
            return;
        }
        
        if (stoneItemData == null)
        {
            // Try to find Stone.asset
            string[] guids = AssetDatabase.FindAssets("Stone t:ItemData");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                stoneItemData = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                Debug.Log($"Auto-found Stone ItemData: {path}");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Không tìm thấy Stone ItemData!", "OK");
                return;
            }
        }
        
        // Create prefab
        GameObject rockPrefab = Instantiate(rockModel);
        rockPrefab.name = "PickableRock";
        
        // Ensure collider FIRST (PickableItem requires it)
        Collider col = rockPrefab.GetComponent<Collider>();
        if (col == null)
        {
            // Use BoxCollider instead of MeshCollider to avoid ground check issues
            // BoxCollider is smaller and more predictable
            BoxCollider boxCol = rockPrefab.AddComponent<BoxCollider>();
            
            // Make collider smaller to avoid interfering with ground check
            boxCol.size = Vector3.one * 0.5f; // Smaller collider
            boxCol.center = Vector3.up * 0.25f; // Lift it up a bit
            
            Debug.Log("Added small BoxCollider");
        }
        else
        {
            // If has MeshCollider, replace with BoxCollider
            if (col is MeshCollider)
            {
                DestroyImmediate(col);
                BoxCollider boxCol = rockPrefab.AddComponent<BoxCollider>();
                boxCol.size = Vector3.one * 0.5f;
                boxCol.center = Vector3.up * 0.25f;
                Debug.Log("Replaced MeshCollider with BoxCollider");
            }
        }
        
        // NOW add PickableItem component (after collider)
        PickableItem pickable = rockPrefab.GetComponent<PickableItem>();
        if (pickable == null)
        {
            pickable = rockPrefab.AddComponent<PickableItem>();
        }
        
        // Assign ItemData
        SerializedObject serializedPickable = new SerializedObject(pickable);
        serializedPickable.FindProperty("itemData").objectReferenceValue = stoneItemData;
        serializedPickable.FindProperty("quantity").intValue = 1;
        serializedPickable.ApplyModifiedProperties();
        
        // Don't set to Item layer - keep as Default to avoid ground check issues
        // Items will be detected by raycast regardless of layer
        
        // Save as prefab
        string prefabPath = "Assets/Prefabs/Items/PickableRock.prefab";
        System.IO.Directory.CreateDirectory("Assets/Prefabs/Items");
        
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(rockPrefab, prefabPath);
        
        DestroyImmediate(rockPrefab);
        
        Debug.Log($"✅ Created Pickable Rock Prefab at: {prefabPath}");
        
        EditorUtility.DisplayDialog("Success",
            $"✅ Pickable Rock Prefab created!\n\n" +
            $"Path: {prefabPath}\n\n" +
            "Bây giờ bạn có thể spawn rocks trên terrain!",
            "OK");
        
        Selection.activeObject = savedPrefab;
    }
    
    void SpawnRocksOnTerrain()
    {
        if (targetTerrain == null)
        {
            targetTerrain = FindFirstObjectByType<Terrain>();
            if (targetTerrain == null)
            {
                EditorUtility.DisplayDialog("Error", "Không tìm thấy Terrain!", "OK");
                return;
            }
        }
        
        // Load prefab
        GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Items/PickableRock.prefab");
        if (rockPrefab == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Chưa có PickableRock prefab!\n\n" +
                "Hãy tạo prefab trước (Step 1)",
                "OK");
            return;
        }
        
        // Create parent object
        GameObject rocksParent = GameObject.Find("SpawnedRocks");
        if (rocksParent == null)
        {
            rocksParent = new GameObject("SpawnedRocks");
            Undo.RegisterCreatedObjectUndo(rocksParent, "Create Rocks Parent");
        }
        
        TerrainData terrainData = targetTerrain.terrainData;
        Vector3 terrainPos = targetTerrain.transform.position;
        Vector3 terrainSize = terrainData.size;
        
        int spawned = 0;
        int attempts = 0;
        int maxAttempts = spawnCount * 3;
        
        while (spawned < spawnCount && attempts < maxAttempts)
        {
            attempts++;
            
            // Random position on terrain
            float x = Random.Range(0f, terrainSize.x);
            float z = Random.Range(0f, terrainSize.z);
            
            // Get terrain height
            float normalizedX = x / terrainSize.x;
            float normalizedZ = z / terrainSize.z;
            float y = terrainData.GetInterpolatedHeight(normalizedX, normalizedZ);
            
            Vector3 spawnPos = terrainPos + new Vector3(x, y, z);
            
            // Raycast to ensure on ground
            if (Physics.Raycast(spawnPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
            {
                spawnPos = hit.point;
            }
            
            // Spawn rock
            GameObject rock = (GameObject)PrefabUtility.InstantiatePrefab(rockPrefab);
            rock.transform.position = spawnPos;
            rock.transform.parent = rocksParent.transform;
            
            // Random scale
            float scale = Random.Range(minScale, maxScale);
            rock.transform.localScale = Vector3.one * scale;
            
            // Random rotation
            float yRotation = Random.Range(-randomRotation, randomRotation);
            rock.transform.rotation = Quaternion.Euler(0, yRotation, 0);
            
            Undo.RegisterCreatedObjectUndo(rock, "Spawn Rock");
            
            spawned++;
        }
        
        Debug.Log($"✅ Spawned {spawned} rocks on terrain!");
        
        EditorUtility.DisplayDialog("Success",
            $"✅ Spawned {spawned} rocks!\n\n" +
            "Rocks are grouped under 'SpawnedRocks' GameObject",
            "OK");
        
        Selection.activeGameObject = rocksParent;
    }
    
    void ClearSpawnedRocks()
    {
        GameObject rocksParent = GameObject.Find("SpawnedRocks");
        if (rocksParent != null)
        {
            if (EditorUtility.DisplayDialog("Confirm",
                $"Xóa tất cả {rocksParent.transform.childCount} rocks?",
                "Yes", "Cancel"))
            {
                Undo.DestroyObjectImmediate(rocksParent);
                Debug.Log("✅ Cleared all spawned rocks");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Info", "Không có rocks để xóa", "OK");
        }
    }
}
