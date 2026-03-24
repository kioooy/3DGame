#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Tool để debug / reset cốt truyện trong Play Mode.
/// Menu: Tools > Story Debug Panel
/// </summary>
public class StoryDebugTool : EditorWindow
{
    private static readonly string[] phaseNames = {
        "Phase 0 – Bắt đầu (chưa gặp Xén Tóc)",
        "Phase 1 – Đã thắng Xén Tóc (vật tay)",
        "Phase 2 – Đang cưỡi Xén Tóc đến bãi đá",
        "Phase 3 – Đã thắng Côn Kiến (đua xe)",
        "Phase 4 – Đang giải cứu Dế Choắt (Dế Trũi đào)",
        "Phase 5 – KẾT THÚC: Dế Choắt được cứu"
    };

    [MenuItem("Tools/Story Debug Panel")]
    public static void ShowWindow()
    {
        var w = GetWindow<StoryDebugTool>("Story Debug");
        w.minSize = new Vector2(380, 260);
        w.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("🐛 Dế Mèn – Story Quest Debug", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Cần chạy Play Mode để sử dụng tool này.", MessageType.Warning);

            // Đọc thẳng từ PlayerPrefs
            int savedPhase = PlayerPrefs.GetInt("story_phase", 0);
            EditorGUILayout.LabelField("Phase hiện tại (PlayerPrefs):", phaseNames[Mathf.Min(savedPhase, phaseNames.Length - 1)]);

            GUILayout.Space(10);
            if (GUILayout.Button("🔄 Reset cốt truyện về Phase 0"))
            {
                PlayerPrefs.DeleteKey("story_phase");
                PlayerPrefs.DeleteKey("XenToc_PlayerWon");
                PlayerPrefs.Save();
                Debug.Log("[StoryDebug] Reset về Phase 0");
            }
            return;
        }

        // --- PLAY MODE ---
        var mgr = StoryQuestManager.Instance;
        int current = mgr.currentPhase;

        EditorGUILayout.LabelField("Phase hiện tại:", phaseNames[Mathf.Min(current, phaseNames.Length - 1)]);
        GUILayout.Space(8);

        GUILayout.Label("Nhảy đến phase:", EditorStyles.boldLabel);
        for (int i = 0; i < phaseNames.Length; i++)
        {
            GUI.enabled = i != current;
            if (GUILayout.Button($"[{i}] {phaseNames[i]}"))
            {
                mgr.currentPhase = i;
                mgr.Save();
                Debug.Log($"[StoryDebug] Nhảy đến Phase {i}");
            }
        }
        GUI.enabled = true;

        GUILayout.Space(10);
        if (GUILayout.Button("🔄 Reset về Phase 0", GUILayout.Height(32)))
        {
            mgr.ResetStory();
        }
    }
}
#endif
