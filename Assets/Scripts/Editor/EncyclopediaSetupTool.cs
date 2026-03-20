using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class EncyclopediaSetupTool : EditorWindow
{
    [MenuItem("Tools/Game Setup/Sổ Tay Côn Trùng (Bestiary)")]
    public static void ShowWindow()
    {
        GetWindow<EncyclopediaSetupTool>("Sổ Tay Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Khởi tạo hệ thống Bách Khoa Toàn Thư", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("1. Tạo Dữ Liệu Mẫu (Resources/Encyclopedia)", GUILayout.Height(30)))
        {
            CreateSampleData();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("2. Tạo UI Canvas Bách Khoa Toàn Thư", GUILayout.Height(30)))
        {
            CreateEncyclopediaUI();
        }
    }

    private void CreateSampleData()
    {
        string folderPath = "Assets/Resources/Encyclopedia";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
            
        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/Resources", "Encyclopedia");

        string[] samples = { "DeMen", "DeChoat", "XenToc", "ConKien", "DeTrui" };
        string[] names = { "Dế Mèn", "Dế Choắt", "Xén Tóc", "Kiến Quân Đội", "Dế Trũi" };
        
        for (int i = 0; i < samples.Length; i++)
        {
            string path = $"{folderPath}/{samples[i]}.asset";
            if (AssetDatabase.LoadAssetAtPath<InsectData>(path) == null)
            {
                InsectData data = ScriptableObject.CreateInstance<InsectData>();
                data.insectID = samples[i];
                data.insectName = names[i];
                data.description = "Mô tả sinh học về " + names[i] + ". \nHãy cập nhật thêm tại file thiết kế (Data).";
                data.funFact = "Trí khôn dân gian: " + names[i] + " thường xuất hiện ở đâu?";
                data.dangerLevel = InsectDangerLevel.HienLanh;
                
                AssetDatabase.CreateAsset(data, path);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("<color=green>✅ Đã tạo Dữ liệu Sổ tay mẫu tại Assets/Resources/Encyclopedia</color>");
    }

    private void CreateEncyclopediaUI()
    {
        // Tự động đảm bảo có thư mục Data nếu người dùng quên ấn
        CreateSampleData();

        // Xóa cũ nếu có
        var oldCanvas = GameObject.Find("EncyclopediaCanvas");
        if (oldCanvas) DestroyImmediate(oldCanvas);
        var oldManager = GameObject.Find("EncyclopediaManager");
        if (oldManager) DestroyImmediate(oldManager);

        // 1. Manager Object
        GameObject manager = new GameObject("EncyclopediaManager");
        EncyclopediaManager encyManager = manager.AddComponent<EncyclopediaManager>();

        // 2. Canvas
        GameObject canvasObj = new GameObject("EncyclopediaCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90; 
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        // Script EventSystemCleaner đã tự xử lý phần EventSystem trùng lặp ở Runtime.
        // Không gọi sinh rác thêm EventSystem ở đây nữa.

        // 3. Notification UI (Popup)
        GameObject notifObj = new GameObject("NotificationAnchor");
        notifObj.transform.SetParent(canvasObj.transform, false);
        EncyclopediaNotificationUI notifUI = notifObj.AddComponent<EncyclopediaNotificationUI>();
        
        RectTransform notifRT = notifObj.AddComponent<RectTransform>();
        notifRT.sizeDelta = new Vector2(500, 100);
        Image notifBg = notifObj.AddComponent<Image>();
        notifBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f); // Đen sang trọng

        GameObject notifIcon = CreateImage("Icon", notifObj.transform, new Rect(15, 15, 70, 70));
        GameObject notifTitle = CreateText("Title", notifObj.transform, new Rect(100, 60, 350, 30), "KHÁM PHÁ SINH VẬT MỚI!", 18, new Color(1f, 0.8f, 0.2f), TextAlignmentOptions.Left);
        GameObject notifName = CreateText("Name", notifObj.transform, new Rect(100, 15, 350, 40), "Tên Côn Trùng", 30, Color.white, TextAlignmentOptions.Left);
        
        notifUI.container = notifRT;
        notifUI.insectIcon = notifIcon.GetComponent<Image>();
        notifUI.titleTxt = notifTitle.GetComponent<TextMeshProUGUI>();
        notifUI.insectNameTxt = notifName.GetComponent<TextMeshProUGUI>();

        // 4. Main Panel (Overlay)
        GameObject mainPanel = new GameObject("MainEncyclopediaPanel");
        mainPanel.transform.SetParent(canvasObj.transform, false);
        RectTransform mainRT = mainPanel.AddComponent<RectTransform>();
        mainRT.anchorMin = Vector2.zero; mainRT.anchorMax = Vector2.one; 
        mainRT.sizeDelta = Vector2.zero;
        Image mainBg = mainPanel.AddComponent<Image>(); 
        mainBg.color = new Color(0, 0, 0, 0.85f); // Tối màu nền game

        // Nút Close bự ẩn đằng sau (Bấm ra ngoài để tắt)
        Button bgCloseBtn = mainPanel.AddComponent<Button>();

        // -- CUỐN SÁCH TỔNG (Book Container) --
        GameObject bookObj = new GameObject("BookContainer");
        bookObj.transform.SetParent(mainPanel.transform, false);
        RectTransform bookRT = bookObj.AddComponent<RectTransform>();
        bookRT.anchorMin = new Vector2(0.5f, 0.5f); bookRT.anchorMax = new Vector2(0.5f, 0.5f);
        bookRT.sizeDelta = new Vector2(1400, 800);
        bookRT.anchoredPosition = Vector2.zero;
        
        Image bookBg = bookObj.AddComponent<Image>();
        bookBg.color = new Color(0.96f, 0.94f, 0.88f); // Màu giấy ngả vàng sang trọng

        // Title chính
        GameObject mainTitle = CreateText("MainTitle", bookObj.transform, new Rect(0, 0, 1400, 80), "BÁCH KHOA TOÀN THƯ", 48, new Color(0.2f, 0.2f, 0.2f), TextAlignmentOptions.Center);
        mainTitle.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        RectTransform mtRT = mainTitle.GetComponent<RectTransform>();
        mtRT.anchorMin = new Vector2(0, 1); mtRT.anchorMax = new Vector2(1, 1);
        mtRT.pivot = new Vector2(0.5f, 1); mtRT.anchoredPosition = new Vector2(0, -30);

        // Nút Close nhỏ (trên góc Cuốn Sách)
        GameObject closeBtnObj = new GameObject("CloseButton");
        closeBtnObj.transform.SetParent(bookObj.transform, false);
        Image cbImg = closeBtnObj.AddComponent<Image>();
        cbImg.color = new Color(0.9f, 0.2f, 0.2f);
        Button cbBtn = closeBtnObj.AddComponent<Button>();
        RectTransform cbRT = closeBtnObj.GetComponent<RectTransform>();
        cbRT.anchorMin = new Vector2(1, 1); cbRT.anchorMax = new Vector2(1, 1);
        cbRT.pivot = new Vector2(1, 1); cbRT.anchoredPosition = new Vector2(-20, -20);
        cbRT.sizeDelta = new Vector2(50, 50);
        GameObject xTxt = CreateText("Text", closeBtnObj.transform, new Rect(0, 0, 50, 50), "X", 30, Color.white, TextAlignmentOptions.Center);
        xTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        xTxt.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;

        // -- Horizontal Layout cho 2 trang sách --
        GameObject pagesObj = new GameObject("Pages");
        pagesObj.transform.SetParent(bookObj.transform, false);
        RectTransform pagesRT = pagesObj.AddComponent<RectTransform>();
        pagesRT.anchorMin = Vector2.zero; pagesRT.anchorMax = Vector2.one; 
        pagesRT.offsetMin = new Vector2(40, 40); pagesRT.offsetMax = new Vector2(-40, -120); // Chừa phần title
        
        HorizontalLayoutGroup hlg = pagesObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 60;
        hlg.childControlWidth = true; hlg.childControlHeight = true;
        hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;

        // --- TRANG TRÁI (DANH SÁCH) ---
        GameObject leftPage = new GameObject("LeftPage_List");
        leftPage.transform.SetParent(pagesObj.transform, false);
        Image leftBg = leftPage.AddComponent<Image>();
        leftBg.color = new Color(0, 0, 0, 0.05f); // Làm nền danh sách chìm đi 1 tí
        LayoutElement leftLE = leftPage.AddComponent<LayoutElement>();
        leftLE.flexibleWidth = 1f;

        GameObject scrollView = CreateScrollView(leftPage.transform);
        Transform contentTransform = scrollView.transform.Find("Viewport/Content");

        // --- TRANG PHẢI (CHI TIẾT) ---
        GameObject rightPage = new GameObject("RightPage_Detail");
        rightPage.transform.SetParent(pagesObj.transform, false);
        LayoutElement rightLE = rightPage.AddComponent<LayoutElement>();
        rightLE.flexibleWidth = 1.2f;

        // Layout Dọc cho Chi tiết (Giúp không bị đè chữ)
        VerticalLayoutGroup detailVlg = rightPage.AddComponent<VerticalLayoutGroup>();
        detailVlg.spacing = 15;
        detailVlg.padding = new RectOffset(40, 40, 10, 10); // Margins
        detailVlg.childAlignment = TextAnchor.UpperCenter;
        detailVlg.childControlHeight = false; detailVlg.childControlWidth = true;
        detailVlg.childForceExpandHeight = false; detailVlg.childForceExpandWidth = false;

        // Hình ảnh bự
        GameObject detailImg = CreateImage("DetailIcon", rightPage.transform, new Rect(0, 0, 250, 250));
        LayoutElement imgLE = detailImg.AddComponent<LayoutElement>();
        imgLE.preferredHeight = 250; imgLE.preferredWidth = 250;
        Image dImgComp = detailImg.GetComponent<Image>();
        dImgComp.preserveAspect = true; // Giữ tỉ lệ ảnh gốc

        // Tên Sinh vật
        GameObject detailName = CreateText("DetailName", rightPage.transform, new Rect(0, 0, 0, 50), "Tên Côn Trùng", 45, new Color(0.2f,0.2f,0.2f), TextAlignmentOptions.Center);
        detailName.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        LayoutElement nameLE = detailName.AddComponent<LayoutElement>();
        nameLE.preferredHeight = 50;

        // Mức độ nguy hiểm
        GameObject detailDanger = CreateText("DetailDanger", rightPage.transform, new Rect(0, 0, 0, 35), "Mức độ:", 24, Color.black, TextAlignmentOptions.Center);
        detailDanger.GetComponent<TextMeshProUGUI>().richText = true;
        LayoutElement dangerLE = detailDanger.AddComponent<LayoutElement>();
        dangerLE.preferredHeight = 35;

        // Mô tả chi tiết
        GameObject detailDesc = CreateText("DetailDesc", rightPage.transform, new Rect(0, 0, 0, 130), "Mô tả...", 22, new Color(0.3f, 0.3f, 0.3f), TextAlignmentOptions.TopLeft);
        detailDesc.GetComponent<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.Normal;
        LayoutElement descLE = detailDesc.AddComponent<LayoutElement>();
        descLE.preferredHeight = 130;

        // Sự thật thú vị
        GameObject detailFact = CreateText("DetailFact", rightPage.transform, new Rect(0, 0, 0, 100), "Sự thật thú vị...", 22, new Color(0.2f, 0.4f, 0.8f), TextAlignmentOptions.TopLeft);
        detailFact.GetComponent<TextMeshProUGUI>().textWrappingMode = TextWrappingModes.Normal;
        detailFact.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Italic;
        LayoutElement factLE = detailFact.AddComponent<LayoutElement>();
        factLE.preferredHeight = 100;

        // 5. Kết nối Script
        EncyclopediaUI uiScript = canvasObj.AddComponent<EncyclopediaUI>();
        uiScript.mainPanel = mainPanel;
        uiScript.listContent = contentTransform;
        uiScript.closeBtn = cbBtn;
        bgCloseBtn.onClick.AddListener(uiScript.Close); // Bấm ra nền đen cũng tắt
        
        uiScript.detailImage = dImgComp;
        uiScript.detailNameTxt = detailName.GetComponent<TextMeshProUGUI>();
        uiScript.detailDescTxt = detailDesc.GetComponent<TextMeshProUGUI>();
        uiScript.detailDangerTxt = detailDanger.GetComponent<TextMeshProUGUI>();
        uiScript.detailFactTxt = detailFact.GetComponent<TextMeshProUGUI>();

        // 6. Nút Bấm List Item Prefab đẹp hơn
        GameObject prefabBtn = new GameObject("InsectButtonPrefab");
        Image pBg = prefabBtn.AddComponent<Image>();
        pBg.color = new Color(0.9f, 0.9f, 0.9f); // Nút xám nhạt
        Button pBtn = prefabBtn.AddComponent<Button>();
        
        // Horizontal Layout cho nút
        HorizontalLayoutGroup btnHlg = prefabBtn.AddComponent<HorizontalLayoutGroup>();
        btnHlg.padding = new RectOffset(10, 10, 10, 10);
        btnHlg.spacing = 20;
        btnHlg.childControlHeight = true; btnHlg.childControlWidth = false;
        btnHlg.childForceExpandHeight = true; btnHlg.childForceExpandWidth = false;

        RectTransform pRT = prefabBtn.GetComponent<RectTransform>();
        pRT.sizeDelta = new Vector2(0, 100);

        GameObject pIconObj = CreateImage("IconMask", prefabBtn.transform, new Rect(0, 0, 80, 80));
        LayoutElement piLE = pIconObj.AddComponent<LayoutElement>();
        piLE.preferredWidth = 80; piLE.preferredHeight = 80;
        Image pIconMask = pIconObj.GetComponent<Image>();
        pIconMask.color = Color.white; 
        
        GameObject pIcon = CreateImage("IconImg", pIconObj.transform, new Rect(0, 0, 80, 80));
        Image realIcon = pIcon.GetComponent<Image>();
        realIcon.preserveAspect = true;
        RectTransform riRT = pIcon.GetComponent<RectTransform>();
        riRT.anchorMin = Vector2.zero; riRT.anchorMax = Vector2.one; riRT.sizeDelta = Vector2.zero;

        GameObject pName = CreateText("Name", prefabBtn.transform, new Rect(0, 0, 300, 80), "Côn trùng", 28, new Color(0.2f,0.2f,0.2f), TextAlignmentOptions.Left);
        pName.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
        pName.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
        LayoutElement pnLE = pName.AddComponent<LayoutElement>();
        pnLE.preferredWidth = 400; // Chiếm hết bề ngang còn lại

        uiScript.insectBtnPrefab = prefabBtn;
        prefabBtn.SetActive(false); // Ẩn khỏi scene
        prefabBtn.transform.SetParent(canvasObj.transform, false);

        mainPanel.SetActive(false);

        Undo.RegisterCreatedObjectUndo(manager, "Create Encyclopedia Manager");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create Encyclopedia Canvas");
        
        Debug.Log("<color=green>✅ Đã xậy dựng UI Bách Khoa Toàn Thư Hoàn Hảo!</color>");
    }

    private GameObject CreateImage(string name, Transform parent, Rect rect)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        Image img = obj.AddComponent<Image>();
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(rect.x, rect.y);
        rt.sizeDelta = new Vector2(rect.width, rect.height);
        return obj;
    }

    private GameObject CreateText(string name, Transform parent, Rect rect, string text, float fontSize, Color color, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(rect.x, rect.y);
        rt.sizeDelta = new Vector2(rect.width, rect.height);
        return obj;
    }

    private GameObject CreateScrollView(Transform parent)
    {
        GameObject sv = new GameObject("Scroll View");
        sv.transform.SetParent(parent, false);
        RectTransform svRT = sv.AddComponent<RectTransform>();
        svRT.anchorMin = Vector2.zero; svRT.anchorMax = Vector2.one; svRT.sizeDelta = Vector2.zero;
        ScrollRect scrollRect = sv.AddComponent<ScrollRect>();
        Image svImg = sv.AddComponent<Image>();
        svImg.color = new Color(0,0,0,0.1f);

        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(sv.transform, false);
        RectTransform viewportRT = viewport.AddComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero; viewportRT.anchorMax = Vector2.one; viewportRT.sizeDelta = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1); contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1); contentRT.sizeDelta = new Vector2(0, 300);

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.spacing = 10;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        ContentSizeFitter csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRT;
        scrollRect.viewport = viewportRT;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        return sv;
    }
}
