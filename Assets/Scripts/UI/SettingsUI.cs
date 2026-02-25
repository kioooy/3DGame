using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// UI Menu Cài Đặt trong game.
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

    // ── Controls ──
    [Header("Controls Controls")]
    public Slider sliderSensitivity;
    public TextMeshProUGUI labelSensitivity;
    public Toggle toggleInvertY;

    // ── Buttons ──
    [Header("Action Buttons")]
    public Button btnApply;
    public Button btnReset;
    public Button btnClose;

    bool _isOpen = false;
    public bool IsOpen => _isOpen;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Khởi tạo Dropdowns
        SetupResolutionDropdown();
        SetupQualityDropdown();

        // Load giá trị hiện tại lên UI
        RefreshUI();

        // Gán sự kiện
        if (tabAudioBtn)    tabAudioBtn.onClick.AddListener(() => ShowTab(0));
        if (tabGraphicsBtn) tabGraphicsBtn.onClick.AddListener(() => ShowTab(1));
        if (tabControlsBtn) tabControlsBtn.onClick.AddListener(() => ShowTab(2));

        if (sliderMaster)      sliderMaster.onValueChanged.AddListener(v => { UpdateLabel(labelMaster, v); });
        if (sliderMusic)       sliderMusic.onValueChanged.AddListener(v  => { UpdateLabel(labelMusic, v); });
        if (sliderSFX)         sliderSFX.onValueChanged.AddListener(v    => { UpdateLabel(labelSFX, v); });
        if (sliderSensitivity) sliderSensitivity.onValueChanged.AddListener(v => UpdateLabel(labelSensitivity, v, "x"));

        if (btnApply) btnApply.onClick.AddListener(OnClickApply);
        if (btnReset) btnReset.onClick.AddListener(OnClickReset);
        if (btnClose) btnClose.onClick.AddListener(Close);

        // Bắt đầu đóng
        if (settingsPanel) settingsPanel.SetActive(false);
        ShowTab(0);
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
        _isOpen = true;
        if (settingsPanel) settingsPanel.SetActive(true);
        RefreshUI();
        // Dừng thời gian khi mở settings (tuỳ chọn)
        // Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        _isOpen = false;
        if (settingsPanel) settingsPanel.SetActive(false);
        // Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Toggle()
    {
        if (_isOpen) Close(); else Open();
    }

    // ──────────────────────────────────────
    //   Tabs
    // ──────────────────────────────────────
    void ShowTab(int index)
    {
        if (panelAudio)    panelAudio.SetActive(index == 0);
        if (panelGraphics) panelGraphics.SetActive(index == 1);
        if (panelControls) panelControls.SetActive(index == 2);

        // Tab button highlight
        SetTabHighlight(tabAudioBtn,    index == 0);
        SetTabHighlight(tabGraphicsBtn, index == 1);
        SetTabHighlight(tabControlsBtn, index == 2);
    }

    void SetTabHighlight(Button btn, bool active)
    {
        if (btn == null) return;
        var colors = btn.colors;
        colors.normalColor = active ? new Color(0.25f, 0.65f, 0.35f) : new Color(0.2f, 0.2f, 0.2f, 0.9f);
        btn.colors = colors;
    }

    // ──────────────────────────────────────
    //   Refresh UI từ SettingsManager
    // ──────────────────────────────────────
    void RefreshUI()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        if (sliderMaster)  { sliderMaster.value  = sm.masterVolume;   UpdateLabel(labelMaster, sm.masterVolume); }
        if (sliderMusic)   { sliderMusic.value   = sm.musicVolume;    UpdateLabel(labelMusic, sm.musicVolume); }
        if (sliderSFX)     { sliderSFX.value     = sm.sfxVolume;      UpdateLabel(labelSFX, sm.sfxVolume); }

        if (dropdownQuality)    dropdownQuality.value    = sm.qualityLevel;
        if (dropdownResolution) dropdownResolution.value = sm.resolutionIdx;
        if (toggleFullscreen)   toggleFullscreen.isOn    = sm.fullscreen;

        if (sliderSensitivity) { sliderSensitivity.value = sm.mouseSensitivity; UpdateLabel(labelSensitivity, sm.mouseSensitivity, "x"); }
        if (toggleInvertY)     toggleInvertY.isOn        = sm.invertYAxis;
    }

    // ──────────────────────────────────────
    //   Apply / Reset
    // ──────────────────────────────────────
    void OnClickApply()
    {
        var sm = SettingsManager.Instance;
        if (sm == null) return;

        sm.masterVolume      = sliderMaster  ? sliderMaster.value  : sm.masterVolume;
        sm.musicVolume       = sliderMusic   ? sliderMusic.value   : sm.musicVolume;
        sm.sfxVolume         = sliderSFX     ? sliderSFX.value     : sm.sfxVolume;

        sm.qualityLevel      = dropdownQuality    ? dropdownQuality.value    : sm.qualityLevel;
        sm.resolutionIdx     = dropdownResolution ? dropdownResolution.value : sm.resolutionIdx;
        sm.fullscreen        = toggleFullscreen   ? toggleFullscreen.isOn    : sm.fullscreen;

        sm.mouseSensitivity  = sliderSensitivity ? sliderSensitivity.value : sm.mouseSensitivity;
        sm.invertYAxis       = toggleInvertY     ? toggleInvertY.isOn      : sm.invertYAxis;

        sm.SaveSettings();
        sm.ApplyAll();

        StartCoroutine(ShowFeedback(btnApply, "✓ Đã lưu!"));
    }

    void OnClickReset()
    {
        if (SettingsManager.Instance) SettingsManager.Instance.ResetToDefault();
        RefreshUI();
        StartCoroutine(ShowFeedback(btnReset, "✓ Đặt lại xong!"));
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
    //   Helpers
    // ──────────────────────────────────────
    void UpdateLabel(TextMeshProUGUI label, float value, string suffix = "%")
    {
        if (label == null) return;
        if (suffix == "%")
            label.text = Mathf.RoundToInt(value * 100) + "%";
        else
            label.text = value.ToString("F1") + suffix;
    }

    void SetupResolutionDropdown()
    {
        if (dropdownResolution == null) return;
        dropdownResolution.ClearOptions();
        var resolutions = Screen.resolutions;
        var options = new System.Collections.Generic.List<string>();
        foreach (var r in resolutions)
            options.Add($"{r.width} x {r.height} @ {r.refreshRateRatio.numerator}Hz");
        dropdownResolution.AddOptions(options);

        int idx = SettingsManager.Instance ? SettingsManager.Instance.resolutionIdx : resolutions.Length - 1;
        dropdownResolution.value = Mathf.Clamp(idx, 0, options.Count - 1);
        dropdownResolution.RefreshShownValue();
    }

    void SetupQualityDropdown()
    {
        if (dropdownQuality == null) return;
        dropdownQuality.ClearOptions();
        var names = new System.Collections.Generic.List<string>
            { "🔻 Thấp", "🔸 Trung bình", "🔹 Cao", "⭐ Rất cao (Ultra)" };
        dropdownQuality.AddOptions(names);
        dropdownQuality.value = SettingsManager.Instance ? SettingsManager.Instance.qualityLevel : 2;
        dropdownQuality.RefreshShownValue();
    }
}
