#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class FixRacingCollidersTool : EditorWindow
{
    [MenuItem("Tools/3DGame/Fix Racing Colliders")]
    public static void FixColliders()
    {
        int fixedCount = 0;

        // 1. Sửa Player
        RacingPlayer player = Object.FindFirstObjectByType<RacingPlayer>();
        if (player != null)
        {
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = player.gameObject.AddComponent<Rigidbody>();
                Debug.Log($"Đã thêm Rigidbody cho {player.name}");
                fixedCount++;
            }
            rb.useGravity = false;
            rb.isKinematic = true; // Tránh bị rớt xuyên map
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            Collider col = player.GetComponent<Collider>();
            if (col == null)
            {
                col = player.gameObject.AddComponent<CapsuleCollider>();
                ((CapsuleCollider)col).height = 2f;
                ((CapsuleCollider)col).center = new Vector3(0, 1f, 0);
                Debug.Log($"Đã thêm CapsuleCollider cho {player.name}");
                fixedCount++;
            }
        }

        // 2. Sửa NPC
        RacingNPC npc = Object.FindFirstObjectByType<RacingNPC>();
        if (npc != null)
        {
            Rigidbody rb = npc.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = npc.gameObject.AddComponent<Rigidbody>();
                Debug.Log($"Đã thêm Rigidbody cho {npc.name}");
                fixedCount++;
            }
            rb.useGravity = false;
            rb.isKinematic = true;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            Collider col = npc.GetComponent<Collider>();
            if (col == null)
            {
                col = npc.gameObject.AddComponent<CapsuleCollider>();
                ((CapsuleCollider)col).height = 2f;
                ((CapsuleCollider)col).center = new Vector3(0, 1f, 0);
                Debug.Log($"Đã thêm CapsuleCollider cho {npc.name}");
                fixedCount++;
            }
        }

        // 3. Sửa Chướng ngại vật (Obstacles)
        // Hệ thống trigger cần MỘT TRONG HAI có Rigidbody, nhưng để cho chắc chắn 100%, đắp luôn Rigidbody vô rào chắn
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Obstacle"))
            {
                Collider col = obj.GetComponent<Collider>();
                if (col != null && !col.isTrigger)
                {
                    col.isTrigger = true;
                    fixedCount++;
                }

                Rigidbody rb = obj.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = obj.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                    fixedCount++;
                }

                // Gia tăng độ dày của BoxCollider để tránh việc tốc độ quá nhanh trượt xuyên qua
                if (col is BoxCollider box)
                {
                    if (box.size.z < 1.0f)
                    {
                        box.size = new Vector3(box.size.x, box.size.y, 1.5f);
                    }
                }
            }
        }

        if (fixedCount > 0)
        {
            EditorUtility.DisplayDialog("Thành công", $"Đã fix và gia cố vật lý cho {fixedCount} Component trong Scene! Hãy chạy game lại để xem kết quả.", "Tuyệt");
        }
        else
        {
            EditorUtility.DisplayDialog("Thông báo", "Kiểm tra thấy các vật thể đều đã có đủ Rigidbody và Collider, không cần fix gì thêm.", "OK");
        }
    }
}
#endif
