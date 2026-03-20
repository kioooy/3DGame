using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public class MainMenuSetupTool : EditorWindow
{
    private Sprite backgroundImage;
    private string targetGameScene = "SampleScene";

    [MenuItem("Tools/Tự Động Tạo Main Menu")]
    public static void ShowWindow()
    {
        GetWindow<MainMenuSetupTool>("Main Menu Builder").Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Công Cụ Khởi Tạo Main Menu", EditorStyles.boldLabel);
        GUILayout.Space(10);

        GUILayout.Label("1. Kéo hình ảnh nền của bạn vào đây:", EditorStyles.label);
        backgroundImage = (Sprite)EditorGUILayout.ObjectField(
            "Hình Nền (Sprite)", 
            backgroundImage, 
            typeof(Sprite), 
            false
        );

        GUILayout.Space(10);
        GUILayout.Label("2. Tên của Game Scene cần Load khi bấm PLAY:", EditorStyles.label);
        targetGameScene = EditorGUILayout.TextField("Tên Scene Game", targetGameScene);

        GUILayout.Space(20);
        if (GUILayout.Button("3. TẠO MAIN MENU NGAY", GUILayout.Height(50)))
        {
            if (backgroundImage == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Bạn quên chưa kéo hình ảnh nền vào ô trống kìa!", "OK");
                return;
            }
            BuildMenu();
        }
    }

    void BuildMenu()
    {
        var existingMenu = GameObject.Find("MainMenuCanvas");
        if (existingMenu != null)
        {
            bool replace = EditorUtility.DisplayDialog("Cảnh báo", "Đã tìm thấy một MainMenuCanvas.\nBạn có muốn ghi đè, xóa cái cũ để tạo Menu mới tinh không?", "Tạo Mới", "Hủy");
            if (!replace) return;
            DestroyImmediate(existingMenu);
        }

        // ============================
        // 1. CREATE CANVAS
        // ============================
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        // Đặt Sorting Order rất cao để che phủ toàn bộ UI khác (Settings = 100, Menu = 200)
        canvas.sortingOrder = 200; 

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();
        
        MainMenuManager manager = canvasObj.AddComponent<MainMenuManager>();
        manager.mainMenuCanvas = canvasObj;
        manager.gameSceneName = targetGameScene;

        // Bắt buộc phải có EventSystem để UI bấm được
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            // Dự án của user đang dùng Input System Package mới, nên phải dùng InputSystemUIInputModule
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(eventSystemObj, "Create Event System");
        }

        // ============================
        // 2. BACKGROUND IMAGE
        // ============================
        GameObject bgObj = new GameObject("BackgroundBase");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.sprite = backgroundImage;
        bgImg.preserveAspect = false;

        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Làm tối một chút để Nút dễ nhìn hơn
        GameObject darkOverlay = new GameObject("DarkHazeOverlay");
        darkOverlay.transform.SetParent(canvasObj.transform, false);
        Image dimImg = darkOverlay.AddComponent<Image>();
        dimImg.color = new Color(0, 0, 0, 0.4f);
        RectTransform dimRT = darkOverlay.GetComponent<RectTransform>();
        dimRT.anchorMin = Vector2.zero;
        dimRT.anchorMax = Vector2.one;
        dimRT.offsetMin = Vector2.zero;
        dimRT.offsetMax = Vector2.zero;

        // ============================
        // 3. TITLE TEXT (DẾ MÈN PHIÊU LƯU KÝ)
        // ============================
        GameObject titleObj = new GameObject("GameTitle");
        titleObj.transform.SetParent(canvasObj.transform, false);
        var titleText = titleObj.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.text = "DẾ MÈN PHIÊU LƯU KÝ";
        titleText.fontSize = 110;
        titleText.fontStyle = TMPro.FontStyles.Bold;
        titleText.alignment = TMPro.TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.85f, 0.2f); // Màu Vàng Gold
        // Add Outline Text
        titleText.fontSharedMaterial.EnableKeyword("OUTLINE_ON");
        titleText.outlineWidth = 0.2f;
        titleText.outlineColor = Color.black;

        RectTransform titleRT = titleObj.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.8f);
        titleRT.anchorMax = new Vector2(0.5f, 0.8f);
        titleRT.sizeDelta = new Vector2(1200, 200);

        // ============================
        // 4. BUTTONS
        // ============================
        Button btnPlay = CreateButton(canvasObj.transform, "BTN_Play", "PLAY", new Vector2(0, -50), new Color(0.18f, 0.8f, 0.25f));
        Button btnSettings = CreateButton(canvasObj.transform, "BTN_Settings", "SETTINGS", new Vector2(0, -180), new Color(0.25f, 0.5f, 0.8f));
        Button btnExit = CreateButton(canvasObj.transform, "BTN_Exit", "EXIT", new Vector2(0, -310), new Color(0.8f, 0.25f, 0.18f));

        // Nối vào Manager
        manager.playButton = btnPlay;
        manager.settingsButton = btnSettings;
        manager.exitButton = btnExit;

        // Đánh dấu Undo System
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Main Menu Builder");
        EditorUtility.DisplayDialog("Hoàn Tất!", "Chuyển đổi thành công! Menu này sẽ tự động Load Scene: " + targetGameScene + ".\nNhớ kéo cả Scene Menu và Scene Game vào [File > Build Settings] nhé!", "Đã Xong");
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 positionYOffset, Color bgColor)
    {
        // Button Phisical Object
        GameObject bObj = new GameObject(name);
        bObj.transform.SetParent(parent, false);
        
        // Rect Transform
        RectTransform rt = bObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = positionYOffset;
        rt.sizeDelta = new Vector2(400, 90);

        // Image Background
        Image img = bObj.AddComponent<Image>();
        img.color = bgColor;
        
        // Button Logic Component
        Button btn = bObj.AddComponent<Button>();
        btn.targetGraphic = img;

        // Text Component
        GameObject tObj = new GameObject("Text");
        tObj.transform.SetParent(bObj.transform, false);
        var txt = tObj.AddComponent<TMPro.TextMeshProUGUI>();
        txt.text = label;
        txt.fontSize = 45;
        txt.fontStyle = TMPro.FontStyles.Bold;
        txt.alignment = TMPro.TextAlignmentOptions.Center;
        txt.color = Color.white;

        RectTransform trt = tObj.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return btn;
    }
}
