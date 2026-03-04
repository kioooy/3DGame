#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool chạy trong Editor giúp sắp xếp 4 nhân vật (Dế Choắt, Dế Trũi, Kiến, Xén Tóc)
/// vào các Waypoint tương ứng với luồng nhiệm vụ.
/// </summary>
public class QuestCharacterArranger : EditorWindow
{
    [MenuItem("Tools/Sắp Xếp Nhân Vật theo Nhiệm Vụ")]
    public static void ShowWindow()
    {
        GetWindow<QuestCharacterArranger>("Quest Arranger").Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Công Cụ Sắp Xếp Character", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "Game có 4 điểm cốt truyện diễn ra:\n" +
            "1. Làng Dế (Gặp Dế Choắt)\n" +
            "2. Nông Trại (Gặp Dế Trũi)\n" +
            "3. Hang Động (Gặp Kiến)\n" +
            "4. Đấu Trường (Gặp Xén Tóc)\n\n" +
            "Bấm nút dưới đây để tạo 4 điểm neo (Waypoint). " +
            "Tool sẽ tự động tìm Prefab của 4 nhân vật này gắn vào Waypoint. " +
            "Sau đó bạn chỉ cần kéo Waypoint thả vào đúng vị trí trên Terrain của bạn.",
            MessageType.Info);

        GUILayout.Space(15);

        if (GUILayout.Button("Tạo Waypoint & Gắn Nhân Vật", GUILayout.Height(40)))
        {
            SetupQuestWaypoints();
        }
    }

    void SetupQuestWaypoints()
    {
        // Tạo Master Parent chứa toàn bộ Quest Locations để gọn Hierarchy
        GameObject masterNode = GameObject.Find("Quest_Locations");
        if (masterNode == null)
        {
            masterNode = new GameObject("Quest_Locations");
            Undo.RegisterCreatedObjectUndo(masterNode, "Tạo Master Node");
        }

        // Tạo 4 Waypoint cách nhau 50 units theo trục Z để không dính chùm
        CreateWaypoint(masterNode.transform, "Waypoint_1_LangDe", "DeChoat", new Vector3(0, 0, 0));
        CreateWaypoint(masterNode.transform, "Waypoint_2_BoRuong", "DeTrui", new Vector3(20, 0, 0));
        CreateWaypoint(masterNode.transform, "Waypoint_3_HangKien", "ConKien", new Vector3(40, 0, 0));
        CreateWaypoint(masterNode.transform, "Waypoint_4_BossArena", "XenToc", new Vector3(60, 0, 0));

        Debug.Log("Đã tạo xong các Quest Waypoints. Hãy chọn chúng trong Hierarchy và di chuyển vào Map của bạn!");
    }

    void CreateWaypoint(Transform parent, string wpName, string charPrefabName, Vector3 spawnPos)
    {
        // Kiểm tra Waypoint tồn tại chưa
        GameObject wp = GameObject.Find(wpName);
        if (wp == null)
        {
            wp = new GameObject(wpName);
            wp.transform.SetParent(parent);
            wp.transform.position = spawnPos;
            Undo.RegisterCreatedObjectUndo(wp, "Tạo " + wpName);
        }

        // Thêm hình cầu tạm (Gizmos) để level designer dễ nhìn vị trí Waypoint
        // Tích hợp icon hiển thị thay vì mesh
        
        // Tìm prefab tương ứng (Cả Prefab lẫn Model FBX)
        string[] guids = AssetDatabase.FindAssets(charPrefabName);
        GameObject prefab = null;
        
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            // Chỉ lấy file .prefab hoặc .fbx, bỏ qua các C# script hay folder trùng tên
            if (path.EndsWith(".prefab") || path.EndsWith(".fbx") || path.EndsWith(".FBX"))
            {
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null) break;
            }
        }
            
        if (prefab != null)
        {
            // Instantiate vào làm con của Waypoint
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(wp.transform, false);
            instance.transform.localPosition = Vector3.zero;
            Undo.RegisterCreatedObjectUndo(instance, "Spawn " + charPrefabName);
        }
        else
        {
            Debug.LogWarning("Không tìm thấy Model/Prefab có tên: " + charPrefabName + " trong thư mục /Assets/. Bạn hãy tự ném nhân vật vào con của " + wpName + " nhé.");
        }
    }
}
#endif
