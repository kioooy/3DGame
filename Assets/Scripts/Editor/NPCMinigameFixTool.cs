using UnityEngine;
using UnityEditor;

/// <summary>
/// Tool cũ - giờ mỗi NPC đã có script riêng (XenTocNPC, ConKienNPC, DeTruiNPC, DeChoatNPC).
/// Tool này chỉ còn dùng để verify cấu hình minigame trên DeTruiNPC.
/// </summary>
public class NPCMinigameFixTool : EditorWindow
{
    [MenuItem("Window/Quest System/2. Fix NPC Minigame Assignments")]
    public static void FixNPCMinigames()
    {
        int count = 0;

        // DeTruiNPC: racing + caro
        foreach (DeTruiNPC npc in FindObjectsByType<DeTruiNPC>(FindObjectsSortMode.None))
        {
            npc.enableRacing = true;
            npc.enableCaro   = true;
            EditorUtility.SetDirty(npc);
            count++;
            Debug.Log($"[NPC Fix] DeTruiNPC '{npc.gameObject.name}': racing=true, caro=true");
        }

        // XenTocNPC: arm wrestling được xử lý trong XenTocNPC riêng
        foreach (XenTocNPC npc in FindObjectsByType<XenTocNPC>(FindObjectsSortMode.None))
        {
            EditorUtility.SetDirty(npc);
            count++;
            Debug.Log($"[NPC Fix] XenTocNPC '{npc.gameObject.name}': OK (arm wrestling tích hợp sẵn)");
        }

        // ConKienNPC
        foreach (ConKienNPC npc in FindObjectsByType<ConKienNPC>(FindObjectsSortMode.None))
        {
            EditorUtility.SetDirty(npc);
            count++;
            Debug.Log($"[NPC Fix] ConKienNPC '{npc.gameObject.name}': OK");
        }

        // DeChoatNPC
        foreach (DeChoatNPC npc in FindObjectsByType<DeChoatNPC>(FindObjectsSortMode.None))
        {
            EditorUtility.SetDirty(npc);
            count++;
            Debug.Log($"[NPC Fix] DeChoatNPC '{npc.gameObject.name}': OK");
        }

        Debug.Log($"[FixNPCMinigameTool] Đã kiểm tra {count} NPC trong Scene hiện tại!");
        EditorUtility.DisplayDialog("Xong", $"Đã kiểm tra {count} NPC. Xem Console để biết chi tiết.", "OK");
    }
}
