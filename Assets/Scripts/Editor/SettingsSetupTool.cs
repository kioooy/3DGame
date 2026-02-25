using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor Tool: Tự động tạo toàn bộ UI Settings Menu trong scene.
/// Menu: Tools > Settings System > Setup Settings UI
/// </summary>
public class SettingsSetupTool : EditorWindow
{
    [MenuItem("Tools/Settings System/Setup Settings UI")]
    static void ShowWindow()
    {
        var w = GetWindow<SettingsSetupTool>("Settings Setup");
        w.minSize = new Vector2(480, 420);
        w.Show();
    }

    Vector2 _scroll;

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        GUILayout.Label("⚙  Settings System Setup", EditorStyles.boldLabel);
        GUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Tool này tạo hoàn toàn tự động:\n\n" +
            "☑  SettingsManager (DontDestroyOnLoad)\n" +
            "☑  SettingsCanvas (Screen Space Overlay)\n" +
            "☑  Menu Cài Đặt với 3 tab:\n" +
            "     • 🔊 Âm Thanh  (3 slider âm lượng)\n" +
            "     • 🖥  Đồ Hoạ    (chất lượng, độ phân giải, toàn màn hình)\n" +
            "     • 🎮 Điều Khiển (độ nhạy chuột, đảo trục Y)\n\n" +
            "Sau khi setup: nhấn ESC trong game để mở/đóng menu.",
            MessageType.Info);

        GUILayout.Space(12);

        // Status
        EditorGUILayout.LabelField("Trạng thái hiện tại:", EditorStyles.boldLabel);
        var mgr = FindFirstObjectByType<SettingsManager>();
        var ui  = FindFirstObjectByType<SettingsUI>();

        GUI.color = mgr != null ? Color.green : Color.yellow;
        EditorGUILayout.LabelField($"• SettingsManager: {(mgr != null ? "✓ Có trong scene" : "⚠ Chưa có")}");
        GUI.color = ui != null ? Color.green : Color.yellow;
        EditorGUILayout.LabelField($"• SettingsUI: {(ui != null ? "✓ Có trong scene" : "⚠ Chưa có")}");
        GUI.color = Color.white;

        GUILayout.Space(16);

        if (GUILayout.Button("🚀  Tạo Settings System Hoàn Chỉnh", GUILayout.Height(48)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận",
                "Tạo Settings System?\nNếu đã có sẽ xoá và tạo lại.", "Tạo ngay!", "Huỷ"))
                Build();
        }

        GUILayout.Space(6);

        if (GUILayout.Button("🗑  Xoá Settings System", GUILayout.Height(32)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Xoá tất cả Settings objects?", "Xoá", "Huỷ"))
                Cleanup();
        }

        EditorGUILayout.EndScrollView();
    }

    // ══════════════════════════════════════════════════════════
    //  BUILD
    // ══════════════════════════════════════════════════════════
    void Build()
    {
        Cleanup();

        // 1. SettingsManager
        var mgrGO = new GameObject("SettingsManager");
        mgrGO.AddComponent<SettingsManager>();
        Undo.RegisterCreatedObjectUndo(mgrGO, "Create SettingsManager");

        // 2. Canvas
        var canvasGO = new GameObject("SettingsCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create SettingsCanvas");

        // 3. Backdrop (mờ nền)
        var backdrop = CreatePanel(canvasGO.transform, "Backdrop",
            new Color(0, 0, 0, 0.55f), Vector2.zero, Vector2.one, new Vector2(0,0), new Vector2(0,0));

        // 4. Settings Panel (cửa sổ chính)
        var panel = CreatePanel(canvasGO.transform, "SettingsPanel",
            new Color(0.09f, 0.09f, 0.12f, 0.97f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(900, 580));

        AddOutline(panel, new Color(0.35f, 0.7f, 0.45f, 1f));

        // 5. Tiêu đề
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(panel.transform, false);
        var titleRect = titleGO.AddComponent<RectTransform>();
        titleRect.anchorMin  = new Vector2(0, 1);
        titleRect.anchorMax  = new Vector2(1, 1);
        titleRect.pivot      = new Vector2(0.5f, 1);
        titleRect.offsetMin  = new Vector2(20, 0);
        titleRect.offsetMax  = new Vector2(-20, 0);
        titleRect.sizeDelta  = new Vector2(0, 64);
        var titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text      = "⚙  CÀI ĐẶT";
        titleTmp.fontSize  = 32;
        titleTmp.fontStyle = FontStyles.Bold;
        titleTmp.color     = new Color(0.35f, 0.85f, 0.5f);
        titleTmp.alignment = TextAlignmentOptions.Center;

        // 6. Tab Buttons bar
        var tabBar = new GameObject("TabBar");
        tabBar.transform.SetParent(panel.transform, false);
        var tabRect = tabBar.AddComponent<RectTransform>();
        tabRect.anchorMin = new Vector2(0, 1);
        tabRect.anchorMax = new Vector2(1, 1);
        tabRect.pivot     = new Vector2(0.5f, 1);
        tabRect.anchoredPosition = new Vector2(0, -64);
        tabRect.sizeDelta = new Vector2(0, 48);
        var tabLayout = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabLayout.spacing = 6;
        tabLayout.padding = new RectOffset(16, 16, 4, 4);
        tabLayout.childForceExpandWidth = true;

        var tabAudioBtn    = CreateTabButton(tabBar.transform, "🔊 Âm Thanh");
        var tabGraphicsBtn = CreateTabButton(tabBar.transform, "🖥  Đồ Hoạ");
        var tabControlBtn  = CreateTabButton(tabBar.transform, "🎮 Điều Khiển");

        // 7. Content area
        var contentArea = new GameObject("ContentArea");
        contentArea.transform.SetParent(panel.transform, false);
        var contentRect = contentArea.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 0);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.offsetMin = new Vector2(16, 70);
        contentRect.offsetMax = new Vector2(-16, -114);

        // 8. Tab Panels
        var panelAudio    = CreateTabPanel(contentArea.transform, "PanelAudio");
        var panelGraphics = CreateTabPanel(contentArea.transform, "PanelGraphics");
        var panelControls = CreateTabPanel(contentArea.transform, "PanelControls");

        // ── Audio content ──
        Slider masterSlider, musicSlider, sfxSlider;
        TextMeshProUGUI masterLabel, musicLabel, sfxLabel;
        BuildAudioPanel(panelAudio.transform,
            out masterSlider, out masterLabel,
            out musicSlider,  out musicLabel,
            out sfxSlider,    out sfxLabel);

        // ── Graphics content ──
        TMP_Dropdown qualityDD, resDD;
        Toggle fullscreenToggle;
        BuildGraphicsPanel(panelGraphics.transform, out qualityDD, out resDD, out fullscreenToggle);

        // ── Controls content ──
        Slider sensiSlider;
        TextMeshProUGUI sensiLabel;
        Toggle invertToggle;
        BuildControlsPanel(panelControls.transform, out sensiSlider, out sensiLabel, out invertToggle);

        // 9. Bottom buttons
        var btnApply = CreateButton(panel.transform, "BtnApply", "💾 Lưu & Áp Dụng",
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-130, 18), new Vector2(200, 44), new Color(0.2f, 0.65f, 0.35f));
        var btnReset = CreateButton(panel.transform, "BtnReset", "↺ Mặc Định",
            new Vector2(0.5f, 0), new Vector2(0.5f, 0),
            new Vector2(0, 18), new Vector2(160, 44), new Color(0.45f, 0.35f, 0.1f));
        var btnClose = CreateButton(panel.transform, "BtnClose", "✕",
            new Vector2(1, 1), new Vector2(1, 1),
            new Vector2(-14, -14), new Vector2(40, 40), new Color(0.7f, 0.15f, 0.15f));

        // 10. Add SettingsUI component and wire up
        var settingsUI = canvasGO.AddComponent<SettingsUI>();
        var so = new SerializedObject(settingsUI);

        so.FindProperty("settingsPanel").objectReferenceValue    = panel;
        so.FindProperty("tabAudioBtn").objectReferenceValue      = tabAudioBtn;
        so.FindProperty("tabGraphicsBtn").objectReferenceValue   = tabGraphicsBtn;
        so.FindProperty("tabControlsBtn").objectReferenceValue   = tabControlBtn;
        so.FindProperty("panelAudio").objectReferenceValue       = panelAudio;
        so.FindProperty("panelGraphics").objectReferenceValue    = panelGraphics;
        so.FindProperty("panelControls").objectReferenceValue    = panelControls;

        so.FindProperty("sliderMaster").objectReferenceValue     = masterSlider;
        so.FindProperty("labelMaster").objectReferenceValue      = masterLabel;
        so.FindProperty("sliderMusic").objectReferenceValue      = musicSlider;
        so.FindProperty("labelMusic").objectReferenceValue       = musicLabel;
        so.FindProperty("sliderSFX").objectReferenceValue        = sfxSlider;
        so.FindProperty("labelSFX").objectReferenceValue         = sfxLabel;

        so.FindProperty("dropdownQuality").objectReferenceValue    = qualityDD;
        so.FindProperty("dropdownResolution").objectReferenceValue = resDD;
        so.FindProperty("toggleFullscreen").objectReferenceValue   = fullscreenToggle;

        so.FindProperty("sliderSensitivity").objectReferenceValue  = sensiSlider;
        so.FindProperty("labelSensitivity").objectReferenceValue   = sensiLabel;
        so.FindProperty("toggleInvertY").objectReferenceValue      = invertToggle;

        so.FindProperty("btnApply").objectReferenceValue  = btnApply;
        so.FindProperty("btnReset").objectReferenceValue  = btnReset;
        so.FindProperty("btnClose").objectReferenceValue  = btnClose;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(settingsUI);

        Debug.Log("[SettingsSetupTool] ✅ Setup hoàn tất! Nhấn ESC trong Game Mode để mở.");
        EditorUtility.DisplayDialog("✅ Thành Công!",
            "Settings System đã được tạo!\n\n" +
            "Điều khiển:\n" +
            "• ESC → Mở / Đóng menu\n" +
            "• Tab Âm Thanh: điều chỉnh âm lượng\n" +
            "• Tab Đồ Hoạ: chất lượng, độ phân giải\n" +
            "• Tab Điều Khiển: độ nhạy chuột\n" +
            "• Nút 'Lưu & Áp Dụng' để lưu vĩnh viễn",
            "Tuyệt vời!");

        Selection.activeGameObject = canvasGO;
    }

    // ══════════════════════════════════════════════════════════
    //  PANEL BUILDERS
    // ══════════════════════════════════════════════════════════
    void BuildAudioPanel(Transform parent,
        out Slider masterSlider, out TextMeshProUGUI masterLabel,
        out Slider musicSlider,  out TextMeshProUGUI musicLabel,
        out Slider sfxSlider,    out TextMeshProUGUI sfxLabel)
    {
        float y = -20;
        masterSlider = BuildSliderRow(parent, "🔊 Âm lượng tổng",   ref y, 0, 1, 1f,   out masterLabel);
        musicSlider  = BuildSliderRow(parent, "🎵 Âm nhạc nền",      ref y, 0, 1, 0.8f, out musicLabel);
        sfxSlider    = BuildSliderRow(parent, "💥 Hiệu ứng âm thanh", ref y, 0, 1, 1f,   out sfxLabel);
    }

    void BuildGraphicsPanel(Transform parent,
        out TMP_Dropdown qualityDD, out TMP_Dropdown resDD, out Toggle fullToggle)
    {
        float y = -20;
        qualityDD   = BuildDropdownRow(parent, "⭐ Chất lượng đồ hoạ", ref y);
        resDD       = BuildDropdownRow(parent, "📐 Độ phân giải",       ref y);
        fullToggle  = BuildToggleRow(parent,   "🖥  Toàn màn hình",     ref y, true);
    }

    void BuildControlsPanel(Transform parent,
        out Slider sensiSlider, out TextMeshProUGUI sensiLabel, out Toggle invertToggle)
    {
        float y = -20;
        sensiSlider  = BuildSliderRow(parent, "🖱  Độ nhạy chuột", ref y, 0.5f, 10f, 2f, out sensiLabel);
        invertToggle = BuildToggleRow(parent, "🔃 Đảo trục Y",     ref y, false);
    }

    // ══════════════════════════════════════════════════════════
    //  ROW BUILDERS
    // ══════════════════════════════════════════════════════════
    Slider BuildSliderRow(Transform parent, string labelText, ref float yPos,
        float min, float max, float defaultVal, out TextMeshProUGUI valueLabel)
    {
        var row = new GameObject($"Row_{labelText}");
        row.transform.SetParent(parent, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(1, 1);
        rowRect.pivot     = new Vector2(0.5f, 1);
        rowRect.anchoredPosition = new Vector2(0, yPos);
        rowRect.sizeDelta = new Vector2(0, 52);
        yPos -= 58;

        // Label
        var lbl = new GameObject("Label");
        lbl.transform.SetParent(row.transform, false);
        var lblRect = lbl.AddComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0, 0);
        lblRect.anchorMax = new Vector2(0.35f, 1);
        lblRect.offsetMin = new Vector2(8, 4);
        lblRect.offsetMax = new Vector2(0, -4);
        var lblTmp = lbl.AddComponent<TextMeshProUGUI>();
        lblTmp.text      = labelText;
        lblTmp.fontSize  = 18;
        lblTmp.color     = Color.white;
        lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Value label
        var valGO = new GameObject("Value");
        valGO.transform.SetParent(row.transform, false);
        var valRect = valGO.AddComponent<RectTransform>();
        valRect.anchorMin = new Vector2(0.85f, 0);
        valRect.anchorMax = new Vector2(1, 1);
        valRect.offsetMin = new Vector2(4, 4);
        valRect.offsetMax = new Vector2(-8, -4);
        valueLabel = valGO.AddComponent<TextMeshProUGUI>();
        valueLabel.text      = Mathf.RoundToInt(defaultVal * 100) + "%";
        valueLabel.fontSize  = 16;
        valueLabel.color     = new Color(0.7f, 1f, 0.8f);
        valueLabel.alignment = TextAlignmentOptions.MidlineRight;

        // Slider  
        var sliderGO = new GameObject("Slider");
        sliderGO.transform.SetParent(row.transform, false);
        var sliderRect = sliderGO.AddComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.35f, 0);
        sliderRect.anchorMax = new Vector2(0.85f, 1);
        sliderRect.offsetMin = new Vector2(8, 10);
        sliderRect.offsetMax = new Vector2(-8, -10);

        var slider = sliderGO.AddComponent<Slider>();

        // Background
        var bg = CreateImageChild(sliderGO.transform, "Background", new Color(0.2f, 0.2f, 0.2f));
        var bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = bgRect.offsetMax = Vector2.zero;

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGO.transform, false);
        var faRect = fillArea.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero;
        faRect.anchorMax = Vector2.one;
        faRect.offsetMin = faRect.offsetMax = Vector2.zero;

        var fill = CreateImageChild(fillArea.transform, "Fill", new Color(0.25f, 0.75f, 0.4f));
        var fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(defaultVal, 1);
        fillRect.offsetMin = fillRect.offsetMax = Vector2.zero;

        // Handle
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderGO.transform, false);
        var haRect = handleArea.AddComponent<RectTransform>();
        haRect.anchorMin = Vector2.zero;
        haRect.anchorMax = Vector2.one;
        haRect.offsetMin = haRect.offsetMax = Vector2.zero;

        var handle = CreateImageChild(handleArea.transform, "Handle", new Color(0.9f, 0.9f, 0.95f));
        var handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 0);
        handleRect.anchorMin = new Vector2(defaultVal, 0);
        handleRect.anchorMax = new Vector2(defaultVal, 1);

        slider.fillRect   = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();
        slider.direction  = Slider.Direction.LeftToRight;
        slider.minValue   = min;
        slider.maxValue   = max;
        slider.value      = defaultVal;

        return slider;
    }

    TMP_Dropdown BuildDropdownRow(Transform parent, string labelText, ref float yPos)
    {
        var row = new GameObject($"Row_{labelText}");
        row.transform.SetParent(parent, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(1, 1);
        rowRect.pivot     = new Vector2(0.5f, 1);
        rowRect.anchoredPosition = new Vector2(0, yPos);
        rowRect.sizeDelta = new Vector2(0, 52);
        yPos -= 58;

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(row.transform, false);
        var lblRect = lbl.AddComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0, 0);
        lblRect.anchorMax = new Vector2(0.4f, 1);
        lblRect.offsetMin = new Vector2(8, 4);
        lblRect.offsetMax = new Vector2(0, -4);
        var lblTmp = lbl.AddComponent<TextMeshProUGUI>();
        lblTmp.text = labelText; lblTmp.fontSize = 18; lblTmp.color = Color.white;
        lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var ddGO = new GameObject("Dropdown");
        ddGO.transform.SetParent(row.transform, false);
        var ddRect = ddGO.AddComponent<RectTransform>();
        ddRect.anchorMin = new Vector2(0.4f, 0);
        ddRect.anchorMax = new Vector2(1f, 1);
        ddRect.offsetMin = new Vector2(8, 6);
        ddRect.offsetMax = new Vector2(-8, -6);

        var ddBg = ddGO.AddComponent<Image>();
        ddBg.color = new Color(0.18f, 0.18f, 0.22f);

        var dd = ddGO.AddComponent<TMP_Dropdown>();

        var captionGO = new GameObject("Label");
        captionGO.transform.SetParent(ddGO.transform, false);
        var cRect = captionGO.AddComponent<RectTransform>();
        cRect.anchorMin = Vector2.zero; cRect.anchorMax = Vector2.one;
        cRect.offsetMin = new Vector2(8, 2); cRect.offsetMax = new Vector2(-24, -2);
        var cTmp = captionGO.AddComponent<TextMeshProUGUI>();
        cTmp.fontSize = 16; cTmp.color = Color.white;
        cTmp.alignment = TextAlignmentOptions.MidlineLeft;

        dd.captionText = cTmp;
        dd.AddOptions(new System.Collections.Generic.List<string> { "Option 1" });

        return dd;
    }

    Toggle BuildToggleRow(Transform parent, string labelText, ref float yPos, bool defaultVal)
    {
        var row = new GameObject($"Row_{labelText}");
        row.transform.SetParent(parent, false);
        var rowRect = row.AddComponent<RectTransform>();
        rowRect.anchorMin = new Vector2(0, 1);
        rowRect.anchorMax = new Vector2(1, 1);
        rowRect.pivot     = new Vector2(0.5f, 1);
        rowRect.anchoredPosition = new Vector2(0, yPos);
        rowRect.sizeDelta = new Vector2(0, 52);
        yPos -= 58;

        var lbl = new GameObject("Label");
        lbl.transform.SetParent(row.transform, false);
        var lblRect = lbl.AddComponent<RectTransform>();
        lblRect.anchorMin = new Vector2(0, 0);
        lblRect.anchorMax = new Vector2(0.8f, 1);
        lblRect.offsetMin = new Vector2(8, 4);
        lblRect.offsetMax = new Vector2(0, -4);
        var lblTmp = lbl.AddComponent<TextMeshProUGUI>();
        lblTmp.text = labelText; lblTmp.fontSize = 18; lblTmp.color = Color.white;
        lblTmp.alignment = TextAlignmentOptions.MidlineLeft;

        var tglGO = new GameObject("Toggle");
        tglGO.transform.SetParent(row.transform, false);
        var tRect = tglGO.AddComponent<RectTransform>();
        tRect.anchorMin = new Vector2(0.85f, 0.1f);
        tRect.anchorMax = new Vector2(0.85f, 0.9f);
        tRect.sizeDelta = new Vector2(52, 0);

        var bgImg = tglGO.AddComponent<Image>();
        bgImg.color = new Color(0.25f, 0.25f, 0.3f);

        var checkGO = CreateImageChild(tglGO.transform, "Checkmark", new Color(0.3f, 0.8f, 0.45f));
        var ckRect = checkGO.GetComponent<RectTransform>();
        ckRect.anchorMin = new Vector2(0.1f, 0.1f);
        ckRect.anchorMax = new Vector2(0.9f, 0.9f);
        ckRect.offsetMin = ckRect.offsetMax = Vector2.zero;

        var tgl = tglGO.AddComponent<Toggle>();
        tgl.targetGraphic = bgImg;
        tgl.graphic = checkGO.GetComponent<Image>();
        tgl.isOn = defaultVal;

        return tgl;
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════
    GameObject CreatePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
        return go;
    }

    GameObject CreateTabPanel(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        go.SetActive(false);
        return go;
    }

    Button CreateTabButton(Transform parent, string label)
    {
        var go = new GameObject($"Tab_{label}");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        var btn = go.AddComponent<Button>();

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var rect = txtGO.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 17;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    Button CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img  = go.AddComponent<Image>();
        img.color = color;
        var btn  = go.AddComponent<Button>();
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot     = anchorMin;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var tRect = txtGO.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.offsetMin = tRect.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 18; tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white; tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    void AddOutline(GameObject go, Color color)
    {
        var outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(2, -2);
    }

    GameObject CreateImageChild(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return go;
    }

    void Cleanup()
    {
        foreach (var ui in FindObjectsByType<SettingsUI>(FindObjectsSortMode.None))
            DestroyImmediate(ui.gameObject);
        foreach (var mgr in FindObjectsByType<SettingsManager>(FindObjectsSortMode.None))
            DestroyImmediate(mgr.gameObject);

        var canvasGO = GameObject.Find("SettingsCanvas");
        if (canvasGO) DestroyImmediate(canvasGO);
        var mgrGO = GameObject.Find("SettingsManager");
        if (mgrGO) DestroyImmediate(mgrGO);
        Debug.Log("[SettingsSetupTool] Đã dọn dẹp.");
    }
}
