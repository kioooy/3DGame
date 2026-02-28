using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

/// <summary>
/// Editor Tool: Tao UI Settings Menu.
/// Menu: Tools > Settings System > Setup Settings UI
/// </summary>
public class SettingsSetupTool : EditorWindow
{
    [MenuItem("Tools/Settings System/Setup Settings UI")]
    static void Open()
    {
        var w = GetWindow<SettingsSetupTool>("Settings Setup");
        w.minSize = new Vector2(440, 380);
        w.Show();
    }

    Vector2 _scroll;
    void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        GUILayout.Label("Settings System – Dark Premium", EditorStyles.boldLabel);
        GUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "Tab 1 – AM LUONG  : Master / Nhac nen / Hieu ung\n" +
            "Tab 2 – CAU HINH  : Chat luong / Do phan giai / Toan man hinh\n" +
            "Tab 3 – CHUOT     : Do nhay / Dao Y / Hieu ung con tro",
            MessageType.Info);

        GUILayout.Space(10);
        var mgr = FindFirstObjectByType<SettingsManager>();
        var ui  = FindFirstObjectByType<SettingsUI>();
        EditorGUILayout.LabelField("Trang thai:", EditorStyles.boldLabel);
        GUI.color = mgr ? Color.green : Color.yellow;
        EditorGUILayout.LabelField("  SettingsManager : " + (mgr ? "OK" : "Chua co"));
        GUI.color = ui ? Color.green : Color.yellow;
        EditorGUILayout.LabelField("  SettingsUI      : " + (ui  ? "OK" : "Chua co"));
        GUI.color = Color.white;

        GUILayout.Space(14);
        GUI.backgroundColor = new Color(0.2f, 0.65f, 0.35f);
        bool build = GUILayout.Button("Tao / Tao Lai Settings System", GUILayout.Height(46));
        GUI.backgroundColor = Color.white;
        GUILayout.Space(4);
        GUI.backgroundColor = new Color(0.6f, 0.2f, 0.2f);
        bool del = GUILayout.Button("Xoa Settings System", GUILayout.Height(30));
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndScrollView();

        // Thao tac SAU EndScrollView de tranh GUILayout loi
        if (build && EditorUtility.DisplayDialog("Xac nhan", "Tao lai Settings System?", "Tao!", "Huy"))
            Build();
        if (del && EditorUtility.DisplayDialog("Xac nhan", "Xoa tat ca?", "Xoa", "Huy"))
            Cleanup();
    }

    // ═══════════════════════════════ PALETTE ═══════════════════════════════
    static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out var c); return c; }

    static readonly Color BG_DEEP   = H("#0D1117");
    static readonly Color BG_PANEL  = H("#161B22");
    static readonly Color BG_INNER  = H("#0D1117");
    static readonly Color BG_ROW    = new Color(0.10f, 0.13f, 0.16f, 0.90f);
    static readonly Color BORDER    = H("#30363D");
    static readonly Color GRN       = H("#3FB950");
    static readonly Color BLU       = H("#58A6FF");
    static readonly Color ORG       = H("#E3A953");
    static readonly Color TEXT_PRI  = H("#E6EDF3");
    static readonly Color TEXT_MUT  = H("#8B949E");
    static readonly Color SLI_BG    = H("#21262D");
    static readonly Color DD_BG     = H("#21262D");
    static readonly Color BTN_GRN   = H("#238636");
    static readonly Color BTN_RST   = H("#6E7681");
    static readonly Color BTN_X     = H("#8B1A1A");
    static readonly Color OFF_PILL  = H("#3D444D");

    // ═══════════════════════════════ BUILD ═════════════════════════════════
    void Build()
    {
        Cleanup();

        // ── SettingsManager ──
        var mGO = new GameObject("SettingsManager");
        mGO.AddComponent<SettingsManager>();
        Undo.RegisterCreatedObjectUndo(mGO, "SettingsManager");

        // ── UIAudioFeedback ──
        var aGO = new GameObject("UIAudioFeedback");
        aGO.AddComponent<UIAudioFeedback>();
        Undo.RegisterCreatedObjectUndo(aGO, "UIAudioFeedback");

        // ── Canvas ──
        var canvasGO = new GameObject("SettingsCanvas");
        var cv = canvasGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 200;
        var cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGO, "SettingsCanvas");

        // ── Backdrop ──
        var backdropGO  = UI("Backdrop", canvasGO.transform);
        var backdropImg = backdropGO.GetComponent<Image>();
        backdropImg.color = new Color(0,0,0,0);
        Stretch(backdropGO);

        // ══════════════════════════════════════════════════════════════════
        //  PANEL  960 × 620  (giua man hinh)
        // ══════════════════════════════════════════════════════════════════
        var panelGO = UI("SettingsPanel", canvasGO.transform);
        var panelRT = RT(panelGO);
        panelRT.anchorMin = panelRT.anchorMax = panelRT.pivot = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(960, 620);
        panelGO.GetComponent<Image>().color = BG_PANEL;

        var cg = panelGO.AddComponent<CanvasGroup>();
        cg.alpha = 0; cg.interactable = false; cg.blocksRaycasts = false;

        var panelOL = panelGO.AddComponent<Outline>();
        panelOL.effectColor    = GRN;
        panelOL.effectDistance = new Vector2(1.5f, -1.5f);

        // ════════ HEADER  (y: 620-68=552 → 620, height=68) ════════
        //  anchorMin=(0,1) anchorMax=(1,1) → top-anchored strip
        //  offsetMin.y = -68 (bottom 68px bên dưới top)
        //  offsetMax.y =   0 (top tại chính top)
        var hdrGO = UI("Header", panelGO.transform);
        Stretch(hdrGO);
        var hdrRT = RT(hdrGO);
        hdrRT.anchorMin = new Vector2(0, 1);
        hdrRT.anchorMax = new Vector2(1, 1);
        hdrRT.offsetMin = new Vector2(0, -68);
        hdrRT.offsetMax = new Vector2(0,   0);
        hdrGO.GetComponent<Image>().color = BG_DEEP;

        // Accent line (3px top)
        var acLn = UI("AccentLine", hdrGO.transform);
        RT(acLn).anchorMin = new Vector2(0,1); RT(acLn).anchorMax = new Vector2(1,1);
        RT(acLn).offsetMin = new Vector2(0,-3); RT(acLn).offsetMax = Vector2.zero;
        acLn.GetComponent<Image>().color = GRN;

        // Tieu de
        var titleTMP = MakeTMP(hdrGO.transform, "Title", "CAI DAT", 26, TEXT_PRI, FontStyles.Bold, TextAlignmentOptions.MidlineLeft);
        StretchCustom(titleTMP.gameObject, new Vector2(0,0), new Vector2(0.6f,1), new Vector2(20,0), Vector2.zero);

        // Hint ESC
        var hintTMP = MakeTMP(hdrGO.transform, "Hint", "ESC de dong", 12, TEXT_MUT, FontStyles.Normal, TextAlignmentOptions.MidlineRight);
        StretchCustom(hintTMP.gameObject, new Vector2(0.6f,0), Vector2.one, Vector2.zero, new Vector2(-52,0));

        // Nut X
        var btnClose = Btn(hdrGO.transform, "BtnClose", "X", BTN_X, TEXT_PRI, 16, FontStyles.Bold);
        var bxRT = RT(btnClose.gameObject);
        bxRT.anchorMin = bxRT.anchorMax = bxRT.pivot = new Vector2(1, 0.5f);
        bxRT.anchoredPosition = new Vector2(-10, 0);
        bxRT.sizeDelta = new Vector2(36, 36);

        // ════════ TAB BAR  (height=40, ngay duoi header) ════════
        //  offsetMin.y=-108 (bottom=108px duoi top) offsetMax.y=-68 (top=68px duoi top)
        var tabBarGO = UI("TabBar", panelGO.transform);
        var tabBarRT = RT(tabBarGO);
        tabBarRT.anchorMin = new Vector2(0, 1);
        tabBarRT.anchorMax = new Vector2(1, 1);
        tabBarRT.offsetMin = new Vector2(0, -108);
        tabBarRT.offsetMax = new Vector2(0,  -68);
        tabBarGO.GetComponent<Image>().color = BG_DEEP;

        // Divider duoi tab bar
        var tbDiv = UI("Divider", tabBarGO.transform);
        RT(tbDiv).anchorMin = new Vector2(0,0); RT(tbDiv).anchorMax = new Vector2(1,0);
        RT(tbDiv).offsetMin = Vector2.zero; RT(tbDiv).offsetMax = new Vector2(0,1);
        tbDiv.GetComponent<Image>().color = BORDER;

        var tabAudio    = TabBtn(tabBarGO.transform, "Tab_Audio",    "AM LUONG", GRN);
        var tabGraphics = TabBtn(tabBarGO.transform, "Tab_Graphics", "CAU HINH", BLU);
        var tabControls = TabBtn(tabBarGO.transform, "Tab_Controls", "CHUOT",    ORG);

        var hLG = tabBarGO.AddComponent<HorizontalLayoutGroup>();
        hLG.spacing = 1; hLG.childForceExpandWidth = hLG.childForceExpandHeight = true;

        // ════════ CONTENT AREA ════════
        //  Full stretch, top cach 108px (header+tab), bottom cach 56px (bottombar)
        var contentGO = UI("ContentArea", panelGO.transform);
        Stretch(contentGO);
        var contentRT = RT(contentGO);
        contentRT.anchorMin = Vector2.zero;
        contentRT.anchorMax = Vector2.one;
        contentRT.offsetMin = new Vector2(0,  56);
        contentRT.offsetMax = new Vector2(0,-108);
        contentGO.GetComponent<Image>().color = BG_INNER;

        // Them Mask de clip content
        contentGO.AddComponent<RectMask2D>();

        const float PAD = 20f;
        var pAudio    = TabPanel(contentGO.transform, "PanelAudio",    PAD);
        var pGraphics = TabPanel(contentGO.transform, "PanelGraphics", PAD);
        var pControls = TabPanel(contentGO.transform, "PanelControls", PAD);

        // ── Build content ──
        Slider sMaster, sMusic, sSFX;
        TextMeshProUGUI lMaster, lMusic, lSFX;
        AudioPanel(pAudio.transform, out sMaster, out lMaster, out sMusic, out lMusic, out sSFX, out lSFX);

        TMP_Dropdown dQuality, dRes;
        Toggle tFS;
        GraphicsPanel(pGraphics.transform, out dQuality, out dRes, out tFS);

        Slider sSens; TextMeshProUGUI lSens;
        Toggle tInvY, tTrail, tSmooth, tHL;
        Slider sCursorSz; TextMeshProUGUI lCursorSz;
        TMP_Dropdown dCursorStyle;
        ControlsPanel(pControls.transform,
            out sSens, out lSens, out tInvY,
            out tTrail, out sCursorSz, out lCursorSz,
            out tSmooth, out tHL, out dCursorStyle);

        // ════════ BOTTOM BAR  (height=56, cuoi panel) ════════
        var botGO = UI("BottomBar", panelGO.transform);
        var botRT = RT(botGO);
        botRT.anchorMin = new Vector2(0, 0);
        botRT.anchorMax = new Vector2(1, 0);
        botRT.offsetMin = Vector2.zero;
        botRT.offsetMax = new Vector2(0, 56);
        botGO.GetComponent<Image>().color = BG_DEEP;

        var botDiv = UI("Divider", botGO.transform);
        RT(botDiv).anchorMin = new Vector2(0,1); RT(botDiv).anchorMax = new Vector2(1,1);
        RT(botDiv).offsetMin = new Vector2(0,-1); RT(botDiv).offsetMax = Vector2.zero;
        botDiv.GetComponent<Image>().color = BORDER;

        var btnReset = Btn(botGO.transform, "BtnReset", "Mac Dinh", BTN_RST, TEXT_PRI, 15, FontStyles.Bold);
        var brRT = RT(btnReset.gameObject);
        brRT.anchorMin = brRT.anchorMax = brRT.pivot = new Vector2(0, 0.5f);
        brRT.anchoredPosition = new Vector2(16, 0); brRT.sizeDelta = new Vector2(150, 36);

        var btnApply = Btn(botGO.transform, "BtnApply", "Luu va Ap Dung", BTN_GRN, TEXT_PRI, 15, FontStyles.Bold);
        var baRT = RT(btnApply.gameObject);
        baRT.anchorMin = baRT.anchorMax = baRT.pivot = new Vector2(1, 0.5f);
        baRT.anchoredPosition = new Vector2(-16, 0); baRT.sizeDelta = new Vector2(190, 36);

        // ════════ WIRE SettingsUI ════════
        var sui = canvasGO.AddComponent<SettingsUI>();
        var so  = new SerializedObject(sui);
        so.FindProperty("settingsPanel")        .objectReferenceValue = panelGO;
        so.FindProperty("backdropImage")        .objectReferenceValue = backdropImg;
        so.FindProperty("tabAudioBtn")          .objectReferenceValue = tabAudio;
        so.FindProperty("tabGraphicsBtn")       .objectReferenceValue = tabGraphics;
        so.FindProperty("tabControlsBtn")       .objectReferenceValue = tabControls;
        so.FindProperty("panelAudio")           .objectReferenceValue = pAudio;
        so.FindProperty("panelGraphics")        .objectReferenceValue = pGraphics;
        so.FindProperty("panelControls")        .objectReferenceValue = pControls;
        so.FindProperty("sliderMaster")         .objectReferenceValue = sMaster;
        so.FindProperty("labelMaster")          .objectReferenceValue = lMaster;
        so.FindProperty("sliderMusic")          .objectReferenceValue = sMusic;
        so.FindProperty("labelMusic")           .objectReferenceValue = lMusic;
        so.FindProperty("sliderSFX")            .objectReferenceValue = sSFX;
        so.FindProperty("labelSFX")             .objectReferenceValue = lSFX;
        so.FindProperty("dropdownQuality")      .objectReferenceValue = dQuality;
        so.FindProperty("dropdownResolution")   .objectReferenceValue = dRes;
        so.FindProperty("toggleFullscreen")     .objectReferenceValue = tFS;
        so.FindProperty("sliderSensitivity")    .objectReferenceValue = sSens;
        so.FindProperty("labelSensitivity")     .objectReferenceValue = lSens;
        so.FindProperty("toggleInvertY")        .objectReferenceValue = tInvY;
        so.FindProperty("toggleCursorTrail")    .objectReferenceValue = tTrail;
        so.FindProperty("sliderCursorSize")     .objectReferenceValue = sCursorSz;
        so.FindProperty("labelCursorSize")      .objectReferenceValue = lCursorSz;
        so.FindProperty("toggleSmoothMouse")    .objectReferenceValue = tSmooth;
        so.FindProperty("toggleCursorHighlight").objectReferenceValue = tHL;
        so.FindProperty("dropdownCursorStyle")  .objectReferenceValue = dCursorStyle;
        so.FindProperty("btnApply")             .objectReferenceValue = btnApply;
        so.FindProperty("btnReset")             .objectReferenceValue = btnReset;
        so.FindProperty("btnClose")             .objectReferenceValue = btnClose;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(sui);
        Debug.Log("[SettingsSetupTool] Hoan tat!");
        EditorUtility.DisplayDialog("OK!", "Settings UI da tao xong.\nNhan ESC de mo/dong.", "OK");
        Selection.activeGameObject = canvasGO;
    }

    // ═══════════════════════════════ CONTENT ═══════════════════════════════
    void AudioPanel(Transform p,
        out Slider sm, out TextMeshProUGUI lm,
        out Slider su, out TextMeshProUGUI lu,
        out Slider ss, out TextMeshProUGUI ls)
    {
        float y = -8f;
        SecHeader(p, "AM LUONG", GRN, ref y);
        sm = SliderRow(p, "V", "Am luong tong",      GRN, ref y, 0,1,1f, out lm);
        su = SliderRow(p, "M", "Nhac nen",           GRN, ref y, 0,1,0.8f, out lu);
        ss = SliderRow(p, "S", "Hieu ung am thanh",  GRN, ref y, 0,1,1f, out ls);
    }

    void GraphicsPanel(Transform p,
        out TMP_Dropdown dq, out TMP_Dropdown dr, out Toggle tf)
    {
        float y = -8f;
        SecHeader(p, "CAU HINH DO HOA", BLU, ref y);
        dq = DropdownRow(p, "Q", "Chat luong do hoa", BLU, ref y);
        dr = DropdownRow(p, "R", "Do phan giai",      BLU, ref y);
        tf = ToggleRow(p,  "F", "Toan man hinh",      BLU, ref y, true);
    }

    void ControlsPanel(Transform p,
        out Slider ss, out TextMeshProUGUI ls, out Toggle tY,
        out Toggle tTr, out Slider sSz, out TextMeshProUGUI lSz,
        out Toggle tSm, out Toggle tHL, out TMP_Dropdown dSt)
    {
        float y = -8f;
        SecHeader(p, "TOC DO CON TROT", ORG, ref y);
        ss  = SliderRow(p, "X", "Do nhay chuot", ORG, ref y, 0.5f,10f,2f, out ls);
        tY  = ToggleRow(p,  "Y", "Dao truc Y",  ORG, ref y, false);

        SecHeader(p, "HIEU UNG CON TROT", ORG, ref y);
        tTr  = ToggleRow(p,  "T", "Hieu ung duoi chuot",  ORG, ref y, false);
        sSz  = SliderRow(p,  "Z", "Kich co con trot",     ORG, ref y, 0.5f,2f,1f, out lSz);
        tSm  = ToggleRow(p,  "M", "Lam muot chuyen dong", ORG, ref y, false);
        tHL  = ToggleRow(p,  "H", "Vong sang con trot",   ORG, ref y, false);
        dSt  = DropdownRow(p,"C", "Kieu con trot",        ORG, ref y);
        dSt.AddOptions(new System.Collections.Generic.List<string>
            { "Mac dinh", "Phan mem", "An con trot" });
    }

    // ═══════════════════════════════ ROWS ══════════════════════════════════
    const float ROW_H = 52f, ROW_G = 6f;

    void SecHeader(Transform p, string title, Color accent, ref float y)
    {
        var go = new GameObject("Sec_" + title); go.transform.SetParent(p, false);
        var r  = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0,1); r.anchorMax = new Vector2(1,1);
        r.pivot     = new Vector2(0.5f,1);
        r.anchoredPosition = new Vector2(0, y);
        r.sizeDelta = new Vector2(0, 28); y -= 32f;

        // Duong ke trai
        var ll = new GameObject("LL"); ll.transform.SetParent(go.transform,false);
        var llR = ll.AddComponent<RectTransform>();
        llR.anchorMin = new Vector2(0,0.5f); llR.anchorMax = new Vector2(0.15f,0.5f);
        llR.sizeDelta = new Vector2(0,1); llR.offsetMin = llR.offsetMax = Vector2.zero;
        ll.AddComponent<Image>().color = accent;

        var lbl = MakeTMP(go.transform, "Lbl", title, 11, accent, FontStyles.Bold, TextAlignmentOptions.Center);
        StretchCustom(lbl.gameObject, new Vector2(0.15f,0), new Vector2(0.85f,1), Vector2.zero, Vector2.zero);

        // Duong ke phai
        var lr = new GameObject("LR"); lr.transform.SetParent(go.transform,false);
        var lrR = lr.AddComponent<RectTransform>();
        lrR.anchorMin = new Vector2(0.85f,0.5f); lrR.anchorMax = new Vector2(1f,0.5f);
        lrR.sizeDelta = new Vector2(0,1); lrR.offsetMin = lrR.offsetMax = Vector2.zero;
        lr.AddComponent<Image>().color = accent;
    }

    Slider SliderRow(Transform p, string ico, string label, Color accent,
        ref float y, float min, float max, float def, out TextMeshProUGUI valLbl)
    {
        var row = Row(p, "SlRow_"+label, ref y);

        var icT = MakeTMP(row,"Icon","["+ico+"]",12,accent,FontStyles.Bold,TextAlignmentOptions.Center);
        StretchCustom(icT.gameObject,new Vector2(0,0),new Vector2(0,1),new Vector2(10,0),new Vector2(46,0));

        var lbT = MakeTMP(row,"Label",label,15,TEXT_PRI,FontStyles.Normal,TextAlignmentOptions.MidlineLeft);
        StretchCustom(lbT.gameObject,new Vector2(0,0),new Vector2(0.38f,1),new Vector2(50,0),Vector2.zero);

        var vlT = MakeTMP(row,"Val",FmtV(def,min,max),14,accent,FontStyles.Bold,TextAlignmentOptions.MidlineRight);
        StretchCustom(vlT.gameObject,new Vector2(0.84f,0),new Vector2(1,1),Vector2.zero,new Vector2(-10,0));
        valLbl = vlT;

        var sl = MakeSlider(row, new Vector2(0.38f,0.2f), new Vector2(0.84f,0.8f), accent, min,max,def);
        return sl;
    }

    TMP_Dropdown DropdownRow(Transform p, string ico, string label, Color accent, ref float y)
    {
        var row = Row(p,"DDRow_"+label, ref y);

        var icT = MakeTMP(row,"Icon","["+ico+"]",12,accent,FontStyles.Bold,TextAlignmentOptions.Center);
        StretchCustom(icT.gameObject,new Vector2(0,0),new Vector2(0,1),new Vector2(10,0),new Vector2(46,0));

        var lbT = MakeTMP(row,"Label",label,15,TEXT_PRI,FontStyles.Normal,TextAlignmentOptions.MidlineLeft);
        StretchCustom(lbT.gameObject,new Vector2(0,0),new Vector2(0.40f,1),new Vector2(50,0),Vector2.zero);

        // Dropdown container
        var ddGO = new GameObject("Dropdown"); ddGO.transform.SetParent(row,false);
        var ddRT = ddGO.AddComponent<RectTransform>();
        ddRT.anchorMin = new Vector2(0.40f,0.12f);
        ddRT.anchorMax = new Vector2(0.99f,0.88f);
        ddRT.offsetMin = ddRT.offsetMax = Vector2.zero;
        ddGO.AddComponent<Image>().color = DD_BG;
        var dd = ddGO.AddComponent<TMP_Dropdown>();

        // Caption
        var capGO = new GameObject("Label"); capGO.transform.SetParent(ddGO.transform,false);
        var capRT = capGO.AddComponent<RectTransform>();
        capRT.anchorMin = Vector2.zero; capRT.anchorMax = Vector2.one;
        capRT.offsetMin = new Vector2(8,0); capRT.offsetMax = new Vector2(-28,0);
        var capT = capGO.AddComponent<TextMeshProUGUI>();
        capT.fontSize = 13; capT.color = TEXT_PRI;
        capT.alignment = TextAlignmentOptions.MidlineLeft;
        dd.captionText = capT;

        // Arrow
        var arrGO = new GameObject("Arrow"); arrGO.transform.SetParent(ddGO.transform,false);
        var arrRT = arrGO.AddComponent<RectTransform>();
        arrRT.anchorMin = new Vector2(1,0.5f); arrRT.anchorMax = new Vector2(1,0.5f);
        arrRT.pivot = new Vector2(1,0.5f);
        arrRT.anchoredPosition = new Vector2(-6,0); arrRT.sizeDelta = new Vector2(16,16);
        var arrT = arrGO.AddComponent<TextMeshProUGUI>();
        arrT.text = "v"; arrT.fontSize = 10; arrT.color = TEXT_MUT;
        arrT.alignment = TextAlignmentOptions.Center;

        // Template (PHAI co, PHAI SetActive(false) truoc)
        var tplGO = new GameObject("Template"); tplGO.transform.SetParent(ddGO.transform,false);
        var tplRT = tplGO.AddComponent<RectTransform>();
        tplRT.anchorMin = new Vector2(0,0); tplRT.anchorMax = new Vector2(1,0);
        tplRT.pivot     = new Vector2(0.5f,1);
        tplRT.anchoredPosition = new Vector2(0,-4);
        tplRT.sizeDelta = new Vector2(0,0); // chieu cao do Content quyet dinh
        tplGO.AddComponent<Image>().color = DD_BG;
        var sr = tplGO.AddComponent<ScrollRect>();
        sr.horizontal = false;
        tplGO.SetActive(false); // QUAN TRONG

        // Viewport
        var vpGO = new GameObject("Viewport"); vpGO.transform.SetParent(tplGO.transform,false);
        var vpRT = vpGO.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = vpRT.offsetMax = Vector2.zero;
        vpGO.AddComponent<Image>().color = DD_BG;
        vpGO.AddComponent<Mask>().showMaskGraphic = true;
        sr.viewport = vpRT;

        // Content
        var ctGO = new GameObject("Content"); ctGO.transform.SetParent(vpGO.transform,false);
        var ctRT = ctGO.AddComponent<RectTransform>();
        ctRT.anchorMin = new Vector2(0,1); ctRT.anchorMax = new Vector2(1,1);
        ctRT.pivot     = new Vector2(0.5f,1);
        ctRT.anchoredPosition = Vector2.zero;
        ctRT.sizeDelta = Vector2.zero;
        sr.content = ctRT;

        // Item prefab
        var iGO = new GameObject("Item"); iGO.transform.SetParent(ctGO.transform,false);
        var iRT = iGO.AddComponent<RectTransform>();
        iRT.anchorMin = new Vector2(0,0.5f); iRT.anchorMax = new Vector2(1,0.5f);
        iRT.sizeDelta = new Vector2(0,28);
        var iBG = iGO.AddComponent<Image>(); iBG.color = new Color(0,0,0,0);
        var iTgl = iGO.AddComponent<Toggle>();
        iTgl.targetGraphic = iBG;

        var iLblGO = new GameObject("Item Label"); iLblGO.transform.SetParent(iGO.transform,false);
        var iLblRT = iLblGO.AddComponent<RectTransform>();
        iLblRT.anchorMin = Vector2.zero; iLblRT.anchorMax = Vector2.one;
        iLblRT.offsetMin = new Vector2(8,0); iLblRT.offsetMax = Vector2.zero;
        var iLblT = iLblGO.AddComponent<TextMeshProUGUI>();
        iLblT.fontSize = 13; iLblT.color = TEXT_PRI;
        iLblT.alignment = TextAlignmentOptions.MidlineLeft;

        iTgl.graphic = iLblT;
        dd.itemText = iLblT;
        dd.template = tplRT;
        // KHONG goi AddOptions hay RefreshShownValue o day
        // vi se lam TMP_Dropdown hien template (hop trang)
        // SettingsUI se populate options luc runtime
        return dd;
    }

    Toggle ToggleRow(Transform p, string ico, string label, Color accent, ref float y, bool def)
    {
        var row = Row(p,"TglRow_"+label, ref y);

        var icT = MakeTMP(row,"Icon","["+ico+"]",12,TEXT_MUT,FontStyles.Bold,TextAlignmentOptions.Center);
        StretchCustom(icT.gameObject,new Vector2(0,0),new Vector2(0,1),new Vector2(10,0),new Vector2(46,0));

        var lbT = MakeTMP(row,"Label",label,15,TEXT_PRI,FontStyles.Normal,TextAlignmentOptions.MidlineLeft);
        StretchCustom(lbT.gameObject,new Vector2(0,0),new Vector2(0.7f,1),new Vector2(50,0),Vector2.zero);

        // Pill
        var pillGO = new GameObject("Pill"); pillGO.transform.SetParent(row,false);
        var pillRT = pillGO.AddComponent<RectTransform>();
        pillRT.anchorMin = pillRT.anchorMax = pillRT.pivot = new Vector2(1,0.5f);
        pillRT.anchoredPosition = new Vector2(-14,0); pillRT.sizeDelta = new Vector2(54,28);
        var pillBG = pillGO.AddComponent<Image>(); pillBG.color = def ? accent : OFF_PILL;

        // Knob
        var knobGO = new GameObject("Knob"); knobGO.transform.SetParent(pillGO.transform,false);
        var knobRT = knobGO.AddComponent<RectTransform>();
        knobRT.sizeDelta = new Vector2(22,22);
        knobRT.anchorMin = knobRT.anchorMax = knobRT.pivot = new Vector2(def?1f:0f, 0.5f);
        knobRT.anchoredPosition = new Vector2(def?-3f:3f, 0);
        var knobImg = knobGO.AddComponent<Image>(); knobImg.color = Color.white;

        var tgl = pillGO.AddComponent<Toggle>();
        tgl.targetGraphic = pillBG; tgl.graphic = knobImg; tgl.isOn = def;
        tgl.onValueChanged.AddListener(on => pillBG.color = on ? accent : OFF_PILL);
        return tgl;
    }

    // ═══════════════════════════════ SLIDER ════════════════════════════════
    Slider MakeSlider(Transform parent, Vector2 aMin, Vector2 aMax,
        Color accent, float min, float max, float def)
    {
        var go = new GameObject("Slider"); go.transform.SetParent(parent,false);
        var r  = go.AddComponent<RectTransform>();
        r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = r.offsetMax = Vector2.zero;
        var sl = go.AddComponent<Slider>();

        // Track BG
        var bg = new GameObject("Bg"); bg.transform.SetParent(go.transform,false);
        var bR = bg.AddComponent<RectTransform>();
        bR.anchorMin = new Vector2(0,0.35f); bR.anchorMax = new Vector2(1,0.65f);
        bR.offsetMin = bR.offsetMax = Vector2.zero;
        bg.AddComponent<Image>().color = SLI_BG;

        // Fill Area
        var fa = new GameObject("FillArea"); fa.transform.SetParent(go.transform,false);
        var faR = fa.AddComponent<RectTransform>();
        faR.anchorMin = new Vector2(0,0.35f); faR.anchorMax = new Vector2(1,0.65f);
        faR.offsetMin = faR.offsetMax = Vector2.zero;

        var fi = new GameObject("Fill"); fi.transform.SetParent(fa.transform,false);
        var fR = fi.AddComponent<RectTransform>();
        fR.anchorMin = Vector2.zero;
        fR.anchorMax = new Vector2(Mathf.InverseLerp(min,max,def),1);
        fR.offsetMin = fR.offsetMax = Vector2.zero;
        fi.AddComponent<Image>().color = accent;

        // Handle
        var ha = new GameObject("HndArea"); ha.transform.SetParent(go.transform,false);
        var haR = ha.AddComponent<RectTransform>();
        haR.anchorMin = Vector2.zero; haR.anchorMax = Vector2.one;
        haR.offsetMin = haR.offsetMax = Vector2.zero;

        var hd = new GameObject("Handle"); hd.transform.SetParent(ha.transform,false);
        var hR = hd.AddComponent<RectTransform>();
        hR.sizeDelta = new Vector2(16,26);
        hR.anchorMin = new Vector2(Mathf.InverseLerp(min,max,def),0);
        hR.anchorMax = new Vector2(Mathf.InverseLerp(min,max,def),1);
        var hImg = hd.AddComponent<Image>(); hImg.color = TEXT_PRI;
        var hOL  = hd.AddComponent<Outline>(); hOL.effectColor = accent; hOL.effectDistance = new Vector2(1,-1);

        sl.fillRect = fR; sl.handleRect = hR; sl.targetGraphic = hImg;
        sl.direction = Slider.Direction.LeftToRight;
        sl.minValue = min; sl.maxValue = max; sl.value = def;
        return sl;
    }

    // ═══════════════════════════════ FACTORY ═══════════════════════════════

    // Tao UI GameObject (RT + Image)  *** RT TRUOC, Image SAU ***
    static GameObject UI(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();   // 1) RT truoc
        go.AddComponent<Image>();           // 2) Image sau (dung RT co san)
        return go;
    }

    // Lay RectTransform (da co)
    static RectTransform RT(GameObject go) => go.GetComponent<RectTransform>();

    // Stretch full parent
    static void Stretch(GameObject go)
    {
        var r = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    // Stretch voi anchor/offset tuy chinh
    static void StretchCustom(GameObject go, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax)
    {
        var r = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
        r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = oMin; r.offsetMax = oMax;
    }

    // Tab panel (disabled ban dau)
    static GameObject TabPanel(Transform parent, string name, float pad)
    {
        var go = new GameObject(name); go.transform.SetParent(parent,false);
        var r  = go.AddComponent<RectTransform>();
        r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
        r.offsetMin = new Vector2(pad,pad); r.offsetMax = new Vector2(-pad,-pad);
        go.SetActive(false);
        return go;
    }

    // Row co nen xam
    static Transform Row(Transform parent, string name, ref float y)
    {
        var go = new GameObject(name); go.transform.SetParent(parent,false);
        var r  = go.AddComponent<RectTransform>();
        r.anchorMin = new Vector2(0,1); r.anchorMax = new Vector2(1,1);
        r.pivot     = new Vector2(0.5f,1);
        r.anchoredPosition = new Vector2(0,y);
        r.sizeDelta = new Vector2(0,ROW_H);
        y -= ROW_H + ROW_G;

        var bgGO = new GameObject("RowBG"); bgGO.transform.SetParent(go.transform,false);
        bgGO.AddComponent<RectTransform>(); bgGO.AddComponent<Image>().color = BG_ROW;
        Stretch(bgGO);
        return go.transform;
    }

    // Tab Button
    static Button TabBtn(Transform parent, string name, string label, Color accent)
    {
        var go = UI(name, parent);
        go.GetComponent<Image>().color = new Color(0.05f,0.06f,0.08f,1f);
        var btn = go.AddComponent<Button>();

        // Indicator bar (duoi tab, an mac dinh)
        var ind = new GameObject("Ind"); ind.transform.SetParent(go.transform,false);
        var iR  = ind.AddComponent<RectTransform>();
        iR.anchorMin = new Vector2(0.05f,0); iR.anchorMax = new Vector2(0.95f,0);
        iR.pivot     = new Vector2(0.5f,0); iR.sizeDelta = new Vector2(0,3);
        ind.AddComponent<Image>().color = accent;
        ind.SetActive(false);

        // Text (dung mau sang de thay ro tren nen toi)
        var txtGO = new GameObject("Txt"); txtGO.transform.SetParent(go.transform,false);
        txtGO.AddComponent<RectTransform>(); Stretch(txtGO);
        var t = txtGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = 13; t.fontStyle = FontStyles.Bold;
        t.color = Color.white; t.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    // Button (RT + Image + Button + TMP text)
    static Button Btn(Transform parent, string name, string label,
        Color bg, Color fg, int sz, FontStyles style)
    {
        var go = new GameObject(name); go.transform.SetParent(parent,false);
        go.AddComponent<RectTransform>();   // RT truoc
        go.AddComponent<Image>().color = bg;
        var btn = go.AddComponent<Button>();

        var txtGO = new GameObject("Txt"); txtGO.transform.SetParent(go.transform,false);
        txtGO.AddComponent<RectTransform>(); Stretch(txtGO);
        var t = txtGO.AddComponent<TextMeshProUGUI>();
        t.text = label; t.fontSize = sz; t.fontStyle = style;
        t.color = fg; t.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    // TextMeshProUGUI (RT duoc them boi AddComponent<TextMeshProUGUI> tu dong)
    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        float sz, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name); go.transform.SetParent(parent,false);
        go.AddComponent<RectTransform>(); // RT truoc TMP
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = sz; t.color = color; t.fontStyle = style; t.alignment = align;
        return t;
    }

    string FmtV(float v, float min, float max)
        => (min==0&&max==1) ? Mathf.RoundToInt(v*100)+"%" : v.ToString("F1");

    void Cleanup()
    {
        foreach (var x in FindObjectsByType<SettingsUI>     (FindObjectsSortMode.None)) DestroyImmediate(x.gameObject);
        foreach (var x in FindObjectsByType<SettingsManager>(FindObjectsSortMode.None)) DestroyImmediate(x.gameObject);
        foreach (var x in FindObjectsByType<UIAudioFeedback>(FindObjectsSortMode.None)) DestroyImmediate(x.gameObject);
        foreach (var n in new[]{"SettingsCanvas","SettingsManager","UIAudioFeedback"})
        { var g = GameObject.Find(n); if (g) DestroyImmediate(g); }
        Debug.Log("[SettingsSetupTool] Da don dep.");
    }
}
