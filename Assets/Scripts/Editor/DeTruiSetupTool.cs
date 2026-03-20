using UnityEngine;
using UnityEditor;

public class DeTruiSetupTool : EditorWindow
{
    [MenuItem("Tools/Sửa lỗi Dế Trũi (Rơi xuyên đất + Nhảy)")]
    public static void ShowWindow()
    {
        GetWindow<DeTruiSetupTool>("NPC Setup").Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Công cụ tự động cấu hình Vật lý cho Dế Trũi", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("FIX TỰ ĐỘNG", GUILayout.Height(50)))
        {
            FixDeTrui();
        }
    }

    void FixDeTrui()
    {
        DeTruiNPC[] npcs = FindObjectsByType<DeTruiNPC>(FindObjectsSortMode.None);
        if (npcs.Length == 0)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Dế Trũi trong Scene!", "OK");
            return;
        }

        foreach (var npc in npcs)
        {
            GameObject go = npc.gameObject;

            // 1. Setup Rigidbody
            Rigidbody rb = go.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = go.AddComponent<Rigidbody>();
            }
            rb.useGravity = false; // Script tự lo trọng lực
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // 2. Setup Capsule Collider
            CapsuleCollider col = go.GetComponent<CapsuleCollider>();
            if (col == null)
            {
                col = go.AddComponent<CapsuleCollider>();
                // Giá trị tương đối cho 1 humanoid NPC, bạn có thể chỉnh lại cho chuẩn với Model
                col.height = 2f;
                col.center = new Vector3(0, 1f, 0);
                col.radius = 0.5f;
            }

            // 3. Fix LayerMask (Lỗi rơi xuyên đất do chưa Set Layer)
            // Lấy mọi layer trừ Ignore Raycast và TransparentFX để làm mặt đất
            int groundMask = ~LayerMask.GetMask("Ignore Raycast", "TransparentFX", "MinimapIcon", "UI");
            npc.groundLayer = groundMask;
            
            // Obstacle check có thể giống Ground check
            npc.obstacleLayer = groundMask;

            // Lưu thay đổi
            EditorUtility.SetDirty(npc);
            EditorUtility.SetDirty(go);
            Debug.Log($"Đã sửa thành công cấu hình cho: {go.name}");
        }

        EditorUtility.DisplayDialog("Thành công", $"Đã Fix cho {npcs.Length} nhân vật Dế Trũi. Bạn hãy thử Play lại game!", "OK");
    }
}
