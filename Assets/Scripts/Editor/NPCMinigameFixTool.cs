using UnityEngine;
using UnityEditor;

public class NPCMinigameFixTool : EditorWindow
{
    [MenuItem("Window/Quest System/2. Fix NPC Minigame Assignments")]
    public static void FixNPCMinigames()
    {
        DeTruiNPC[] npcs = FindObjectsByType<DeTruiNPC>(FindObjectsSortMode.None);
        int count = 0;
        foreach (DeTruiNPC npc in npcs)
        {
            if (npc.gameObject == null) continue;
            string nameLower = npc.gameObject.name.ToLower();
            
            if (nameLower.Contains("detrui") || nameLower.Contains("dế trũi"))
            {
                npc.enableRacing = true;
                npc.enableCaro = false;
                npc.enableArmWrestling = false;
                count++;
            }
            else if (nameLower.Contains("dechoat") || nameLower.Contains("dế choắt"))
            {
                npc.enableRacing = false;
                npc.enableCaro = true;
                npc.enableArmWrestling = false;
                count++;
            }
            else if (nameLower.Contains("xentoc") || nameLower.Contains("xén tóc"))
            {
                npc.enableRacing = false;
                npc.enableCaro = false;
                npc.enableArmWrestling = true;
                count++;
            }
            else
            {
                // Other NPCs (kien, v.v.)
                npc.enableRacing = false;
                npc.enableCaro = false;
                npc.enableArmWrestling = false;
                count++;
            }
            
            EditorUtility.SetDirty(npc);
        }
        
        Debug.Log($"[FixNPCMinigameTool] Đã fix lỗi Minigame cho {count} NPCs trong Scene hiện tại!");
    }
}
