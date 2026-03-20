using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class StoryViewerSetupTool : EditorWindow
{
    [MenuItem("Window/Quest System/Generate Story Viewer UI")]
    public static void ShowWindow()
    {
        GetWindow<StoryViewerSetupTool>("Story Viewer Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Tạo bộ giao diện Lật trang Truyện (Story Viewer)", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Tự động tạo UI Story Viewer", GUILayout.Height(40)))
        {
            CreateDefaultUI();
        }
    }

    public static void CreateDefaultUI()
    {
        StoryViewerManager existingManager = Object.FindFirstObjectByType<StoryViewerManager>();
        if (existingManager != null && existingManager.storyPanel != null)
        {
            Debug.Log("StoryViewerManager UI đã có sẵn!");
            return;
        }

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("MainCanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Tạo Manager nếu chưa có
        GameObject managerObj = existingManager != null ? existingManager.gameObject : new GameObject("StoryViewerManager");
        StoryViewerManager manager = managerObj.GetComponent<StoryViewerManager>();
        if (manager == null) manager = managerObj.AddComponent<StoryViewerManager>();

        // Tạo Panel chính
        GameObject panelObj = new GameObject("StoryViewerPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        
        Image panelBg = panelObj.AddComponent<Image>();
        panelBg.color = new Color(0, 0, 0, 0.85f); // Nền đen mờ

        // Vùng chứa ảnh
        GameObject imageObj = new GameObject("StoryImage");
        imageObj.transform.SetParent(panelObj.transform, false);
        RectTransform imgRect = imageObj.AddComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.5f, 0.5f);
        imgRect.anchorMax = new Vector2(0.5f, 0.5f);
        imgRect.sizeDelta = new Vector2(800, 450);
        Image storyImg = imageObj.AddComponent<Image>();

        // Nút Prev
        GameObject prevBtnObj = CreateButton("PrevButton", panelObj.transform, new Vector2(0.1f, 0.5f), "<", new Vector2(60, 60));
        Button prevBtn = prevBtnObj.GetComponent<Button>();

        // Nút Next
        GameObject nextBtnObj = CreateButton("NextButton", panelObj.transform, new Vector2(0.9f, 0.5f), ">", new Vector2(60, 60));
        Button nextBtn = nextBtnObj.GetComponent<Button>();

        // Nút Đóng
        GameObject closeBtnObj = CreateButton("CloseButton", panelObj.transform, new Vector2(0.95f, 0.95f), "X", new Vector2(50, 50));
        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.image.color = new Color(0.8f, 0.2f, 0.2f); // Nút đỏ

        // Text trang hiển thị
        GameObject pageTextObj = new GameObject("PageText");
        pageTextObj.transform.SetParent(panelObj.transform, false);
        RectTransform pageRect = pageTextObj.AddComponent<RectTransform>();
        pageRect.anchorMin = new Vector2(0.5f, 0.1f);
        pageRect.anchorMax = new Vector2(0.5f, 0.1f);
        pageRect.sizeDelta = new Vector2(200, 40);
        TextMeshProUGUI pageText = pageTextObj.AddComponent<TextMeshProUGUI>();
        pageText.text = "1 / 1";
        pageText.fontSize = 24;
        pageText.alignment = TextAlignmentOptions.Center;
        pageText.color = Color.white;

        // Gắn references
        manager.storyPanel = panelObj;
        manager.storyImage = storyImg;
        manager.prevButton = prevBtn;
        manager.nextButton = nextBtn;
        manager.closeButton = closeBtn;
        manager.pageText = pageText;

        panelObj.SetActive(false); // Mặc định ẩn

        Debug.Log("Đã tạo UI Story Viewer thành công!");
    }

    private static GameObject CreateButton(string name, Transform parent, Vector2 anchor, string text, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        RectTransform rect = btnObj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f); // Nút tối màu

        Button btn = btnObj.AddComponent<Button>();

        GameObject txtObj = new GameObject("Text");
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform txtRect = txtObj.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpro = txtObj.AddComponent<TextMeshProUGUI>();
        tmpro.text = text;
        tmpro.fontSize = 30;
        tmpro.fontStyle = FontStyles.Bold;
        tmpro.alignment = TextAlignmentOptions.Center;
        tmpro.color = Color.white;

        return btnObj;
    }
}
