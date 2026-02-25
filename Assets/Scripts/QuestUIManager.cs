using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class QuestUIManager : MonoBehaviour
{
    public static QuestUIManager Instance;

    [Header("UI References")]
    public GameObject questPanel;

    // Theo dõi trạng thái quest
    private Dictionary<string, bool> questStatus = new Dictionary<string, bool>();
    private Dictionary<string, TextMeshProUGUI> questTextMap = new Dictionary<string, TextMeshProUGUI>();

    /// <summary>
    /// Kiểm tra quest panel đang mở hay không
    /// </summary>
    public bool IsOpen => questPanel != null && questPanel.activeSelf;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        // Ẩn quest panel khi bắt đầu game
        if (questPanel != null)
            questPanel.SetActive(false);
    }

    void Start()
    {
        // Đăng ký lại các quest entries từ UI đã tạo sẵn
        if (questPanel != null)
        {
            foreach (Transform child in questPanel.transform)
            {
                if (child.name.StartsWith("Quest_"))
                {
                    string questId = child.name.Substring(6); // Bỏ "Quest_"
                    TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
                    if (text != null)
                    {
                        questStatus[questId] = false;
                        questTextMap[questId] = text;
                        Debug.Log($"QuestUIManager: Đã đăng ký quest '{questId}'");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Toggle quest panel (bấm J để mở/đóng)
    /// </summary>
    public void ToggleQuestPanel()
    {
        if (questPanel != null)
            questPanel.SetActive(!questPanel.activeSelf);
    }

    /// <summary>
    /// Hoàn thành nhiệm vụ theo ID
    /// </summary>
    public void CompleteQuest(string questId)
    {
        if (questStatus.ContainsKey(questId))
        {
            questStatus[questId] = true;
            UpdateQuestDisplay(questId);
            Debug.Log($"QuestUIManager: Nhiệm vụ '{questId}' đã hoàn thành!");
        }
    }

    /// <summary>
    /// Kiểm tra quest đã hoàn thành chưa
    /// </summary>
    public bool IsQuestCompleted(string questId)
    {
        return questStatus.ContainsKey(questId) && questStatus[questId];
    }

    /// <summary>
    /// Cập nhật hiển thị quest trên UI
    /// </summary>
    private void UpdateQuestDisplay(string questId)
    {
        if (questTextMap.ContainsKey(questId) && questTextMap[questId] != null)
        {
            TextMeshProUGUI text = questTextMap[questId];
            // Đổi ○ thành ✔, thêm màu xanh lá
            string questName = text.text;
            if (questName.StartsWith("○"))
                questName = "✔" + questName.Substring(1);

            text.text = questName;
            text.color = new Color(0.3f, 0.9f, 0.3f); // Xanh lá sáng
            text.fontStyle = FontStyles.Strikethrough;
        }
    }

    /// <summary>
    /// Tạo UI mặc định cho Quest Panel trong Editor
    /// Click chuột phải vào component → "Generate Quest UI"
    /// </summary>
    [ContextMenu("Generate Quest UI")]
    public void CreateDefaultUI()
    {
        // Tìm hoặc tạo Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("QuestCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Tạo Quest Panel
        if (questPanel == null)
        {
            questPanel = new GameObject("QuestPanel");
            questPanel.transform.SetParent(canvas.transform, false);

            // Panel background
            Image panelImage = questPanel.AddComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.15f, 0.9f);

            RectTransform panelRect = questPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.65f, 0.25f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.offsetMin = new Vector2(10, 10);
            panelRect.offsetMax = new Vector2(-20, -20);

            // === Tiêu đề "NHIỆM VỤ" ===
            GameObject titleObj = new GameObject("TitleText");
            titleObj.transform.SetParent(questPanel.transform, false);
            TextMeshProUGUI titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "NHIỆM VỤ";
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.84f, 0f); // Vàng gold
            titleText.alignment = TextAlignmentOptions.Center;

            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, -15);
            titleRect.sizeDelta = new Vector2(0, 50);

            // === Đường kẻ ngang ===
            GameObject lineObj = new GameObject("Divider");
            lineObj.transform.SetParent(questPanel.transform, false);
            Image lineImage = lineObj.AddComponent<Image>();
            lineImage.color = new Color(1f, 0.84f, 0f, 0.5f);

            RectTransform lineRect = lineObj.GetComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.05f, 1);
            lineRect.anchorMax = new Vector2(0.95f, 1);
            lineRect.pivot = new Vector2(0.5f, 1);
            lineRect.anchoredPosition = new Vector2(0, -70);
            lineRect.sizeDelta = new Vector2(0, 2);

            // === Chương 1: Làng Dế - Sự kiêu ngạo ===
            CreateQuestEntry(questPanel.transform, "talk_detrui",     "○ Nói chuyện với Dế Trũi", -90f);
            CreateQuestEntry(questPanel.transform, "explore_village",  "○ Khám phá Làng Dế",       -125f);

            // === Chương 2: Bờ Ruộng - Trách nhiệm ===
            CreateQuestEntry(questPanel.transform, "collect_items",   "○ Thu thập 3 vật phẩm",    -160f);

            // === Chương 3: Hang Kiến - Đoàn kết ===
            CreateQuestEntry(questPanel.transform, "find_antcolony",  "○ Tìm đường vào Hang Kiến",-195f);

            // === Chương 4: Đối đầu Xén Tóc ===
            CreateQuestEntry(questPanel.transform, "defeat_xentoc",   "○ Đánh bại Xén Tóc",       -230f);

            // === Hướng dẫn phím tắt ===
            GameObject hintObj = new GameObject("HintText");
            hintObj.transform.SetParent(questPanel.transform, false);
            TextMeshProUGUI hintText = hintObj.AddComponent<TextMeshProUGUI>();
            hintText.text = "Nhấn [J] để đóng";
            hintText.fontSize = 18;
            hintText.fontStyle = FontStyles.Italic;
            hintText.color = new Color(0.6f, 0.6f, 0.6f);
            hintText.alignment = TextAlignmentOptions.Center;

            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(0, 0);
            hintRect.anchorMax = new Vector2(1, 0);
            hintRect.pivot = new Vector2(0.5f, 0);
            hintRect.anchoredPosition = new Vector2(0, 15);
            hintRect.sizeDelta = new Vector2(0, 30);

            questPanel.SetActive(false);
            Debug.Log("QuestUIManager: Quest UI đã được tạo thành công!");
        }
        else
        {
            Debug.LogWarning("QuestUIManager: questPanel đã tồn tại!");
        }
    }

    /// <summary>
    /// Tạo một dòng quest entry với ID để theo dõi
    /// </summary>
    private void CreateQuestEntry(Transform parent, string questId, string text, float yPos)
    {
        GameObject entryObj = new GameObject("Quest_" + questId);
        entryObj.transform.SetParent(parent, false);
        TextMeshProUGUI entryText = entryObj.AddComponent<TextMeshProUGUI>();
        entryText.text = text;
        entryText.fontSize = 22;
        entryText.color = Color.white;
        entryText.alignment = TextAlignmentOptions.Left;

        RectTransform entryRect = entryObj.GetComponent<RectTransform>();
        entryRect.anchorMin = new Vector2(0, 1);
        entryRect.anchorMax = new Vector2(1, 1);
        entryRect.pivot = new Vector2(0.5f, 1);
        entryRect.anchoredPosition = new Vector2(0, yPos);
        entryRect.sizeDelta = new Vector2(-40, 35);

        // Đăng ký quest vào hệ thống theo dõi
        questStatus[questId] = false;
        questTextMap[questId] = entryText;
    }
}