using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// SettingsUI – Minimalist.
/// Gang vao SettingsCanvas. Mo/Dong bang ESC hoac goi Toggle().
/// Hieu ung: Panel truot len tu duoi man hinh (slide-up).
/// </summary>
public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    [Header("Panel")]
    public GameObject settingsPanel;
    public Image      backdropImage;

    [Header("Audio")]
    public Slider          sliderMaster;
    public TextMeshProUGUI labelMaster;
    public Slider          sliderMusic;
    public TextMeshProUGUI labelMusic;
    public Slider          sliderSFX;
    public TextMeshProUGUI labelSFX;

    [Header("Graphics")]
    public TMP_Dropdown dropdownQuality;
    public TMP_Dropdown dropdownResolution;
    public Toggle       toggleFullscreen;

    [Header("Mouse")]
    public Slider          sliderSensitivity;
    public TextMeshProUGUI labelSensitivity;

    // Cursor fields (optional – khong bat buoc wire)
    [Header("Cursor (optional)")]
    public Toggle       toggleInvertY;
    public Toggle       toggleCursorTrail;
    public Slider       sliderCursorSize;
    public TextMeshProUGUI labelCursorSize;
    public Toggle       toggleSmoothMouse;
    public Toggle       toggleCursorHighlight;
    public TMP_Dropdown dropdownCursorStyle;

    [Header("Buttons")]
    public Button btnApply;
    public Button btnReset;
    public Button btnClose;

    // Tab fields – giu de khong loi tuong thich voi tool cu (co the bo trong)
    [HideInInspector] public Button  tabAudioBtn;
    [HideInInspector] public Button  tabGraphicsBtn;
    [HideInInspector] public Button  tabControlsBtn;
    [HideInInspector] public GameObject panelAudio;
    [HideInInspector] public GameObject panelGraphics;
    [HideInInspector] public GameObject panelControls;

    [Header("Animation")]
    [Tooltip("Thoi gian mo (giay)")]
    public float openDuration  = 0.28f;
    [Tooltip("Thoi gian dong (giay)")]
    public float closeDuration = 0.20f;
    [Tooltip("Panel truot vao tu do cao nay (pixel)")]
    public float slideOffset   = 80f;

    // ── Private ──────────────────────────────
    bool      _isOpen;
    CanvasGroup _panelCG;
    RectTransform _panelRT;
    Vector2   _closedPos;
    Vector2   _openPos;
    Coroutine _anim;

    // ════════════════════════════════════════
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Lay components
        if (settingsPanel)
        {
            _panelCG  = settingsPanel.GetComponent<CanvasGroup>();
            _panelRT  = settingsPanel.GetComponent<RectTransform>();
        }

        if (_panelRT != null)
        {
            _openPos   = _panelRT.anchoredPosition;
            _closedPos = _openPos - new Vector2(0, slideOffset);
        }

        // Wire listeners
        if (sliderMaster)  sliderMaster.onValueChanged.AddListener(v  => UpdateLabel(labelMaster, v));
        if (sliderMusic)   sliderMusic.onValueChanged.AddListener(v   => UpdateLabel(labelMusic, v));
        if (sliderSFX)     sliderSFX.onValueChanged.AddListener(v     => UpdateLabel(labelSFX, v));
        if (sliderSensitivity) sliderSensitivity.onValueChanged.AddListener(v
            => UpdateLabel(labelSensitivity, v, "x"));
        if (sliderCursorSize) sliderCursorSize.onValueChanged.AddListener(v
            => UpdateLabel(labelCursorSize, v, "x"));

        if (btnApply) btnApply.onClick.AddListener(OnApply);
        if (btnReset) btnReset.onClick.AddListener(OnReset);
        if (btnClose) btnClose.onClick.AddListener(() => Close());

        // Dong ngay
        ForceClose();
        PopulateResolutionDropdown();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Toggle();
    }

    // ════════════════════════════════════════
    //   Open / Close / Toggle
    // ════════════════════════════════════════
    public void Toggle() { if (_isOpen) Close(); else Open(); }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        settingsPanel?.SetActive(true);
        RefreshUI();

        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AnimateOpen());

        if (SettingsManager.Instance != null)
        {
            Cursor.visible   = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;

        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(AnimateClose());
    }

    void ForceClose()
    {
        _isOpen = false;
        if (_panelCG != null)
        {
            _panelCG.alpha          = 0;
            _panelCG.interactable   = false;
            _panelCG.blocksRaycasts = false;
        }
        if (backdropImage) backdropImage.color = new Color(0,0,0,0);
        if (_panelRT != null) _panelRT.anchoredPosition = _closedPos;
        settingsPanel?.SetActive(false);
    }

    // ════════════════════════════════════════
    //   Animations  (slide-up)
    // ════════════════════════════════════════
    IEnumerator AnimateOpen()
    {
        settingsPanel.SetActive(true);
        if (_panelCG != null) { _panelCG.interactable = true; _panelCG.blocksRaycasts = true; }

        float t = 0;
        var startPos = _closedPos;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / openDuration;
            float e = EaseOutCubic(Mathf.Clamp01(t));
            if (_panelCG  != null) _panelCG.alpha = e;
            if (_panelRT  != null) _panelRT.anchoredPosition = Vector2.Lerp(startPos, _openPos, e);
            if (backdropImage) backdropImage.color = new Color(0,0,0, Mathf.Lerp(0, 0.55f, e));
            yield return null;
        }
        if (_panelCG  != null) _panelCG.alpha = 1;
        if (_panelRT  != null) _panelRT.anchoredPosition = _openPos;
        if (backdropImage) backdropImage.color = new Color(0,0,0,0.55f);
    }

    IEnumerator AnimateClose()
    {
        if (_panelCG != null) { _panelCG.interactable = false; _panelCG.blocksRaycasts = false; }

        float t = 0;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / closeDuration;
            float e = EaseInCubic(Mathf.Clamp01(t));
            if (_panelCG  != null) _panelCG.alpha = 1 - e;
            if (_panelRT  != null) _panelRT.anchoredPosition = Vector2.Lerp(_openPos, _closedPos, e);
            if (backdropImage) backdropImage.color = new Color(0,0,0, Mathf.Lerp(0.55f, 0, e));
            yield return null;
        }
        settingsPanel?.SetActive(false);
        if (backdropImage) backdropImage.color = new Color(0,0,0,0);
    }

    static float EaseOutCubic(float t) => 1 - Mathf.Pow(1 - t, 3);
    static float EaseInCubic(float t)  => t * t * t;

    // ════════════════════════════════════════
    //   RefreshUI
    // ════════════════════════════════════════
    void RefreshUI()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        if (sliderMaster)  { sliderMaster.value  = sm.masterVolume;  UpdateLabel(labelMaster,  sm.masterVolume); }
        if (sliderMusic)   { sliderMusic.value   = sm.musicVolume;   UpdateLabel(labelMusic,   sm.musicVolume); }
        if (sliderSFX)     { sliderSFX.value     = sm.sfxVolume;     UpdateLabel(labelSFX,     sm.sfxVolume); }

        if (dropdownQuality)    dropdownQuality.value    = sm.qualityLevel;
        if (dropdownResolution) dropdownResolution.value = sm.resolutionIdx;
        if (toggleFullscreen)   toggleFullscreen.isOn    = sm.fullscreen;

        if (sliderSensitivity) { sliderSensitivity.value = sm.mouseSensitivity;
                                  UpdateLabel(labelSensitivity, sm.mouseSensitivity, "x"); }
        if (toggleInvertY)     toggleInvertY.isOn     = sm.invertYAxis;
        if (toggleCursorTrail) toggleCursorTrail.isOn = sm.cursorTrailEnabled;
        if (sliderCursorSize)  { sliderCursorSize.value = sm.cursorSize;
                                  UpdateLabel(labelCursorSize, sm.cursorSize, "x"); }
        if (toggleSmoothMouse)     toggleSmoothMouse.isOn     = sm.smoothMouse;
        if (toggleCursorHighlight) toggleCursorHighlight.isOn = sm.cursorHighlight;
        if (dropdownCursorStyle)   dropdownCursorStyle.value   = sm.cursorStyle;
    }

    // ════════════════════════════════════════
    //   Apply / Reset
    // ════════════════════════════════════════
    void OnApply()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        if (sliderMaster)       sm.masterVolume      = sliderMaster.value;
        if (sliderMusic)        sm.musicVolume       = sliderMusic.value;
        if (sliderSFX)          sm.sfxVolume         = sliderSFX.value;
        if (dropdownQuality)    sm.qualityLevel      = dropdownQuality.value;
        if (dropdownResolution) sm.resolutionIdx     = dropdownResolution.value;
        if (toggleFullscreen)   sm.fullscreen        = toggleFullscreen.isOn;
        if (sliderSensitivity)  sm.mouseSensitivity  = sliderSensitivity.value;
        if (toggleInvertY)      sm.invertYAxis       = toggleInvertY.isOn;
        if (toggleCursorTrail)  sm.cursorTrailEnabled = toggleCursorTrail.isOn;
        if (sliderCursorSize)   sm.cursorSize        = sliderCursorSize.value;
        if (toggleSmoothMouse)  sm.smoothMouse       = toggleSmoothMouse.isOn;
        if (toggleCursorHighlight) sm.cursorHighlight = toggleCursorHighlight.isOn;
        if (dropdownCursorStyle)   sm.cursorStyle    = dropdownCursorStyle.value;

        sm.SaveSettings();
        sm.ApplyAll();

        UIAudioFeedback.Play(UIAudioFeedback.SoundType.Confirm);
        StartCoroutine(FlashButton(btnApply, "DA LUU!"));
    }

    void OnReset()
    {
        SettingsManager.Instance?.ResetToDefault();
        RefreshUI();
        UIAudioFeedback.Play(UIAudioFeedback.SoundType.Tab);
        StartCoroutine(FlashButton(btnReset, "DA DAT LAI!"));
    }

    IEnumerator FlashButton(Button btn, string msg)
    {
        if (!btn) yield break;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (!tmp) yield break;
        string orig = tmp.text;
        tmp.text = msg;
        yield return new WaitForSecondsRealtime(1.4f);
        tmp.text = orig;
    }

    // ════════════════════════════════════════
    //   Helpers
    // ════════════════════════════════════════
    static void UpdateLabel(TextMeshProUGUI lbl, float v, string suffix = "")
    {
        if (!lbl) return;
        lbl.text = suffix == "" || suffix == "%"
            ? Mathf.RoundToInt(v * 100) + "%"
            : v.ToString("F1") + suffix;
    }

    void PopulateResolutionDropdown()
    {
        if (!dropdownResolution) return;
        dropdownResolution.ClearOptions();
        var opts = new System.Collections.Generic.List<string>();
        foreach (var r in Screen.resolutions)
            opts.Add($"{r.width} x {r.height}");
        dropdownResolution.AddOptions(opts);
        int max = Screen.resolutions.Length - 1;
        dropdownResolution.value = SettingsManager.Instance != null
            ? SettingsManager.Instance.resolutionIdx
            : max;
    }

    // ────────────────────────────────────────
    //  Tab switching (giu de khong bi loi compile
    //  khi code khac goi SwitchTab)
    // ────────────────────────────────────────
    public void SwitchTab(int _) { }
}
