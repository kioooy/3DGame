using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public class QuestSystemSetupTool : EditorWindow
{
    [MenuItem("Window/Quest System/1. Setup Quest UI & Pickable Stone")]
    public static void SetupEverything()
    {
        SetupQuestManager();
        SetupPickableStone();
        Debug.Log("QuestSystemSetupTool: 🚀 Hoàn thành setup Hệ thống Quest & Sinh Đá!");
    }

    private static void SetupQuestManager()
    {
        QuestUIManager questManager = Object.FindFirstObjectByType<QuestUIManager>();
        if (questManager == null)
        {
            GameObject managerObj = new GameObject("QuestManager");
            questManager = managerObj.AddComponent<QuestUIManager>();
        }
        
        // Kích hoạt hàm CreateDefaultUI để sinh ra Canvas Nhiệm Vụ
        questManager.CreateDefaultUI();
        
        EditorUtility.SetDirty(questManager);
        Debug.Log("QuestSystemSetupTool: ✅ Đã setup QuestManager và UI.");
    }

    private static void SetupPickableStone()
    {
        // 1. Tìm player để spawn đá ngay trước mặt
        GameObject player = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
        Vector3 spawnPos = new Vector3(2, 0, 2);
        if (player != null)
        {
            spawnPos = player.transform.position + player.transform.forward * 2f;
            spawnPos.y += 0.5f;
            
            // Đảm bảo Player có PlayerController setup đúng
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null)
            {
                // Cố gắng tự động assign Camera nếu thiếu
                var camField = typeof(PlayerController).GetField("cameraTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (camField != null && camField.GetValue(pc) == null && Camera.main != null)
                {
                    camField.SetValue(pc, Camera.main.transform);
                    EditorUtility.SetDirty(pc);
                    Debug.Log("QuestSystemSetupTool: Đã auto assign Camera cho PlayerController.");
                }
            }
        }

        // 2. Tạo viên đá
        GameObject stoneObj = new GameObject("Quest_Stone");
        stoneObj.transform.position = spawnPos;

        // Thêm Mesh để nhìn thấy
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.SetParent(stoneObj.transform);
        cube.transform.localPosition = Vector3.zero;
        cube.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        // Delete built-in collider of primitive
        DestroyImmediate(cube.GetComponent<Collider>());
        
        // Tạo material xám cho đá
        Renderer rend = cube.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = Color.gray;
            rend.material = mat;
        }

        // 3. Thêm ItemData nếu có, hoặc tạo mới tạm thời
        ItemData stoneData = Object.FindFirstObjectByType<ItemData>();
        if (stoneData == null)
        {
            // Tìm trong project
            string[] guids = AssetDatabase.FindAssets("t:ItemData Stone");
            if (guids.Length > 0)
            {
                stoneData = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }
            if (stoneData == null) 
            {
                stoneData = ScriptableObject.CreateInstance<ItemData>();
                stoneData.itemName = "Đá";
                stoneData.maxStackSize = 99;
                AssetDatabase.CreateAsset(stoneData, "Assets/QuestStoneData.asset");
                AssetDatabase.SaveAssets();
            }
        }

        // 4. Setup PickableItem
        PickableItem pickable = stoneObj.AddComponent<PickableItem>();
        pickable.itemData = stoneData;
        pickable.quantity = 1;
        pickable.autoRotate = true;

        // 5. Thêm Trigger Collider
        BoxCollider col = stoneObj.AddComponent<BoxCollider>();
        col.size = new Vector3(1, 1, 1);
        col.isTrigger = true;

        // Chuyển sang layer Item nếu có
        int itemLayer = LayerMask.NameToLayer("Item");
        if (itemLayer != -1)
        {
            stoneObj.layer = itemLayer;
            cube.layer = itemLayer;
        }

        EditorUtility.SetDirty(stoneObj);
        Selection.activeGameObject = stoneObj;
        
        Debug.Log("QuestSystemSetupTool: ✅ Đã sinh viên đá để test nhặt!");
    }
}
