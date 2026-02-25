using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor Tool: Tự động tạo toàn bộ UI Settings Menu cao cấp trong scene.
/// Menu: Tools > Settings System > Setup Settings UI
/// </summary>
public class SettingsSetupTool : EditorWindow
{
    [MenuItem("Tools/Settings System/Setup Settings UI")]
    static void ShowWindow()
    {
        var w = GetWindow<SettingsSetupTool>("⚙ Settings Setup");
        w.minSize = new Vector2(500, 460);
        w.Show();
    }

    Vector2 _scroll;

    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        GUILayout.Label("⚙  Settings System – Dark Premium UI", EditorStyles.boldLabel);
        GUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "✅ BUG ĐÃ FIX:\n" +
            "   • Resources.Load sprite lỗi → đã xóa\n" +
            "   • ForceClose không ẩn backdrop → đã fix\n" +
            "   • _originalTint chưa init → đã fix\n\n" +
            "🎨 UI MỚI:\n" +
            "   • Header gradient + icon\n" +
            "   • Tab indicator dạng underline\n" +
            "   • Slider dày hơn, màu viridian\n" +
            "   • Toggle kiểu pill (iOS style)\n" +
            "   • Bottom bar cách ly với đường kẻ",
            MessageType.Info);

        GUILayout.Space(12);

        var mgr = FindFirstObjectByType<SettingsManager>();
        var ui  = FindFirstObjectByType<SettingsUI>();
        EditorGUILayout.LabelField("Trạng thái:", EditorStyles.boldLabel);
        GUI.color = mgr != null ? Color.green : Color.yellow;
        EditorGUILayout.LabelField($"• SettingsManager: {(mgr != null ? "✓ OK" : "⚠ Chưa có")}");
        GUI.color = ui != null ? Color.green : Color.yellow;
        EditorGUILayout.LabelField($"• SettingsUI: {(ui != null ? "✓ OK" : "⚠ Chưa có")}");
        GUI.color = Color.white;

        GUILayout.Space(16);

        GUI.backgroundColor = new Color(0.2f, 0.65f, 0.35f);
        if (GUILayout.Button("🚀  Tạo / Tạo Lại Settings System", GUILayout.Height(50)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận",
                "Tạo Settings System? (Nếu đã có, xóa và tạo lại)", "Tạo ngay!", "Huỷ"))
                Build();
        }
        GUI.backgroundColor = Color.white;

        GUILayout.Space(6);
        GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
        if (GUILayout.Button("🗑  Xoá Settings System", GUILayout.Height(32)))
        {
            if (EditorUtility.DisplayDialog("Xác nhận", "Xoá tất cả Settings objects?", "Xoá", "Huỷ"))
                Cleanup();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();
    }

    // ══════════════════════════════════════════════════════════
    //  MÀUSẮC THEME
    // ══════════════════════════════════════════════════════════
    static Color C(string hex)
    {
        ColorUtility.TryParseHtmlString(hex, out var c);
        return c;
    }

    // Palette "Dark Forest" 
    static readonly Color BG_DEEP      = C("#0D1117");   // rất tối
    static readonly Color BG_PANEL     = C("#161B22");   // panel nền
    static readonly Color BG_INNER     = C("#0D1117");   // nội dung bên trong
    static readonly Color BORDER_COLOR = C("#30363D");   // viền ngăn cách
    static readonly Color ACCENT_GREEN = C("#3FB950");   // xanh lá nổi bật
    static readonly Color ACCENT_BLUE  = C("#58A6FF");   // xanh dương nhấn
    static readonly Color TAB_ACTIVE   = C("#21262D");   // tab đang chọn
    static readonly Color TAB_NORMAL   = C("#0D1117");   // tab bình thường
    static readonly Color TEXT_PRIMARY = C("#E6EDF3");   // chữ chính
    static readonly Color TEXT_MUTED   = C("#8B949E");   // chữ phụ
    static readonly Color SLIDER_BG    = C("#21262D");   // track slider
    static readonly Color BTN_GREEN    = C("#238636");   // nút lưu
    static readonly Color BTN_RESET    = C("#6E7681");   // nút reset
    static readonly Color BTN_CLOSE    = C("#8B1A1A");   // nút đóng X

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

        // 2. UIAudioFeedback
        var audioGO = new GameObject("UIAudioFeedback");
        audioGO.AddComponent<UIAudioFeedback>();
        Undo.RegisterCreatedObjectUndo(audioGO, "Create UIAudioFeedback");

        // 3. Canvas
        var canvasGO = new GameObject("SettingsCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler  = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create SettingsCanvas");

        // 4. Backdrop
        var backdrop    = MakeImage(canvasGO.transform, "Backdrop",
            new Color(0, 0, 0, 0)); // bắt đầu trong suốt, SettingsUI animate
        var backdropImg = backdrop.GetComponent<Image>();
        backdrop.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        backdrop.GetComponent<RectTransform>().anchorMax = Vector2.one;
        backdrop.GetComponent<RectTransform>().offsetMin = Vector2.zero;
        backdrop.GetComponent<RectTransform>().offsetMax = Vector2.zero;

        // 5. Main Window Panel  (950 x 640)
        var window = MakeRect(canvasGO.transform, "SettingsPanel",
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, new Vector2(950, 640));
        MakeImage(window.transform, "WindowBG", BG_PANEL)
            .GetComponent<RectTransform>().Fill();

        // CanvasGroup để animate
        var cg = window.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false;

        // Viền ngoài (đường kẻ xanh lá mỏng)
        AddSingleOutline(window.gameObject, ACCENT_GREEN, 1.5f);

        // ─────────── HEADER (70px) ───────────
        var header = MakeRect(window.transform, "Header",
            new Vector2(0,1), new Vector2(1,1),
            Vector2.zero, new Vector2(0, 72));
        var headerBG = MakeImage(header.transform, "HeaderBG",
            C("#0D1117")).GetComponent<Image>();
        headerBG.GetComponent<RectTransform>().Fill();

        // Dải màu gradient ngang trên cùng header
        var headerAccent = MakeRect(header.transform, "HeaderAccent",
            new Vector2(0,1), new Vector2(1,1),
            Vector2.zero, new Vector2(0, 3));
        MakeImage(headerAccent.transform, "AccentLine", ACCENT_GREEN)
            .GetComponent<RectTransform>().Fill();

        // Icon (dạng text)
        var iconGO = MakeText(header.transform, "Icon", "SET",
            20, ACCENT_GREEN, FontStyles.Bold, TextAlignmentOptions.Left);
        SetRect(iconGO, new Vector2(0,0), new Vector2(0,1),
            new Vector2(18, 0), new Vector2(56, 0));

        // Tiêu đề
        var titleGO = MakeText(header.transform, "Title", "CÀI ĐẶT",
            28, TEXT_PRIMARY, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        SetRect(titleGO, new Vector2(0,0), new Vector2(0.6f,1),
            new Vector2(76, 0), new Vector2(0, 0));

        // Hint ESC
        var hintGO = MakeText(header.transform, "Hint", "ESC để đóng",
            13, TEXT_MUTED, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
        SetRect(hintGO, new Vector2(0.6f,0), new Vector2(1,1),
            new Vector2(0, 0), new Vector2(-16, 0));
        
        // Nut X (close)
        var btnClose = MakePillButton(header.transform, "BtnClose", "X",
            BTN_CLOSE, TEXT_PRIMARY, 18, false);
        var bcRect = btnClose.GetComponent<RectTransform>();
        bcRect.anchorMin = new Vector2(1, 0.5f);
        bcRect.anchorMax = new Vector2(1, 0.5f);
        bcRect.pivot     = new Vector2(1, 0.5f);
        bcRect.anchoredPosition = new Vector2(-12, 0);
        bcRect.sizeDelta = new Vector2(38, 38);

        // ─────────── TAB BAR (40px) ───────────
        var tabBarGO = MakeRect(window.transform, "TabBar",
            new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, -72), new Vector2(0, 44));
        MakeImage(tabBarGO.transform, "TabBarBG", BG_DEEP).GetComponent<RectTransform>().Fill();

        // Dưới cùng tabbar: đường kẻ phân cách
        var tabDivider = MakeRect(tabBarGO.transform, "Divider",
            new Vector2(0, 0), new Vector2(1, 0),
            Vector2.zero, new Vector2(0, 1));
        MakeImage(tabDivider.transform, "DivLine", BORDER_COLOR).GetComponent<RectTransform>().Fill();

        var tabBtnAudio    = MakeTabButton(tabBarGO.transform, "Tab_Audio",    "Am Thanh");
        var tabBtnGraphics = MakeTabButton(tabBarGO.transform, "Tab_Graphics", "Do Hoa");
        var tabBtnControls = MakeTabButton(tabBarGO.transform, "Tab_Controls", "Dieu Khien");

        // Layout tab bar horizontal
        var tabLayout = tabBarGO.gameObject.AddComponent<HorizontalLayoutGroup>();
        tabLayout.padding               = new RectOffset(8, 8, 0, 0);
        tabLayout.spacing               = 2;
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = true;

        // ─────────── CONTENT AREA ───────────
        var contentArea = MakeRect(window.transform, "ContentArea",
            new Vector2(0,0), new Vector2(1,1),
            new Vector2(0, 60),   // bottom bar 60px
            new Vector2(0, -116)  // header 72 + tabbar 44
        );
        // Inner BG slightly different shade
        MakeImage(contentArea.transform, "ContentBG", BG_INNER).GetComponent<RectTransform>().Fill();

        const float PAD = 24;
        // Tab panel containers  
        var pAudio    = MakeTabPanel(contentArea.transform, "PanelAudio",    PAD);
        var pGraphics = MakeTabPanel(contentArea.transform, "PanelGraphics", PAD);
        var pControls = MakeTabPanel(contentArea.transform, "PanelControls", PAD);

        // ── Build content ──
        Slider sMaster, sMusic, sSFX;
        TextMeshProUGUI lMaster, lMusic, lSFX;
        BuildAudioPanel(pAudio.transform, out sMaster, out lMaster, out sMusic, out lMusic, out sSFX, out lSFX);

        TMP_Dropdown dQuality, dRes;
        Toggle tFullscreen;
        BuildGraphicsPanel(pGraphics.transform, out dQuality, out dRes, out tFullscreen);

        Slider sSensitivity;
        TextMeshProUGUI lSensitivity;
        Toggle tInvertY;
        BuildControlsPanel(pControls.transform, out sSensitivity, out lSensitivity, out tInvertY);

        // ─────────── BOTTOM BAR (60px) ───────────
        var bottomBar = MakeRect(window.transform, "BottomBar",
            new Vector2(0,0), new Vector2(1,0),
            Vector2.zero, new Vector2(0, 60));
        MakeImage(bottomBar.transform, "BottomBG", BG_DEEP).GetComponent<RectTransform>().Fill();

        // Đường kẻ phân cách trên bottombar
        var botDivider = MakeRect(bottomBar.transform, "Divider",
            new Vector2(0, 1), new Vector2(1, 1),
            Vector2.zero, new Vector2(0, 1));
        MakeImage(botDivider.transform, "DivLine", BORDER_COLOR).GetComponent<RectTransform>().Fill();

        // Nut Reset (trai)
        var btnReset = MakePillButton(bottomBar.transform, "BtnReset", "Mac Dinh",
            BTN_RESET, TEXT_PRIMARY, 16, true);
        PositionInBar(btnReset, Align.Left, 16, 12, 160, 36);

        // Nut Apply (phai)
        var btnApply = MakePillButton(bottomBar.transform, "BtnApply", "Luu va Ap Dung",
            BTN_GREEN, TEXT_PRIMARY, 16, true);
        PositionInBar(btnApply, Align.Right, 16, 12, 200, 36);

        // ─────────── SettingsUI Component ───────────
        var settingsUI  = canvasGO.AddComponent<SettingsUI>();
        var so = new SerializedObject(settingsUI);

        so.FindProperty("settingsPanel").objectReferenceValue      = window.gameObject;
        so.FindProperty("backdropImage").objectReferenceValue      = backdropImg;
        so.FindProperty("tabAudioBtn").objectReferenceValue        = tabBtnAudio;
        so.FindProperty("tabGraphicsBtn").objectReferenceValue     = tabBtnGraphics;
        so.FindProperty("tabControlsBtn").objectReferenceValue     = tabBtnControls;
        so.FindProperty("panelAudio").objectReferenceValue         = pAudio;
        so.FindProperty("panelGraphics").objectReferenceValue      = pGraphics;
        so.FindProperty("panelControls").objectReferenceValue      = pControls;
        so.FindProperty("sliderMaster").objectReferenceValue       = sMaster;
        so.FindProperty("labelMaster").objectReferenceValue        = lMaster;
        so.FindProperty("sliderMusic").objectReferenceValue        = sMusic;
        so.FindProperty("labelMusic").objectReferenceValue         = lMusic;
        so.FindProperty("sliderSFX").objectReferenceValue          = sSFX;
        so.FindProperty("labelSFX").objectReferenceValue           = lSFX;
        so.FindProperty("dropdownQuality").objectReferenceValue    = dQuality;
        so.FindProperty("dropdownResolution").objectReferenceValue = dRes;
        so.FindProperty("toggleFullscreen").objectReferenceValue   = tFullscreen;
        so.FindProperty("sliderSensitivity").objectReferenceValue  = sSensitivity;
        so.FindProperty("labelSensitivity").objectReferenceValue   = lSensitivity;
        so.FindProperty("toggleInvertY").objectReferenceValue      = tInvertY;
        so.FindProperty("btnApply").objectReferenceValue           = btnApply;
        so.FindProperty("btnReset").objectReferenceValue           = btnReset;
        so.FindProperty("btnClose").objectReferenceValue           = btnClose;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(settingsUI);
        Debug.Log("[SettingsSetupTool v3] ✅ Dark Premium UI tạo xong!");
        EditorUtility.DisplayDialog("✅ Thành Công! (v3 – Dark Premium)",
            "Settings UI đã được tạo!\n\n" +
            "• ESC → Mở/Đóng (animation EaseOutBack)\n" +
            "• Hover → scale + glow viridian\n" +
            "• Click → ripple + click sound\n" +
            "• Slider → tick âm theo pitch\n" +
            "• Lưu → confirm chord C-E-G\n\n" +
            "Giao diện: Dark Forest (#0D1117 / #3FB950)",
            "Xịn quá!");

        Selection.activeGameObject = canvasGO;
    }

    // ══════════════════════════════════════════════════════════
    //  CONTENT PANELS
    // ══════════════════════════════════════════════════════════
    void BuildAudioPanel(Transform p,
        out Slider sm, out TextMeshProUGUI lm,
        out Slider smu, out TextMeshProUGUI lmu,
        out Slider ss, out TextMeshProUGUI ls)
    {
        MakeSectionHeader(p, "Am Luong", 0);
        float y = -50;
        sm  = MakeSliderRow(p, "[V]", "Am luong tong",    ref y, 0, 1, 1f,   out lm);
        smu = MakeSliderRow(p, "[M]", "Am nhac nen",       ref y, 0, 1, 0.8f, out lmu);
        ss  = MakeSliderRow(p, "[S]", "Hieu ung am thanh", ref y, 0, 1, 1f,   out ls);
    }

    void BuildGraphicsPanel(Transform p,
        out TMP_Dropdown dq, out TMP_Dropdown dr, out Toggle tf)
    {
        MakeSectionHeader(p, "Hien Thi", 0);
        float y = -50;
        dq = MakeDropdownRow(p, "[Q]", "Chat luong do hoa", ref y);
        dr = MakeDropdownRow(p, "[R]", "Do phan giai",       ref y);
        tf = MakeToggleRow(p,   "[F]", "Toan man hinh",     ref y, true);
    }

    void BuildControlsPanel(Transform p,
        out Slider sen, out TextMeshProUGUI lsen, out Toggle ti)
    {
        MakeSectionHeader(p, "Chuot & Camera", 0);
        float y = -50;
        sen = MakeSliderRow(p, "[X]", "Do nhay chuot", ref y, 0.5f, 10f, 2f, out lsen);
        ti  = MakeToggleRow(p, "[Y]", "Dao truc Y",    ref y, false);
    }

    // ══════════════════════════════════════════════════════════
    //  ROWS
    // ══════════════════════════════════════════════════════════
    const float ROW_H  = 56f;
    const float ROW_GAP = 8f;

    Slider MakeSliderRow(Transform parent, string icon, string label, ref float y,
        float min, float max, float def, out TextMeshProUGUI valueLbl)
    {
        var row = MakeRect(parent, $"Row_{label}", new Vector2(0,1), new Vector2(1,1),
            new Vector2(0, y), new Vector2(0, ROW_H));
        y -= ROW_H + ROW_GAP;

        // BG hàng
        MakeRoundedBG(row.transform, new Color(0.13f, 0.16f, 0.20f, 0.6f));

        // Icon (ASCII safe)
        var ico = MakeText(row.transform, "Icon", icon, 16,
            ACCENT_GREEN, FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(ico, new Vector2(0,0), new Vector2(0,1),
            new Vector2(12, 0), new Vector2(36, 0));

        // Label
        var lbl = MakeText(row.transform, "Label", label, 17,
            TEXT_PRIMARY, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        SetRect(lbl, new Vector2(0,0), new Vector2(0.36f,1),
            new Vector2(52, 0), new Vector2(0, 0));

        // Value
        var valGO = MakeText(row.transform, "Value", FormatVal(def, min, max), 15,
            ACCENT_GREEN, FontStyles.Bold, TextAlignmentOptions.MidlineRight);
        SetRect(valGO, new Vector2(0.82f,0), new Vector2(1f,1),
            new Vector2(0, 0), new Vector2(-12, 0));
        valueLbl = valGO.GetComponent<TextMeshProUGUI>();

        // Slider
        var slider = BuildSlider(row.transform, new Vector2(0.36f, 0), new Vector2(0.82f, 1),
            new Vector2(0, 10), new Vector2(0, -10), min, max, def);

        return slider;
    }

    TMP_Dropdown MakeDropdownRow(Transform parent, string icon, string label, ref float y)
    {
        var row = MakeRect(parent, $"Row_{label}", new Vector2(0,1), new Vector2(1,1),
            new Vector2(0, y), new Vector2(0, ROW_H));
        y -= ROW_H + ROW_GAP;
        MakeRoundedBG(row.transform, new Color(0.13f, 0.16f, 0.20f, 0.6f));

        var ico = MakeText(row.transform, "Icon", icon, 16, ACCENT_BLUE,
            FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(ico, new Vector2(0,0), new Vector2(0,1), new Vector2(12,0), new Vector2(36,0));

        var lbl = MakeText(row.transform, "Label", label, 17, TEXT_PRIMARY,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        SetRect(lbl, new Vector2(0,0), new Vector2(0.36f,1), new Vector2(52,0), new Vector2(0,0));

        // Dropdown container
        var ddGO = new GameObject("Dropdown");
        ddGO.transform.SetParent(row.transform, false);
        var ddRect = ddGO.AddComponent<RectTransform>();
        ddRect.anchorMin = new Vector2(0.36f, 0.1f);
        ddRect.anchorMax = new Vector2(0.99f, 0.9f);
        ddRect.offsetMin = ddRect.offsetMax = Vector2.zero;

        var ddBg = ddGO.AddComponent<Image>();
        ddBg.color = C("#21262D");

        var dd = ddGO.AddComponent<TMP_Dropdown>();

        var captionGO = new GameObject("Label");
        captionGO.transform.SetParent(ddGO.transform, false);
        var cR = captionGO.AddComponent<RectTransform>();
        cR.anchorMin = Vector2.zero; cR.anchorMax = Vector2.one;
        cR.offsetMin = new Vector2(10,2); cR.offsetMax = new Vector2(-28,-2);
        var cT = captionGO.AddComponent<TextMeshProUGUI>();
        cT.fontSize = 15; cT.color = TEXT_PRIMARY;
        cT.alignment = TextAlignmentOptions.Left;

        dd.captionText = cT;
        dd.AddOptions(new System.Collections.Generic.List<string> { "— Chọn —" });

        // Arrow icon
        var arrowGO = new GameObject("Arrow");
        arrowGO.transform.SetParent(ddGO.transform, false);
        var aR = arrowGO.AddComponent<RectTransform>();
        aR.anchorMin = new Vector2(1,0.5f); aR.anchorMax = new Vector2(1,0.5f);
        aR.pivot = new Vector2(1,0.5f);
        aR.anchoredPosition = new Vector2(-8, 0);
        aR.sizeDelta = new Vector2(20, 20);
        var aT = arrowGO.AddComponent<TextMeshProUGUI>();
        aT.text = "▼"; aT.fontSize = 11; aT.color = TEXT_MUTED;
        aT.alignment = TextAlignmentOptions.Center;

        return dd;
    }

    Toggle MakeToggleRow(Transform parent, string icon, string label, ref float y, bool def)
    {
        var row = MakeRect(parent, $"Row_{label}", new Vector2(0,1), new Vector2(1,1),
            new Vector2(0, y), new Vector2(0, ROW_H));
        y -= ROW_H + ROW_GAP;
        MakeRoundedBG(row.transform, new Color(0.13f, 0.16f, 0.20f, 0.6f));

        var ico = MakeText(row.transform, "Icon", icon, 16, TEXT_MUTED,
            FontStyles.Bold, TextAlignmentOptions.Center);
        SetRect(ico, new Vector2(0,0), new Vector2(0,1), new Vector2(12,0), new Vector2(36,0));

        var lbl = MakeText(row.transform, "Label", label, 17, TEXT_PRIMARY,
            FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        SetRect(lbl, new Vector2(0,0), new Vector2(0.7f,1), new Vector2(52,0), new Vector2(0,0));

        // Pill toggle
        var pillGO = new GameObject("Toggle_Pill");
        pillGO.transform.SetParent(row.transform, false);
        var pilR = pillGO.AddComponent<RectTransform>();
        pilR.anchorMin = new Vector2(1, 0.5f); pilR.anchorMax = new Vector2(1, 0.5f);
        pilR.pivot     = new Vector2(1, 0.5f);
        pilR.anchoredPosition = new Vector2(-14, 0);
        pilR.sizeDelta = new Vector2(52, 28);

        var pillBG = pillGO.AddComponent<Image>();
        pillBG.color = def ? ACCENT_GREEN : C("#3D444D");

        // Knob (vòng tròn trắng)
        var knobGO = new GameObject("Knob");
        knobGO.transform.SetParent(pillGO.transform, false);
        var knobR = knobGO.AddComponent<RectTransform>();
        knobR.sizeDelta = new Vector2(22, 22);
        knobR.anchorMin = new Vector2(def ? 1f : 0f, 0.5f);
        knobR.anchorMax = new Vector2(def ? 1f : 0f, 0.5f);
        knobR.pivot     = new Vector2(def ? 1f : 0f, 0.5f);
        knobR.anchoredPosition = new Vector2(def ? -3f : 3f, 0);
        var knobImg = knobGO.AddComponent<Image>();
        knobImg.color = Color.white;

        var tgl = pillGO.AddComponent<Toggle>();
        tgl.targetGraphic = pillBG;
        tgl.graphic       = knobImg;
        tgl.isOn          = def;

        // Khi toggle đổi trạng thái → đổi màu pill
        tgl.onValueChanged.AddListener(on => pillBG.color = on ? ACCENT_GREEN : C("#3D444D"));

        return tgl;
    }

    // ══════════════════════════════════════════════════════════
    //  SLIDER BUILDER
    // ══════════════════════════════════════════════════════════
    Slider BuildSlider(Transform parent, Vector2 amin, Vector2 amax,
        Vector2 offMin, Vector2 offMax, float min, float max, float def)
    {
        var go = new GameObject("Slider");
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = amin; r.anchorMax = amax;
        r.offsetMin = offMin; r.offsetMax = offMax;

        var slider = go.AddComponent<Slider>();

        // Track BG
        var trackBG = new GameObject("Background");
        trackBG.transform.SetParent(go.transform, false);
        var tbR = trackBG.AddComponent<RectTransform>();
        tbR.anchorMin = new Vector2(0, 0.3f); tbR.anchorMax = new Vector2(1, 0.7f);
        tbR.offsetMin = tbR.offsetMax = Vector2.zero;
        var tbImg = trackBG.AddComponent<Image>();
        tbImg.color = SLIDER_BG;

        // Fill Area
        var fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        var faR = fillArea.AddComponent<RectTransform>();
        faR.anchorMin = new Vector2(0, 0.3f); faR.anchorMax = new Vector2(1, 0.7f);
        faR.offsetMin = faR.offsetMax = Vector2.zero;

        var fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(fillArea.transform, false);
        var fR = fillGO.AddComponent<RectTransform>();
        fR.anchorMin = Vector2.zero;
        fR.anchorMax = new Vector2(Mathf.InverseLerp(min, max, def), 1);
        fR.offsetMin = fR.offsetMax = Vector2.zero;
        var fImg = fillGO.AddComponent<Image>();
        fImg.color = ACCENT_GREEN;

        // Handle
        var handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(go.transform, false);
        var haR = handleArea.AddComponent<RectTransform>();
        haR.anchorMin = Vector2.zero; haR.anchorMax = Vector2.one;
        haR.offsetMin = haR.offsetMax = Vector2.zero;

        var handleGO = new GameObject("Handle");
        handleGO.transform.SetParent(handleArea.transform, false);
        var hR = handleGO.AddComponent<RectTransform>();
        hR.sizeDelta = new Vector2(18, 28); // taller handle
        hR.anchorMin = new Vector2(Mathf.InverseLerp(min, max, def), 0);
        hR.anchorMax = new Vector2(Mathf.InverseLerp(min, max, def), 1);
        var hImg = handleGO.AddComponent<Image>();
        hImg.color = TEXT_PRIMARY;

        // Outline trên handle
        var hOL = handleGO.AddComponent<Outline>();
        hOL.effectColor    = ACCENT_GREEN;
        hOL.effectDistance = new Vector2(1, -1);

        slider.fillRect      = fR;
        slider.handleRect    = hR;
        slider.targetGraphic = hImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue      = min;
        slider.maxValue      = max;
        slider.value         = def;

        return slider;
    }

    // ══════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════
    void MakeSectionHeader(Transform parent, string title, float yOffset)
    {
        var hdrGO = new GameObject($"SectionHeader_{title}");
        hdrGO.transform.SetParent(parent, false);
        var r = hdrGO.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0, 1); r.anchorMax = new Vector2(1, 1);
        r.pivot     = new Vector2(0.5f, 1);
        r.anchoredPosition = new Vector2(0, yOffset);
        r.sizeDelta = new Vector2(0, 34);

        // Divider line kèm text
        var divGO = new GameObject("Line");
        divGO.transform.SetParent(hdrGO.transform, false);
        var dR = divGO.AddComponent<RectTransform>();
        dR.anchorMin = new Vector2(0,0.5f); dR.anchorMax = new Vector2(0.25f, 0.5f);
        dR.sizeDelta = new Vector2(0, 1);
        dR.offsetMin = dR.offsetMax = Vector2.zero;
        MakeImage(divGO.transform, "Line", BORDER_COLOR).GetComponent<RectTransform>().Fill();

        var lblGO = MakeText(hdrGO.transform, "SectionTitle", title.ToUpper(),
            12, TEXT_MUTED, FontStyles.Bold, TextAlignmentOptions.Center);
        var lR = lblGO.GetComponent<RectTransform>();
        lR.anchorMin = new Vector2(0.25f, 0); lR.anchorMax = new Vector2(0.75f, 1);
        lR.offsetMin = lR.offsetMax = Vector2.zero;

        var divGO2 = new GameObject("Line2");
        divGO2.transform.SetParent(hdrGO.transform, false);
        var d2R = divGO2.AddComponent<RectTransform>();
        d2R.anchorMin = new Vector2(0.75f,0.5f); d2R.anchorMax = new Vector2(1f, 0.5f);
        d2R.sizeDelta = new Vector2(0, 1);
        d2R.offsetMin = d2R.offsetMax = Vector2.zero;
        MakeImage(divGO2.transform, "Line2", BORDER_COLOR).GetComponent<RectTransform>().Fill();
    }

    Button MakePillButton(Transform parent, string name, string label,
        Color bgColor, Color textColor, int fontSize, bool bold)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var bg  = go.AddComponent<Image>();
        bg.color = bgColor;
        var btn = go.AddComponent<Button>();

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var tR = txtGO.AddComponent<RectTransform>();
        tR.anchorMin = Vector2.zero; tR.anchorMax = Vector2.one;
        tR.offsetMin = tR.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = fontSize;
        tmp.color     = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        if (bold) tmp.fontStyle = FontStyles.Bold;

        return btn;
    }

    Button MakeTabButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = TAB_NORMAL;
        var btn = go.AddComponent<Button>();

        // Indicator bar dưới tab (active)
        var indicator = new GameObject("Indicator");
        indicator.transform.SetParent(go.transform, false);
        var iR = indicator.AddComponent<RectTransform>();
        iR.anchorMin = new Vector2(0.1f, 0); iR.anchorMax = new Vector2(0.9f, 0);
        iR.pivot     = new Vector2(0.5f, 0);
        iR.sizeDelta = new Vector2(0, 3);
        MakeImage(indicator.transform, "IndicatorBar", ACCENT_GREEN).GetComponent<RectTransform>().Fill();
        indicator.SetActive(false); // hidden by default

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(go.transform, false);
        var tR = txtGO.AddComponent<RectTransform>();
        tR.anchorMin = Vector2.zero; tR.anchorMax = Vector2.one;
        tR.offsetMin = tR.offsetMax = Vector2.zero;
        var tmp = txtGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 16;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = TEXT_MUTED;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    enum Align { Left, Right }
    void PositionInBar(Button btn, Align align, float margin, float yOff, float w, float h)
    {
        var r = btn.GetComponent<RectTransform>();
        float xAnchor = (align == Align.Right) ? 1f : 0f;
        r.anchorMin = new Vector2(xAnchor, 0.5f);
        r.anchorMax = new Vector2(xAnchor, 0.5f);
        r.pivot     = new Vector2(xAnchor, 0.5f);
        r.anchoredPosition = new Vector2(align == Align.Right ? -margin : margin, yOff);
        r.sizeDelta = new Vector2(w, h);
    }

    GameObject MakeTabPanel(Transform parent, string name, float padding)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(padding, padding);
        r.offsetMax = new Vector2(-padding, -padding);
        go.SetActive(false);
        return go;
    }

    void MakeRoundedBG(Transform parent, Color color)
    {
        MakeImage(parent, "RowBG", color).GetComponent<RectTransform>().Fill();
    }

    RectTransform MakeRect(Transform parent, string name,
        Vector2 aMin, Vector2 aMax, Vector2 aPos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var r = go.AddComponent<RectTransform>();
        r.anchorMin = aMin; r.anchorMax = aMax;
        r.pivot     = new Vector2(0.5f, 1);
        r.anchoredPosition = aPos;
        r.sizeDelta = size;
        return r;
    }

    GameObject MakeImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    GameObject MakeText(Transform parent, string name, string text,
        float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        return go;
    }

    void SetRect(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax)
    {
        var r = go.GetComponent<RectTransform>();
        r.anchorMin = aMin; r.anchorMax = aMax;
        r.offsetMin = offMin; r.offsetMax = offMax;
    }

    void AddSingleOutline(GameObject go, Color color, float size)
    {
        var ol = go.AddComponent<Outline>();
        ol.effectColor    = color;
        ol.effectDistance = new Vector2(size, -size);
    }

    string FormatVal(float v, float min, float max)
    {
        bool isPct = (min == 0 && max == 1);
        return isPct ? Mathf.RoundToInt(v * 100) + "%" : v.ToString("F1");
    }

    void Cleanup()
    {
        foreach (var x in FindObjectsByType<SettingsUI>     (FindObjectsSortMode.None)) DestroyImmediate(x.gameObject);
        foreach (var x in FindObjectsByType<SettingsManager>(FindObjectsSortMode.None)) DestroyImmediate(x.gameObject);
        foreach (var x in FindObjectsByType<UIAudioFeedback>(FindObjectsSortMode.None)) DestroyImmediate(x.gameObject);
        foreach (var n in new[] { "SettingsCanvas", "SettingsManager", "UIAudioFeedback" })
        { var g = GameObject.Find(n); if (g) DestroyImmediate(g); }
        Debug.Log("[SettingsSetupTool] Đã dọn dẹp.");
    }
}

// ── Extension: RectTransform Fill ──
static class RectTransformExt
{
    public static RectTransform Fill(this RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        return r;
    }
}
