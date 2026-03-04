using UnityEngine;
using UnityEditor;
using System.IO;

public class RandomItemSpawnerTool : EditorWindow
{
    [MenuItem("Tools/Random Item Spawner")]
    public static void ShowWindow()
    {
        GetWindow<RandomItemSpawnerTool>("Item Spawner");
    }

    [Header("1. Khởi Tạo Prefab")]
    public ItemData itemData;
    public GameObject model3D;
    
    [Header("2. Sinh Vật Phẩm")]
    public GameObject itemPrefabToSpawn;
    public int spawnCount = 10;
    public float spawnRadius = 15f;
    public float minScale = 0.8f;
    public float maxScale = 1.2f;

    private Vector2 scrollPos;

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        GUILayout.Label("🛠️ TẠO VẬT PHẨM MỚI (Đá, Cành Cây,...)", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bước 1: Chọn data vật phẩm và mô hình 3D, sau đó bấm tạo Prefab. Sẽ tự động gài thuộc tính nhặt được!", MessageType.Info);
        
        itemData = (ItemData)EditorGUILayout.ObjectField("Item Data (Data kho đồ)", itemData, typeof(ItemData), false);
        if (itemData == null)
        {
            if (GUILayout.Button("Tạo ItemData 'Đá' Tạm Thời")) CreateTempItemData("KiemTra_Da", "Đá");
            if (GUILayout.Button("Tạo ItemData 'Cành Cây' Tạm Thời")) CreateTempItemData("KiemTra_CanhCay", "Cành Cây");
        }

        model3D = (GameObject)EditorGUILayout.ObjectField("Mô Hình 3D (Kéo gạch/đá/cây vào)", model3D, typeof(GameObject), false);

        if (GUILayout.Button(">> TẠO PREFAB CÓ THỂ NHẶT <<", GUILayout.Height(40)))
        {
            CreatePickablePrefab();
        }

        GUILayout.Space(20);

        GUILayout.Label("🌍 SINH VẬT PHẨM VÀO MAP", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Bước 2: Chọn Prefab vừa tạo, số lượng và bán kính. Tool sẽ tự rải ngẫu nhiên xung quanh Player.", MessageType.Info);

        itemPrefabToSpawn = (GameObject)EditorGUILayout.ObjectField("Prefab Cần Sinh", itemPrefabToSpawn, typeof(GameObject), false);
        spawnCount = EditorGUILayout.IntSlider("Số lượng", spawnCount, 1, 100);
        spawnRadius = EditorGUILayout.Slider("Bán kính rải", spawnRadius, 5f, 50f);
        
        EditorGUILayout.BeginHorizontal();
        minScale = EditorGUILayout.FloatField("Scale Nhỏ Nhất", minScale);
        maxScale = EditorGUILayout.FloatField("Scale Lớn Nhất", maxScale);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button(">> RẢI NGẪU NHIÊN QUANH PLAYER <<", GUILayout.Height(50)))
        {
            SpawnItems();
        }

        GUILayout.Space(10);
        
        if (GUILayout.Button("🗑️ XÓA TẤT CẢ VẬT PHẨM ĐANG RẢI TRÊN MAP", GUILayout.Height(30)))
        {
            ClearSpawnedItems();
        }

        EditorGUILayout.EndScrollView();
    }

    private void CreateTempItemData(string fileName, string itemName)
    {
        string dir = "Assets/Resources/Items";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        ItemData newData = ScriptableObject.CreateInstance<ItemData>();
        newData.itemName = itemName;
        newData.maxStackSize = 99;
        
        string path = $"{dir}/{fileName}.asset";
        AssetDatabase.CreateAsset(newData, path);
        AssetDatabase.SaveAssets();
        itemData = newData;
        Debug.Log($"Đã tạo nhanh {path}");
    }

    private void CreatePickablePrefab()
    {
        if (itemData == null || model3D == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng chọn đủ Item Data và Mô Hình 3D!", "OK");
            return;
        }

        GameObject tempObj = Instantiate(model3D);
        tempObj.name = "Pickable_" + itemData.itemName;

        // Xoá toàn bộ collider rườm rà (ở cả nắp con) để tránh lỗi Physics chặn tia quét Raycast
        Collider[] allCols = tempObj.GetComponentsInChildren<Collider>();
        foreach (Collider c in allCols)
        {
            DestroyImmediate(c);
        }

        // Tạo 1 Trigger Collider mới siêu bự để cực kì dễ nhặt cho góc nhìn thứ 1/thứ 3
        BoxCollider triggerCol = tempObj.AddComponent<BoxCollider>();
        triggerCol.isTrigger = true;
        
        Renderer[] allRends = tempObj.GetComponentsInChildren<Renderer>();
        if (allRends.Length > 0)
        {
            Bounds b = allRends[0].bounds;
            for (int i = 1; i < allRends.Length; i++) b.Encapsulate(allRends[i].bounds);
            
            triggerCol.center = tempObj.transform.InverseTransformPoint(b.center);
            // Phóng to kích thước collider gốc nhằm tối đa hoá vùng nhìn chuột
            Vector3 rawSize = tempObj.transform.InverseTransformVector(b.size);
            triggerCol.size = new Vector3(Mathf.Abs(rawSize.x), Mathf.Abs(rawSize.y), Mathf.Abs(rawSize.z)) * 2f; 
            
            // Xả giới hạn bé nhất, tránh vật quá nhỏ (như chiếc nhẫn/hạt mầm) làm tịt hitbox
            triggerCol.size = Vector3.Max(triggerCol.size, Vector3.one * 1.5f);
        }
        else
        {
            triggerCol.size = Vector3.one * 1.5f;
        }

        // Gắn script nhặt
        PickableItem pickable = tempObj.GetComponent<PickableItem>();
        if (pickable == null) pickable = tempObj.AddComponent<PickableItem>();
        pickable.itemData = itemData;
        pickable.quantity = 1;
        pickable.autoRotate = true;

        // Bật phát sáng màu vàng kim khi đứng gần
        pickable.highlightColor = new Color(1f, 0.84f, 0f, 1f); // Gold color
        
        // Custom Material Setup for Emission
        Renderer[] renderers = tempObj.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            if (rend.sharedMaterial != null)
            {
                // Create unique instance of material so they can glow independently
                Material instancedMat = new Material(rend.sharedMaterial);
                // Enable emission keyword just in case the shader supports it
                instancedMat.EnableKeyword("_EMISSION");
                rend.sharedMaterial = instancedMat;
            }
        }

        // Sang Layer Item để tia Raycast dễ quét
        int itemLayer = LayerMask.NameToLayer("Item");
        if (itemLayer > -1)
        {
            tempObj.layer = itemLayer;
            foreach (Transform child in tempObj.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = itemLayer;
            }
        }

        // Lưu thành Prefab
        string dir = "Assets/Prefabs/Items";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        
        string prefabPath = $"{dir}/Pickable_{itemData.itemName.Replace(" ", "")}.prefab";
        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempObj, prefabPath);
        DestroyImmediate(tempObj);

        itemPrefabToSpawn = savedPrefab;
        EditorUtility.DisplayDialog("Thành công", $"Đã tạo cấu hình đồ có thể nhặt được và lưu tại:\n{prefabPath}", "Tuyệt!");
    }

    private void SpawnItems()
    {
        if (itemPrefabToSpawn == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng kéo Prefab Cần Sinh vào ô trống!", "OK");
            return;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");

        Vector3 centerPos = player != null ? player.transform.position : Vector3.zero;

        GameObject parentObj = GameObject.Find("SpawnedItems");
        if (parentObj == null) parentObj = new GameObject("SpawnedItems");

        int spawned = 0;
        for (int i = 0; i < spawnCount * 5; i++) // Thử gấp 5 lần số lượng để đảm bảo rớt xuống đất an toàn
        {
            if (spawned >= spawnCount) break;

            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            Vector3 randomPos = centerPos + new Vector3(randomCircle.x, 100f, randomCircle.y);

            // Bắn tia xuống tìm mặt đất
            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 200f))
            {
                // Kiểm tra tránh đụng layer Item (đừng đẻ chồng lên nhau)
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Item")) continue;

                GameObject newItem = (GameObject)PrefabUtility.InstantiatePrefab(itemPrefabToSpawn);
                newItem.transform.position = hit.point + Vector3.up * 0.2f;
                newItem.transform.SetParent(parentObj.transform);

                float scale = Random.Range(minScale, maxScale);
                newItem.transform.localScale = Vector3.one * scale;
                newItem.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);

                Undo.RegisterCreatedObjectUndo(newItem, "Spawn Random Item");
                spawned++;
            }
        }

        Debug.Log($"Đã rải thành công {spawned} vật phẩm ngẫu nhiên quanh tọa độ {centerPos}");
        Selection.activeGameObject = parentObj;
    }

    private void ClearSpawnedItems()
    {
        GameObject parentObj = GameObject.Find("SpawnedItems");
        if (parentObj != null)
        {
            if (EditorUtility.DisplayDialog("Cảnh báo", $"Bạn có chắc muốn xóa tất cả {parentObj.transform.childCount} vật phẩm đang rải trên mặt đất không?", "Xóa Hết", "Hủy"))
            {
                Undo.DestroyObjectImmediate(parentObj);
                Debug.Log("🗑️ Đã DON DẸP SẠCH SẼ các vật phẩm rải rác trên bản đồ!");
            }
        }
        else
        {
            EditorUtility.DisplayDialog("Tin chuẩn", "Bản đồ sạch bóng, không có vật phẩm rác nào cần dọn!", "OK");
        }
    }
}
