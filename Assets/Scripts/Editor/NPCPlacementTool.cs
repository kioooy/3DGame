using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class NPCPlacementTool : EditorWindow
{
    private Vector3 centerPoint = Vector3.zero;
    private float scatterRadius = 20f;
    private float yOffset = 0.5f;

    [MenuItem("Tools/Antigravity/NPC Placement Tool")]
    public static void ShowWindow()
    {
        GetWindow<NPCPlacementTool>("NPC Placement Tool");
    }

    void OnGUI()
    {
        GUILayout.Label("NPC Placement & Scatter Tool", EditorStyles.boldLabel);
        
        centerPoint = EditorGUILayout.Vector3Field("Center Point", centerPoint);
        scatterRadius = EditorGUILayout.FloatField("Scatter Radius", scatterRadius);
        yOffset = EditorGUILayout.FloatField("Y Offset (Height)", yOffset);

        GUILayout.Space(10);

        if (GUILayout.Button("Scatter 5 New NPCs (Randomly)"))
        {
            ScatterNPCs();
        }

        GUILayout.Space(20);
        GUILayout.Label("Hướng dẫn:", EditorStyles.wordWrappedLabel);
        GUILayout.Label("1. Tool này sẽ tìm các NPC mang script (BoHung, BoNgua...) trong scene.\n2. Nếu tìm thấy, nó sẽ di chuyển chúng đến vị trí ngẫu nhiên quanh tâm.\n3. Nếu chưa có script, hãy gán script vào model trước khi chạy tool.", EditorStyles.wordWrappedLabel);
    }

    private void ScatterNPCs()
    {
        System.Type[] npcTypes = {
            typeof(BoHungNPC),
            typeof(BoNguaNPC),
            typeof(ChauChauNPC),
            typeof(OngNPC),
            typeof(VeSauNPC)
        };

        int count = 0;
        foreach (var type in npcTypes)
        {
            Object[] npcs = FindObjectsByType(type, FindObjectsSortMode.None);
            foreach (var obj in npcs)
            {
                MonoBehaviour npc = (MonoBehaviour)obj;
                Undo.RecordObject(npc.transform, "Scatter NPC");
                
                Vector2 randomCircle = Random.insideUnitCircle * scatterRadius;
                Vector3 newPos = centerPoint + new Vector3(randomCircle.x, yOffset, randomCircle.y);
                
                // Thử bắn Raycast xuống đất để NPC không bị bay
                if (Physics.Raycast(newPos + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 50f))
                {
                    newPos.y = hit.point.y + yOffset;
                }

                npc.transform.position = newPos;
                count++;
            }
        }

        Debug.Log($"Đã rải xong {count} NPC trong Scene!");
    }
}
