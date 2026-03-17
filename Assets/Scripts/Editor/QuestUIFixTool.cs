using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Tool để fix và rebuild toàn bộ Quest UI từ đầu.
/// Menu: Window > Quest System > Fix Quest UI
/// </summary>
public class QuestUIFixTool : EditorWindow
{
    [MenuItem("Window/Quest System/Fix Quest UI (J Key)")]
    public static void FixQuestUI()
    {
        // --- Bước 1: Tìm hoặc tạo DemenQuestUIManager ---
        Demen.Quests.DemenQuestUIManager questManager = Object.FindFirstObjectByType<Demen.Quests.DemenQuestUIManager>();
        if (questManager == null)
        {
            GameObject managerObj = GameObject.Find("DemenQuestCanvas");
            if (managerObj == null) managerObj = new GameObject("DemenQuestCanvas");
            questManager = managerObj.AddComponent<Demen.Quests.DemenQuestUIManager>();
            Debug.Log("QuestUIFixTool: Tạo mới DemenQuestUIManager.");
        }

        // --- Bước 2: Tìm Canvas chính ---
        Canvas targetCanvas = null;
        Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            // Ưu tiên Canvas ở chế độ ScreenSpaceOverlay
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                targetCanvas = c;
                break;
            }
        }
        if (targetCanvas == null && allCanvases.Length > 0)
            targetCanvas = allCanvases[0];

        if (targetCanvas == null)
        {
            GameObject canvasObj = new GameObject("MainCanvas");
            targetCanvas = canvasObj.AddComponent<Canvas>();
            targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("QuestUIFixTool: Tạo mới Canvas.");
        }

        // --- Bước 3: Xoá QuestPanel cũ nếu có ---
        var oldPanel = GameObject.Find("QuestPanel");
        if (oldPanel != null)
        {
            DestroyImmediate(oldPanel);
            Debug.Log("QuestUIFixTool: Xoá QuestPanel cũ.");
        }

        // --- Bước 4: Tạo QuestPanel mới ---
        GameObject questPanel = new GameObject("QuestPanel");
        questPanel.transform.SetParent(targetCanvas.transform, false);

        Image bg = questPanel.AddComponent<Image>();
        bg.color = new Color(0.05f, 0.05f, 0.1f, 0.93f);

        RectTransform panelRect = questPanel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.65f, 0.15f);
        panelRect.anchorMax = new Vector2(1f, 0.98f);
        panelRect.offsetMin = new Vector2(10, 10);
        panelRect.offsetMax = new Vector2(-10, -10);

        // --- Bước 5: Tạo nội dung bên trong ---
        // Tiêu đề
        CreateText(questPanel.transform, "Title", "NHIỆM VỤ", 28, FontStyles.Bold,
            new Color(1f, 0.85f, 0.2f), TextAlignmentOptions.Center, new Vector2(0, -12), new Vector2(0, 40));

        // Đường kẻ
        CreateDivider(questPanel.transform);

        // Danh sách nhiệm vụ
        float y = -75f;
        CreateQuestEntry(questPanel.transform, "talk_detrui",     "○ Trò chuyện với Dế Trũi",        ref y);
        CreateQuestEntry(questPanel.transform, "minigame_detrui", "○ Chạy đua với Dế Trũi",     ref y);
        CreateQuestEntry(questPanel.transform, "talk_dechoat",    "○ Hỏi thăm Dế Choắt",       ref y);
        CreateQuestEntry(questPanel.transform, "minigame_dechoat","○ Chơi Cờ Caro với Dế Choắt",     ref y);
        CreateQuestEntry(questPanel.transform, "talk_conkien",    "○ Trò chuyện với Kiến Chỉ Huy",   ref y);
        CreateQuestEntry(questPanel.transform, "talk_xentoc",     "○ Gặp gỡ Xén Tóc (Boss)",         ref y);
        CreateQuestEntry(questPanel.transform, "minigame_xentoc", "○ Tỷ thí Vật tay với Xén Tóc",    ref y);

        // Gợi ý phím
        CreateText(questPanel.transform, "Hint", "[J] Đóng", 16, FontStyles.Italic,
            new Color(0.55f, 0.55f, 0.55f), TextAlignmentOptions.Center,
            new Vector2(0, 12), new Vector2(0, 28), anchorBottom: true);

        // --- Bước 6: Gán reference về DemenQuestUIManager ---
        SerializedObject so = new SerializedObject(questManager);
        SerializedProperty panelProp = so.FindProperty("questPanel");
        if (panelProp != null)
        {
            panelProp.objectReferenceValue = questPanel;
            so.ApplyModifiedProperties();
        }

        questPanel.SetActive(false);

        // --- Bước 7: Lưu Scene ---
        EditorUtility.SetDirty(questManager);
        EditorUtility.SetDirty(questPanel);
        EditorSceneManager.MarkSceneDirty(questManager.gameObject.scene);

        Debug.Log("QuestUIFixTool: ✅ Đã tạo xong Quest UI! Hãy bấm Ctrl+S để lưu, sau đó chạy game và thử phím J.");
    }

    // ─── Helper methods ─────────────────────────────────────────────────────

    static void CreateQuestEntry(Transform parent, string questId, string label, ref float yPos)
    {
        GameObject obj = new GameObject("Quest_" + questId);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, yPos);
        rt.sizeDelta = new Vector2(-30, 32);

        yPos -= 34f;
    }

    static void CreateText(Transform parent, string name, string content, float size,
        FontStyles style, Color color, TextAlignmentOptions align,
        Vector2 anchoredPos, Vector2 sizeDelta, bool anchorBottom = false)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = align;

        RectTransform rt = obj.GetComponent<RectTransform>();
        if (anchorBottom)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(0.5f, 0);
        }
        else
        {
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
        }
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
    }

    static void CreateDivider(Transform parent)
    {
        GameObject obj = new GameObject("Divider");
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = new Color(1f, 0.85f, 0.2f, 0.4f);

        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.05f, 1);
        rt.anchorMax = new Vector2(0.95f, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, -55);
        rt.sizeDelta = new Vector2(0, 2);
    }
}
