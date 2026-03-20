using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RacingAudioFixer : EditorWindow
{
    [MenuItem("Window/Minigame Tools/Racing Audio Fixer (No Reset)")]
    public static void ShowWindow()
    {
        GetWindow<RacingAudioFixer>("Racing Audio Fixer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Racing Audio Setup (Local Scene Only)", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Apply Audio to Current Scene", GUILayout.Height(40)))
        {
            ApplyAudio();
        }
        
        EditorGUILayout.HelpBox("Tool này gán âm thanh trực tiếp vào các đối tượng trong scene hiện tại mà không làm reset scene hay mở lại scene.", MessageType.Info);
    }

    private static void ApplyAudio()
    {
        // 1. Manager setup
        RacingMinigameManager manager = GameObject.FindFirstObjectByType<RacingMinigameManager>();
        if (manager != null)
        {
            Undo.RecordObject(manager, "Update Racing Manager Audio");
            manager.bgmClip = LoadAudio("sfx_bgm_armwrestling");
            manager.winSFX = LoadAudio("sfx_ui_win");
            manager.loseSFX = LoadAudio("sfx_ui_lose");
            manager.startSFX = LoadAudio("sfx_ui_click");
            manager.goSFX = LoadAudio("sfx_ui_correct");
            
            // Tìm raceArea nếu chưa gán
            if (manager.raceArea == null)
            {
                GameObject raceArea = GameObject.Find("RaceArea");
                if (raceArea == null) raceArea = GameObject.Find("RacingArea");
                if (raceArea != null) manager.raceArea = raceArea;
            }

            // Tìm resultsPanel nếu chưa gán
            if (manager.resultsPanel == null)
            {
                if (manager.endPanel != null) manager.resultsPanel = manager.endPanel;
                else
                {
                    GameObject panel = GameObject.Find("ResultsPanel");
                    if (panel == null) panel = GameObject.Find("EndPanel");
                    if (panel != null) manager.resultsPanel = panel;
                }
            }
            
            // Fix các references khác nếu thiếu
            if (manager.playerRacer == null) manager.playerRacer = GameObject.FindFirstObjectByType<RacingPlayer>();
            if (manager.npcRacer == null) manager.npcRacer = GameObject.FindFirstObjectByType<RacingNPC>();
            
            EditorUtility.SetDirty(manager);
            Debug.Log("✅ [RacingAudioFixer] Updated RacingMinigameManager and fixed missing references");
        }

        // 2. Player setup
        RacingPlayer player = GameObject.FindFirstObjectByType<RacingPlayer>();
        if (player != null)
        {
            Undo.RecordObject(player, "Update Racing Player Audio");
            player.footstepSFX = new AudioClip[] {
                LoadAudio("footstep_1"),
                LoadAudio("footstep_2"),
                LoadAudio("footstep_3"),
                LoadAudio("footstep_4")
            };
            player.impactSFX = LoadAudio("sfx_racing_impact");
            player.jumpSFX = LoadAudio("sfx_racing_jump");
            
            EditorUtility.SetDirty(player);
            Debug.Log("✅ [RacingAudioFixer] Updated RacingPlayer");
        }

        // 3. NPC setup
        RacingNPC npc = GameObject.FindFirstObjectByType<RacingNPC>();
        if (npc != null)
        {
            Undo.RecordObject(npc, "Update Racing NPC Audio");
            npc.footstepSFX = new AudioClip[] {
                LoadAudio("footstep_1"),
                LoadAudio("footstep_2"),
                LoadAudio("footstep_3"),
                LoadAudio("footstep_4")
            };
            npc.impactSFX = LoadAudio("sfx_racing_impact");
            npc.jumpSFX = LoadAudio("sfx_racing_jump");
            
            EditorUtility.SetDirty(npc);
            Debug.Log("✅ [RacingAudioFixer] Updated RacingNPC");
        }
        
        Debug.Log("🎉 [RacingAudioFixer] All audio applied successfully to the current scene!");
    }

    private static AudioClip LoadAudio(string name)
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:AudioClip");
        if (guids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }
}
