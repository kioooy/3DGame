using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor Tool – Tao Settings UI theo phong cach Minimalist.
/// Hieu ung: Panel truot len tu duoi man hinh.
/// Menu: Tools > Settings System > Setup Settings UI
/// </summary>
public class SettingsSetupTool : EditorWindow
{
    [MenuItem("Tools/Settings System/Setup Settings UI")]
    static void Open()
    {
        var w = GetWindow<SettingsSetupTool>("Settings Setup");
        w.minSize = new Vector2(360, 240);
        w.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Settings System – Minimalist", EditorStyles.boldLabel);
        GUILayout.Space(6);

        EditorGUILayout.HelpBox(
            "Cau truc:\n" +
            "  Canvas\n" +
            "    Overlay (nen mo toan man hinh)\n" +
            "    SettingsPanel\n" +
            "      Title  |  VolumeSlider  |  QualityDropdown\n" +
            "      FullscreenToggle  |  CloseButton\n\n" +
            "Hieu ung: Panel truot len tu duoi len (slide-in).",
            MessageType.Info);

        GUILayout.Space(12);

        // Status
        bool hasManager = FindFirstObjectByType<SettingsManager>() != null;
        bool hasUI      = FindFirstObjectByType<SettingsUI>() != null;
        GUI.color = (hasManager && hasUI) ? Color.green : Color.yellow;
        GUILayout.Label(hasManager && hasUI
            ? "  System da co san."
            : "  Chua tao hoac chua day du.", EditorStyles.boldLabel);
        GUI.color = Color.white;

        GUILayout.Space(8);
        GUI.backgroundColor = new Color(0.2f, 0.6f, 1f);
        if (GUILayout.Button("Tao / Tao Lai Settings UI", GUILayout.Height(44)))
        {
            if (EditorUtility.DisplayDialog("Xac nhan", "Tao lai Settings UI?", "Tao!", "Huy"))
                Build();
        }
        GUI.backgroundColor = new Color(0.7f, 0.2f, 0.2f);
        if (GUILayout.Button("Xoa", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Xac nhan", "Xoa tat ca?", "Xoa", "Huy"))
                Cleanup();
        }
        GUI.backgroundColor = Color.white;
    }

    // ════════════════════════════════════════════
    //  PALETTE – Minimalist Dark
    // ════════════════════════════════════════════
    static Color Hex(string h) { ColorUtility.TryParseHtmlString(h, out var c); return c; }

    // Nen panel (rgb toi, alpha cao)
    static readonly Color PANEL_BG   = new Color(0.08f, 0.08f, 0.12f, 0.97f);
    // Overlay (nen mo toan man hinh)
    static readonly Color OVERLAY    = new Color(0f, 0f, 0f, 0.55f);
    // Text
    static readonly Color TEXT_WHITE = new Color(0.95f, 0.95f, 0.95f, 1f);
    static readonly Color TEXT_DIM   = new Color(0.60f, 0.60f, 0.65f, 1f);
    // Accent neon xanh duong
    static readonly Color ACCENT     = Hex("#38BDF8");  // sky-400
    // Control bg
    static readonly Color CTRL_BG    = new Color(0.14f, 0.14f, 0.20f, 1f);
    // Nut
    static readonly Color BTN_CLOSE  = Hex("#EF4444");
    static readonly Color BTN_ROW    = new Color(0.15f, 0.15f, 0.20f, 1f);
    // Toggle
    static readonly Color TOG_ON    = Hex("#38BDF8");
    static readonly Color TOG_OFF   = new Color(0.25f, 0.25f, 0.32f, 1f);

    // ════════════════════════════════════════════
    //  BUILD
    // ════════════════════════════════════════════
    void Build()
    {
        Cleanup();

        // ── SettingsManager ──
        Undo.RegisterCreatedObjectUndo(
            new GameObject("SettingsManager") { }.AddComponent<SettingsManager>().gameObject,
            "Create SettingsManager");
        Undo.RegisterCreatedObjectUndo(
            new GameObject("UIAudioFeedback") { }.AddComponent<UIAudioFeedback>().gameObject,
            "Create UIAudioFeedback");

        // ── Canvas ──────────────────────────────
        var canvasGO = new GameObject("SettingsCanvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        var scaler           = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode          = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution  = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight   = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create SettingsCanvas");

        // ── Overlay (nen mo toan man hinh) ──────
        var overlay    = MakeChild("Overlay", canvasGO.transform);
        Stretch(overlay);
        var overlayImg = overlay.AddComponent<Image>();
        overlayImg.color         = OVERLAY;
        overlayImg.raycastTarget = true;

        // ── SettingsPanel (680 x 540) ────────────
        // Dat vi tri chinh giua theo chieu ngang,
        // chinh giua theo chieu doc (slide vao tu duoi)
        var panel    = MakeChild("SettingsPanel", canvasGO.transform);
        var panelRT  = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(680, 560);
        panelRT.anchoredPosition = Vector2.zero;
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = PANEL_BG;

        // CanvasGroup de fade + slide
        var cg = panel.AddComponent<CanvasGroup>();
        cg.alpha          = 0;
        cg.interactable   = false;
        cg.blocksRaycasts = false;

        // Vien tren (accent line 3px)
        var topBar    = MakeChild("TopAccentBar", panel.transform);
        var topBarRT  = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0, 1);
        topBarRT.anchorMax = new Vector2(1, 1);
        topBarRT.offsetMin = new Vector2(0, -3);
        topBarRT.offsetMax = Vector2.zero;
        topBar.AddComponent<Image>().color = ACCENT;

        // ── TITLE ────────────────────────────────
        var title = MakeText(panel.transform, "Title", "SETTINGS",
            30, TEXT_WHITE, FontStyles.Bold, TextAlignmentOptions.Center);
        var titleRT = title.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(0, -88);
        titleRT.offsetMax = new Vector2(0, -6);

        // Duong ke duoi title
        var divTitle   = MakeChild("DivTitle", panel.transform);
        var divTitleRT = divTitle.GetComponent<RectTransform>();
        divTitleRT.anchorMin = new Vector2(0.05f, 1);
        divTitleRT.anchorMax = new Vector2(0.95f, 1);
        divTitleRT.offsetMin = new Vector2(0, -91);
        divTitleRT.offsetMax = new Vector2(0, -90);
        divTitle.AddComponent<Image>().color = new Color(1,1,1,0.08f);

        // ── CONTENT (cuon doc) ────────────────────
        // Tat ca controls dat trong ScrollRect de san sang mo rong
        float rowY = -108f; // bat dau tu duoi title
        const float ROW_H   = 68f;
        const float ROW_GAP = 10f;
        const float PAD     = 36f;

        // ── 1. VolumeSlider ─────────────────────
        Slider sliderMaster;
        TextMeshProUGUI lblMaster;
        MakeSliderRow(panel.transform, "VolumeSlider", "Am Luong",
            0f, 1f, 1f, ref rowY, ROW_H, PAD, out sliderMaster, out lblMaster);

        // ── 2. QualityDropdown ────────────────────
        TMP_Dropdown ddQuality;
        MakeDropdownRow(panel.transform, "QualityDropdown", "Chat Luong Do Hoa",
            new[]{ "Thap", "Trung Binh", "Cao", "Rat Cao" },
            ref rowY, ROW_H, PAD, out ddQuality);

        // ── 3. Resolution Dropdown ────────────────
        TMP_Dropdown ddRes;
        MakeDropdownRow(panel.transform, "ResolutionDropdown", "Do Phan Giai",
            new[]{ "---" }, ref rowY, ROW_H, PAD, out ddRes);

        // ── 4. Fullscreen Toggle ──────────────────
        Toggle tFullscreen;
        MakeToggleRow(panel.transform, "FullscreenToggle", "Toan Man Hinh",
            true, ref rowY, ROW_H, PAD, out tFullscreen);

        // ── 5. Mouse Sensitivity ─────────────────
        Slider sliderSens;
        TextMeshProUGUI lblSens;
        MakeSliderRow(panel.transform, "SensSlider", "Do Nhay Chuot",
            0.5f, 10f, 2f, ref rowY, ROW_H, PAD, out sliderSens, out lblSens);

        rowY -= 12f; // khoang cach truoc nut

        // ── CloseButton ───────────────────────────
        var btnClose = MakeButton(panel.transform, "CloseButton", "DONG  X",
            BTN_CLOSE, TEXT_WHITE, 17);
        var bcRT = btnClose.GetComponent<RectTransform>();
        bcRT.anchorMin = new Vector2(0.5f, 1);
        bcRT.anchorMax = new Vector2(0.5f, 1);
        bcRT.pivot     = new Vector2(0.5f, 1);
        bcRT.anchoredPosition = new Vector2(0, rowY);
        bcRT.sizeDelta        = new Vector2(240, 52);

        // ── ApplyButton ───────────────────────────
        rowY -= 62f;
        var btnApply = MakeButton(panel.transform, "ApplyButton", "LUU CAI DAT",
            ACCENT, new Color(0.05f, 0.05f, 0.1f), 17);
        var baRT = btnApply.GetComponent<RectTransform>();
        baRT.anchorMin = new Vector2(0.5f, 1);
        baRT.anchorMax = new Vector2(0.5f, 1);
        baRT.pivot     = new Vector2(0.5f, 1);
        baRT.anchoredPosition = new Vector2(0, rowY);
        baRT.sizeDelta        = new Vector2(240, 52);

        // ── Wire SettingsUI ───────────────────────
        var settingsUI  = canvasGO.AddComponent<SettingsUI>();
        var so = new SerializedObject(settingsUI);
        so.FindProperty("settingsPanel").objectReferenceValue      = panel;
        so.FindProperty("backdropImage").objectReferenceValue      = overlayImg;
        so.FindProperty("sliderMaster").objectReferenceValue       = sliderMaster;
        so.FindProperty("labelMaster").objectReferenceValue        = lblMaster;
        so.FindProperty("dropdownQuality").objectReferenceValue    = ddQuality;
        so.FindProperty("dropdownResolution").objectReferenceValue = ddRes;
        so.FindProperty("toggleFullscreen").objectReferenceValue   = tFullscreen;
        so.FindProperty("sliderSensitivity").objectReferenceValue  = sliderSens;
        so.FindProperty("labelSensitivity").objectReferenceValue   = lblSens;
        so.FindProperty("btnApply").objectReferenceValue           = btnApply.GetComponent<Button>();
        so.FindProperty("btnClose").objectReferenceValue           = btnClose.GetComponent<Button>();
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(settingsUI);
        Selection.activeGameObject = canvasGO;
        Debug.Log("[SettingsSetupTool] Setup xong!");
        EditorUtility.DisplayDialog("Hoan tat!",
            "Settings Panel da duoc tao!\n\n" +
            "Nhan ESC trong Game Mode de mo/dong.\n" +
            "Panel se truot len tu phia duoi man hinh.",
            "OK");
    }

    // ════════════════════════════════════════════
    //  ROW BUILDERS
    // ════════════════════════════════════════════

    void MakeSliderRow(Transform parent, string goName, string labelText,
        float min, float max, float def, ref float y, float rowH, float pad,
        out Slider slider, out TextMeshProUGUI valueLabel)
    {
        var row  = MakeRowBG(parent, goName, ref y, rowH);

        // Label
        var lbl  = MakeText(row.transform, "Lbl", labelText,
            17, TEXT_WHITE, FontStyles.Normal, TextAlignmentOptions.Left);
        var lR   = lbl.GetComponent<RectTransform>();
        lR.anchorMin = Vector2.zero; lR.anchorMax = new Vector2(0.38f, 1);
        lR.offsetMin = new Vector2(pad, 0); lR.offsetMax = Vector2.zero;

        // Value
        var valGO= MakeText(row.transform, "Val", FormatVal(def, max),
            16, ACCENT, FontStyles.Bold, TextAlignmentOptions.Right);
        var vR   = valGO.GetComponent<RectTransform>();
        vR.anchorMin = new Vector2(0.78f, 0); vR.anchorMax = Vector2.one;
        vR.offsetMin = Vector2.zero; vR.offsetMax = new Vector2(-pad, 0);
        valueLabel = valGO.GetComponent<TextMeshProUGUI>();

        // Slider GO
        var sGO  = MakeChild("Slider", row.transform);
        var sRT  = sGO.GetComponent<RectTransform>();
        sRT.anchorMin = new Vector2(0.38f, 0.25f);
        sRT.anchorMax = new Vector2(0.78f, 0.75f);
        sRT.offsetMin = sRT.offsetMax = Vector2.zero;
        slider = sGO.AddComponent<Slider>();
        slider.minValue = min; slider.maxValue = max; slider.value = def;
        slider.wholeNumbers = false;

        // BG track
        var bgGO = MakeChild("BG", sGO.transform);
        Stretch(bgGO); var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = CTRL_BG;
        slider.targetGraphic = bgImg;

        // Fill area
        var faGO = MakeChild("FillArea", sGO.transform); Stretch(faGO);
        var fillGO = MakeChild("Fill", faGO.transform);
        Stretch(fillGO);
        var fillImg = fillGO.AddComponent<Image>(); fillImg.color = ACCENT;
        slider.fillRect = fillGO.GetComponent<RectTransform>();

        // Handle area
        var haGO = MakeChild("HandleArea", sGO.transform); Stretch(haGO);
        var hGO  = MakeChild("Handle", haGO.transform);
        var hRT  = hGO.GetComponent<RectTransform>(); hRT.sizeDelta = new Vector2(20,20);
        var hImg = hGO.AddComponent<Image>(); hImg.color = Color.white;
        slider.handleRect    = hRT;
        slider.direction     = Slider.Direction.LeftToRight;
    }

    void MakeDropdownRow(Transform parent, string goName, string labelText,
        string[] options, ref float y, float rowH, float pad, out TMP_Dropdown dd)
    {
        var row = MakeRowBG(parent, goName, ref y, rowH);

        var lbl = MakeText(row.transform, "Lbl", labelText,
            17, TEXT_WHITE, FontStyles.Normal, TextAlignmentOptions.Left);
        lbl.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        lbl.GetComponent<RectTransform>().anchorMax = new Vector2(0.38f, 1);
        lbl.GetComponent<RectTransform>().offsetMin = new Vector2(pad, 0);

        var ddGO  = MakeChild("Dropdown", row.transform);
        var ddRT  = ddGO.GetComponent<RectTransform>();
        ddRT.anchorMin = new Vector2(0.38f, 0.15f);
        ddRT.anchorMax = new Vector2(1f,    0.85f);
        ddRT.offsetMin = Vector2.zero;
        ddRT.offsetMax = new Vector2(-pad, 0);
        var bg = ddGO.AddComponent<Image>(); bg.color = CTRL_BG;

        dd = ddGO.AddComponent<TMP_Dropdown>();

        var capGO  = MakeChild("Label", ddGO.transform);
        var capRT  = capGO.GetComponent<RectTransform>();
        capRT.anchorMin = Vector2.zero; capRT.anchorMax = Vector2.one;
        capRT.offsetMin = new Vector2(12,4); capRT.offsetMax = new Vector2(-30,-4);
        var capT   = capGO.AddComponent<TextMeshProUGUI>();
        capT.fontSize = 15; capT.color = TEXT_WHITE;
        capT.alignment = TextAlignmentOptions.Left;
        dd.captionText = capT;

        var arrGO  = MakeChild("Arrow", ddGO.transform);
        var arrRT  = arrGO.GetComponent<RectTransform>();
        arrRT.anchorMin = new Vector2(1,0.5f); arrRT.anchorMax = new Vector2(1,0.5f);
        arrRT.pivot = new Vector2(1,0.5f);
        arrRT.anchoredPosition = new Vector2(-10,0); arrRT.sizeDelta = new Vector2(20,20);
        var arrT   = arrGO.AddComponent<TextMeshProUGUI>();
        arrT.text = "v"; arrT.fontSize = 12; arrT.color = TEXT_DIM;
        arrT.alignment = TextAlignmentOptions.Center;

        dd.AddOptions(new System.Collections.Generic.List<string>(options));
    }

    void MakeToggleRow(Transform parent, string goName, string labelText,
        bool def, ref float y, float rowH, float pad, out Toggle toggle)
    {
        var row = MakeRowBG(parent, goName, ref y, rowH);

        var lbl = MakeText(row.transform, "Lbl", labelText,
            17, TEXT_WHITE, FontStyles.Normal, TextAlignmentOptions.Left);
        lbl.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        lbl.GetComponent<RectTransform>().anchorMax = new Vector2(0.72f, 1);
        lbl.GetComponent<RectTransform>().offsetMin = new Vector2(pad, 0);

        // Pill toggle
        var pillGO = MakeChild("Pill", row.transform);
        var pRT    = pillGO.GetComponent<RectTransform>();
        pRT.anchorMin = new Vector2(1,0.5f); pRT.anchorMax = new Vector2(1,0.5f);
        pRT.pivot     = new Vector2(1,0.5f);
        pRT.anchoredPosition = new Vector2(-pad, 0);
        pRT.sizeDelta        = new Vector2(56, 28);
        var pillImg  = pillGO.AddComponent<Image>();
        pillImg.color = def ? TOG_ON : TOG_OFF;

        var knobGO = MakeChild("Knob", pillGO.transform);
        var kRT    = knobGO.GetComponent<RectTransform>();
        kRT.sizeDelta = new Vector2(22,22);
        kRT.anchorMin = kRT.anchorMax = new Vector2(def?1f:0f, 0.5f);
        kRT.pivot     = new Vector2(def?1f:0f, 0.5f);
        kRT.anchoredPosition = new Vector2(def?-3f:3f, 0);
        var kImg  = knobGO.AddComponent<Image>(); kImg.color = Color.white;

        toggle = pillGO.AddComponent<Toggle>();
        toggle.targetGraphic = pillImg;
        toggle.graphic       = kImg;
        toggle.isOn          = def;
    }

    // ════════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════════

    // Row background
    RectTransform MakeRowBG(Transform parent, string name, ref float y, float h)
    {
        var go  = MakeChild("Row_" + name, parent);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.pivot     = new Vector2(0.5f, 1);
        rt.anchoredPosition = new Vector2(0, y);
        rt.sizeDelta        = new Vector2(-48, h);   // margin 24px moi ben
        y -= h + 10f;
        go.AddComponent<Image>().color = BTN_ROW;
        return rt;
    }

    // Button
    static GameObject MakeButton(Transform parent, string name, string text, Color bg, Color fg, int fs)
    {
        var go   = MakeChild(name, parent);
        go.AddComponent<Image>().color = bg;
        go.AddComponent<Button>();

        var tGO  = MakeChild("Text", go.transform);
        var tRT  = tGO.GetComponent<RectTransform>();
        tRT.anchorMin = Vector2.zero; tRT.anchorMax = Vector2.one;
        tRT.offsetMin = new Vector2(8,0); tRT.offsetMax = new Vector2(-8,0);
        var tmp  = tGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fs;
        tmp.color = fg; tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    // Text
    static GameObject MakeText(Transform parent, string name, string text,
        int size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go  = MakeChild(name, parent);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = size;
        tmp.color = color; tmp.fontStyle = style; tmp.alignment = align;
        tmp.raycastTarget = false;
        tmp.overflowMode  = TextOverflowModes.Ellipsis;
        return go;
    }

    static GameObject MakeChild(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static string FormatVal(float v, float max)
        => max <= 1f ? Mathf.RoundToInt(v * 100) + "%" : v.ToString("F1");

    // ════════════════════════════════════════════
    //  CLEANUP
    // ════════════════════════════════════════════
    void Cleanup()
    {
        foreach (var n in new[]{"SettingsCanvas","SettingsManager","UIAudioFeedback"})
        {
            var obj = GameObject.Find(n);
            if (obj) Undo.DestroyObjectImmediate(obj);
        }
        Debug.Log("[SettingsSetupTool] Da don dep.");
    }
}
