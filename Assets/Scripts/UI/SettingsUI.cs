using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI Menu Cài Đặt trong game – phiên bản nâng cấp với animation &amp; audio.
/// Gắn vào SettingsCanvas. Mở/đóng bằng phím Escape hoặc gọi Toggle().
/// </summary>
public class SettingsUI : MonoBehaviour
{
    public static SettingsUI Instance { get; private set; }

    [Header("Root Panel")]
    public GameObject settingsPanel;

    [Header("Tab Buttons")]
    public Button tabAudioBtn;
    public Button tabGraphicsBtn;
    public Button tabControlsBtn;

    [Header("Tab Panels")]
    public GameObject panelAudio;
    public GameObject panelGraphics;
    public GameObject panelControls;

    // ── Audio ──
    [Header("Audio Controls")]
    public Slider sliderMaster;
    public Slider sliderMusic;
    public Slider sliderSFX;
    public TextMeshProUGUI labelMaster;
    public TextMeshProUGUI labelMusic;
    public TextMeshProUGUI labelSFX;

    // ── Graphics ──
    [Header("Graphics Controls")]
    public TMP_Dropdown dropdownQuality;
    public TMP_Dropdown dropdownResolution;
    public Toggle toggleFullscreen;

    // ── Mouse / Cursor ──
    [Header("Mouse & Cursor Controls")]
    public Slider          sliderSensitivity;
    public TextMeshProUGUI labelSensitivity;
    public Toggle          toggleInvertY;
    // Hieu ung con tro chuot moi
    public Toggle          toggleCursorTrail;
    public Slider          sliderCursorSize;
    public TextMeshProUGUI labelCursorSize;
    public Toggle          toggleSmoothMouse;
    public Toggle          toggleCursorHighlight;
    public TMP_Dropdown    dropdownCursorStyle;

    // ── Buttons ──
    [Header("Action Buttons")]
    public Button btnApply;
    public Button btnReset;
    public Button btnClose;

    [Header("Animation Settings")]
    [Tooltip("Thời gian mở/đóng panel (giây)")]
    public float openDuration   = 0.22f;
    public float closeDuration  = 0.16f;
    [Tooltip("Scale khởi điểm khi mở")]
    public float openStartScale = 0.82f;
    [Tooltip("Panel có Backdrop mờ không")]
    public bool  useBackdrop    = true;
    public Image backdropImage;

    bool _isOpen = false;
    public bool IsOpen => _isOpen;

    RectTransform _panelRect;
    CanvasGroup   _panelCG;
    int _currentTab = 0;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // CanvasGroup để fade in/out
        if (settingsPanel != null)
        {
            _panelRect = settingsPanel.GetComponent<RectTransform>();
            _panelCG   = settingsPanel.GetComponent<CanvasGroup>();
            if (_panelCG == null) _panelCG = settingsPanel.AddComponent<CanvasGroup>();
        }

        // Dropdowns
        SetupResolutionDropdown();
        SetupQualityDropdown();
        RefreshUI();

        // Tab buttons - gọi SwitchTab (tên đúng trong phần lò này)
        if (tabAudioBtn)    tabAudioBtn.onClick.AddListener(() => SwitchTab(0));
        if (tabGraphicsBtn) tabGraphicsBtn.onClick.AddListener(() => SwitchTab(1));
        if (tabControlsBtn) tabControlsBtn.onClick.AddListener(() => SwitchTab(2));

        // Slider audio feedback (slider tick khi kéo)
        WireSliderAudio(sliderMaster);
        WireSliderAudio(sliderMusic);
        WireSliderAudio(sliderSFX);
        WireSliderAudio(sliderSensitivity);
        WireSliderAudio(sliderCursorSize);

        // Slider label update
        if (sliderMaster)      sliderMaster.onValueChanged.AddListener(v => UpdateLabel(labelMaster, v));
        if (sliderMusic)       sliderMusic.onValueChanged.AddListener(v  => UpdateLabel(labelMusic, v));
        if (sliderSFX)         sliderSFX.onValueChanged.AddListener(v    => UpdateLabel(labelSFX, v));
        if (sliderSensitivity) sliderSensitivity.onValueChanged.AddListener(v => UpdateLabel(labelSensitivity, v, "x"));
        if (sliderCursorSize)  sliderCursorSize.onValueChanged.AddListener(v  => UpdateLabel(labelCursorSize, v, "x"));

        // Action buttons
        if (btnApply) btnApply.onClick.AddListener(OnClickApply);
        if (btnReset) btnReset.onClick.AddListener(OnClickReset);
        if (btnClose) btnClose.onClick.AddListener(Close);

        // Thêm UIButtonEffect tự động
        AutoAttachEffects();

        // Bắt đầu trong trạng thái đóng
        ForceClose();
        SwitchTab(0, silent: true);
    }

    void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Toggle();
        }
    }

    // ──────────────────────────────────────
    //   Open / Close / Toggle
    // ──────────────────────────────────────
    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        StopAllCoroutines();
        settingsPanel.SetActive(true);
        if (backdropImage) StartCoroutine(FadeBackdrop(0f, 0.55f, openDuration));
        StartCoroutine(AnimatePanel(isOpening: true));
        RefreshUI();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        UIAudioFeedback.Play(UIAudioFeedback.SoundType.Open);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        StopAllCoroutines();
        if (backdropImage) StartCoroutine(FadeBackdrop(backdropImage.color.a, 0f, closeDuration));
        StartCoroutine(AnimatePanel(isOpening: false));
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
        UIAudioFeedback.Play(UIAudioFeedback.SoundType.Close);
    }

    public void Toggle() { if (_isOpen) Close(); else Open(); }

    void ForceClose()
    {
        _isOpen = false;
        if (settingsPanel) settingsPanel.SetActive(false);
        if (_panelCG)
        {
            _panelCG.alpha          = 0f;
            _panelCG.interactable   = false;
            _panelCG.blocksRaycasts = false;
        }
        if (_panelRect) _panelRect.localScale = Vector3.one;
        // Ẩn cả backdrop ngay lập tức
        if (backdropImage) backdropImage.color = new Color(0, 0, 0, 0);
    }

    // ──────────────────────────────────────
    //   Panel Animation (scale + fade)
    // ──────────────────────────────────────
    IEnumerator AnimatePanel(bool isOpening)
    {
        if (_panelCG == null || _panelRect == null) yield break;

        float duration  = isOpening ? openDuration : closeDuration;
        float startAlpha = isOpening ? 0f  : 1f;
        float endAlpha   = isOpening ? 1f  : 0f;
        float startScale = isOpening ? openStartScale : 1f;
        float endScale   = isOpening ? 1f  : openStartScale * 0.9f;

        _panelCG.interactable   = false;
        _panelCG.blocksRaycasts = false;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float ease = isOpening ? EaseOutBack(t) : EaseInCubic(t);

            _panelCG.alpha = Mathf.Lerp(startAlpha, endAlpha, Mathf.Clamp01(t));
            float s = Mathf.Lerp(startScale, endScale, ease);
            _panelRect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }

        _panelCG.alpha = endAlpha;
        _panelRect.localScale = Vector3.one * endScale;

        if (!isOpening)
            settingsPanel.SetActive(false);
        else
        {
            _panelCG.interactable   = true;
            _panelCG.blocksRaycasts = true;
            _panelRect.localScale   = Vector3.one;
        }
    }

    IEnumerator FadeBackdrop(float from, float to, float duration)
    {
        if (backdropImage == null) yield break;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float a = Mathf.Lerp(from, to, t);
            backdropImage.color = new Color(0, 0, 0, a);
            yield return null;
        }
        backdropImage.color = new Color(0, 0, 0, to);
    }

    // ──────────────────────────────────────
    //   Tab Switching với slide animation
    // ──────────────────────────────────────
    void SwitchTab(int index, bool silent = false)
    {
        if (index == _currentTab && !silent) return;
        _currentTab = index;

        if (!silent) UIAudioFeedback.Play(UIAudioFeedback.SoundType.Tab);

        // Slide out current, slide in new
        ShowTabPanel(panelAudio,    index == 0);
        ShowTabPanel(panelGraphics, index == 1);
        ShowTabPanel(panelControls, index == 2);

        // Button highlight
        HighlightTabBtn(tabAudioBtn,    index == 0);
        HighlightTabBtn(tabGraphicsBtn, index == 1);
        HighlightTabBtn(tabControlsBtn, index == 2);
    }

    void ShowTabPanel(GameObject panel, bool show)
    {
        if (panel == null) return;
        panel.SetActive(show);
        if (show)
        {
            // Slide in từ phải
            var rt = panel.GetComponent<RectTransform>();
            if (rt != null)
                StartCoroutine(SlideIn(rt));
        }
    }

    IEnumerator SlideIn(RectTransform rt)
    {
        float t = 0f;
        Vector3 startPos = new Vector3(40f, 0, 0);
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / 0.15f;
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f);
            rt.anchoredPosition = Vector3.Lerp(startPos, Vector3.zero, ease);
            yield return null;
        }
        rt.anchoredPosition = Vector3.zero;
    }

    void HighlightTabBtn(Button btn, bool active)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = active
            ? new Color(0.22f, 0.68f, 0.38f)
            : new Color(0.18f, 0.18f, 0.22f, 0.9f);
        btn.colors = colors;

        // Scale nút tab đang active nhẹ hơn
        var rect = btn.GetComponent<RectTransform>();
        if (rect != null)
            StopCoroutine("ScaleTab");
        StartCoroutine(ScaleTab(btn.transform, active ? 1.04f : 1f));
    }

    IEnumerator ScaleTab(Transform t, float target)
    {
        float elapsed = 0f;
        Vector3 start = t.localScale;
        Vector3 end   = Vector3.one * target;
        while (elapsed < 0.12f)
        {
            elapsed += Time.unscaledDeltaTime;
            t.localScale = Vector3.Lerp(start, end, elapsed / 0.12f);
            yield return null;
        }
        t.localScale = end;
    }

    // ──────────────────────────────────────
    //   Refresh UI from SettingsManager
    // ──────────────────────────────────────
    void RefreshUI()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        if (sliderMaster)  { sliderMaster.value  = sm.masterVolume;   UpdateLabel(labelMaster, sm.masterVolume); }
        if (sliderMusic)   { sliderMusic.value   = sm.musicVolume;    UpdateLabel(labelMusic,  sm.musicVolume); }
        if (sliderSFX)     { sliderSFX.value     = sm.sfxVolume;      UpdateLabel(labelSFX,    sm.sfxVolume); }

        if (dropdownQuality)    dropdownQuality.value    = sm.qualityLevel;
        if (dropdownResolution) dropdownResolution.value = sm.resolutionIdx;
        if (toggleFullscreen)   toggleFullscreen.isOn    = sm.fullscreen;

        if (sliderSensitivity)    { sliderSensitivity.value = sm.mouseSensitivity; UpdateLabel(labelSensitivity, sm.mouseSensitivity, "x"); }
        if (toggleInvertY)          toggleInvertY.isOn       = sm.invertYAxis;
        // Cursor fields
        if (toggleCursorTrail)    toggleCursorTrail.isOn    = sm.cursorTrailEnabled;
        if (sliderCursorSize)     { sliderCursorSize.value  = sm.cursorSize; UpdateLabel(labelCursorSize, sm.cursorSize, "x"); }
        if (toggleSmoothMouse)    toggleSmoothMouse.isOn    = sm.smoothMouse;
        if (toggleCursorHighlight) toggleCursorHighlight.isOn = sm.cursorHighlight;
        if (dropdownCursorStyle)  dropdownCursorStyle.value  = sm.cursorStyle;
    }

    // ──────────────────────────────────────
    //   Apply / Reset
    // ──────────────────────────────────────
    void OnClickApply()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        sm.masterVolume       = sliderMaster         ? sliderMaster.value         : sm.masterVolume;
        sm.musicVolume        = sliderMusic          ? sliderMusic.value          : sm.musicVolume;
        sm.sfxVolume          = sliderSFX            ? sliderSFX.value            : sm.sfxVolume;
        sm.qualityLevel       = dropdownQuality      ? dropdownQuality.value      : sm.qualityLevel;
        sm.resolutionIdx      = dropdownResolution   ? dropdownResolution.value   : sm.resolutionIdx;
        sm.fullscreen         = toggleFullscreen     ? toggleFullscreen.isOn      : sm.fullscreen;
        sm.mouseSensitivity   = sliderSensitivity    ? sliderSensitivity.value    : sm.mouseSensitivity;
        sm.invertYAxis        = toggleInvertY        ? toggleInvertY.isOn         : sm.invertYAxis;
        // Cursor
        sm.cursorTrailEnabled = toggleCursorTrail    ? toggleCursorTrail.isOn     : sm.cursorTrailEnabled;
        sm.cursorSize         = sliderCursorSize     ? sliderCursorSize.value     : sm.cursorSize;
        sm.smoothMouse        = toggleSmoothMouse    ? toggleSmoothMouse.isOn     : sm.smoothMouse;
        sm.cursorHighlight    = toggleCursorHighlight? toggleCursorHighlight.isOn : sm.cursorHighlight;
        sm.cursorStyle        = dropdownCursorStyle  ? dropdownCursorStyle.value  : sm.cursorStyle;

        sm.SaveSettings();
        sm.ApplyAll();

        UIAudioFeedback.Play(UIAudioFeedback.SoundType.Confirm);

        // Bounce effect trên nút Apply
        if (btnApply) btnApply.GetComponent<UIButtonEffect>()?.PlayBounce();

        StartCoroutine(ShowFeedback(btnApply, "✓ Đã lưu!"));
    }

    void OnClickReset()
    {
        if (SettingsManager.Instance) SettingsManager.Instance.ResetToDefault();
        RefreshUI();
        UIAudioFeedback.Play(UIAudioFeedback.SoundType.Tab);
        if (btnReset) btnReset.GetComponent<UIButtonEffect>()?.PlayBounce();
        StartCoroutine(ShowFeedback(btnReset, "✓ Đặt lại!"));
    }

    IEnumerator ShowFeedback(Button btn, string msg)
    {
        if (btn == null) yield break;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp == null) yield break;
        string original = tmp.text;
        tmp.text = msg;
        yield return new WaitForSecondsRealtime(1.5f);
        tmp.text = original;
    }

    // ──────────────────────────────────────
    //   Auto-attach UIButtonEffect + Audio
    // ──────────────────────────────────────
    void AutoAttachEffects()
    {
        // Đảm bảo UIAudioFeedback tồn tại
        if (UIAudioFeedback.Instance == null)
        {
            var afGO = new GameObject("UIAudioFeedback");
            afGO.AddComponent<UIAudioFeedback>();
        }

        // Attach UIButtonEffect lên tất cả Button trong Settings panel
        if (settingsPanel == null) return;
        foreach (var btn in settingsPanel.GetComponentsInChildren<Button>(true))
        {
            if (btn.GetComponent<UIButtonEffect>() == null)
            {
                var eff = btn.gameObject.AddComponent<UIButtonEffect>();
                // Nút Close nhỏ hơn → effect nhẹ hơn
                if (btn == btnClose)
                {
                    eff.hoverScale = 1.12f;
                    eff.glowColor  = new Color(1f, 0.3f, 0.3f, 0.8f);
                }
                // Nút Tab: glow xanh lá
                if (btn == tabAudioBtn || btn == tabGraphicsBtn || btn == tabControlsBtn)
                {
                    eff.hoverScale = 1.05f;
                    eff.enableRipple = false;
                }
                // Nút Apply: glow xanh mạnh hơn
                if (btn == btnApply)
                {
                    eff.glowColor  = new Color(0.2f, 1f, 0.5f, 0.9f);
                    eff.hoverScale = 1.06f;
                }
            }
        }

        // Attach Outline lên tất cả Button (nếu chưa có) để glow hoạt động
        foreach (var btn in settingsPanel.GetComponentsInChildren<Button>(true))
        {
            if (btn.GetComponent<Outline>() == null)
            {
                var ol = btn.gameObject.AddComponent<Outline>();
                ol.enabled = false; // tắt mặc định, UIButtonEffect sẽ bật khi hover
            }
        }
    }

    void WireSliderAudio(Slider slider)
    {
        if (slider == null) return;
        slider.onValueChanged.AddListener(v => UIAudioFeedback.PlaySlider(
            Mathf.InverseLerp(slider.minValue, slider.maxValue, v)));
    }

    // ──────────────────────────────────────
    //   Helpers
    // ──────────────────────────────────────
    void UpdateLabel(TextMeshProUGUI label, float value, string suffix = "%")
    {
        if (label == null) return;
        label.text = suffix == "%" ? Mathf.RoundToInt(value * 100) + "%" : value.ToString("F1") + suffix;
    }

    void SetupResolutionDropdown()
    {
        if (dropdownResolution == null) return;
        dropdownResolution.ClearOptions();
        var resolutions = Screen.resolutions;
        var options = new System.Collections.Generic.List<string>();
        foreach (var r in resolutions)
            options.Add($"{r.width} × {r.height} @ {r.refreshRateRatio.numerator}Hz");
        dropdownResolution.AddOptions(options);
        int idx = SettingsManager.Instance ? SettingsManager.Instance.resolutionIdx : resolutions.Length - 1;
        dropdownResolution.value = Mathf.Clamp(idx, 0, options.Count - 1);
        dropdownResolution.RefreshShownValue();
    }

    void SetupQualityDropdown()
    {
        if (dropdownQuality == null) return;
        dropdownQuality.ClearOptions();
        dropdownQuality.AddOptions(new System.Collections.Generic.List<string>
            { "🔻 Thấp", "🔸 Trung bình", "🔹 Cao", "⭐ Ultra" });
        dropdownQuality.value = SettingsManager.Instance ? SettingsManager.Instance.qualityLevel : 2;
        dropdownQuality.RefreshShownValue();
    }

    // ──────────────────────────────────────
    //   Easing functions
    // ──────────────────────────────────────
    float EaseOutBack(float t)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    float EaseInCubic(float t) => t * t * t;
}
