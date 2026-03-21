using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton quản lý cài đặt game – âm thanh, đồ hoạ, điều khiển.
/// Tự động lưu vào PlayerPrefs và load lại khi khởi động.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private static SettingsManager _instance;
    public static SettingsManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<SettingsManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("SettingsManager [Auto-Created]");
                    _instance = go.AddComponent<SettingsManager>();
                }
            }
            return _instance;
        }
        private set { _instance = value; }
    }

    [Header("Audio Mixer (tuỳ chọn)")]
    public AudioMixer audioMixer;

    // ──────────────────────────────────────
    //   Giá trị cài đặt hiện tại
    // ──────────────────────────────────────
    [HideInInspector] public float masterVolume   = 1f;
    [HideInInspector] public float musicVolume    = 0.8f;
    [HideInInspector] public float sfxVolume      = 1f;
    [HideInInspector] public float playerVolume   = 1f;

    [HideInInspector] public int   qualityLevel   = 2;   // 0=Thấp 1=TB 2=Cao 3=Rất cao
    [HideInInspector] public bool  fullscreen     = true;
    [HideInInspector] public int   resolutionIdx  = 0;   // index trong Screen.resolutions

    [HideInInspector] public float mouseSensitivity    = 2f;
    [HideInInspector] public bool  invertYAxis          = false;

    // ── Hieu ung con tro chuot ──
    [HideInInspector] public bool  cursorTrailEnabled   = false; // hieu ung duoi chuot
    [HideInInspector] public float cursorSize           = 1f;    // he so phong to: 0.5 - 2.0
    [HideInInspector] public bool  smoothMouse          = false; // lam muot chuyen dong chuot
    [HideInInspector] public int   cursorStyle          = 0;     // 0=Mac dinh 1=Chinhx 2=Vong tron
    [HideInInspector] public bool  cursorHighlight      = false; // vong sang xung quanh chuot

    // ──────────────────────────────────────
    //   Keys PlayerPrefs
    // ──────────────────────────────────────
    const string K_MASTER       = "s_masterVol";
    const string K_MUSIC        = "s_musicVol";
    const string K_SFX          = "s_sfxVol";
    const string K_PLAYER       = "s_playerVol";
    const string K_QUALITY      = "s_quality";
    const string K_FULLSCR      = "s_fullscreen";
    const string K_RESOL        = "s_resolution";
    const string K_SENSI        = "s_sensitivity";
    const string K_INVERTY      = "s_invertY";
    const string K_CURSOR_TRAIL = "s_cursorTrail";
    const string K_CURSOR_SIZE  = "s_cursorSize";
    const string K_SMOOTH_MOUSE = "s_smoothMouse";
    const string K_CURSOR_STYLE = "s_cursorStyle";
    const string K_CURSOR_HL    = "s_cursorHL";

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
        ApplyAll();
    }

    // ──────────────────────────────────────
    //   Load / Save
    // ──────────────────────────────────────
    public void LoadSettings()
    {
        masterVolume      = PlayerPrefs.GetFloat(K_MASTER,  1f);
        musicVolume       = PlayerPrefs.GetFloat(K_MUSIC,  0.8f);
        sfxVolume         = PlayerPrefs.GetFloat(K_SFX,    1f);
        playerVolume      = PlayerPrefs.GetFloat(K_PLAYER, 1f);
        qualityLevel      = PlayerPrefs.GetInt  (K_QUALITY, 2);
        fullscreen        = PlayerPrefs.GetInt  (K_FULLSCR, 1) == 1;
        mouseSensitivity  = PlayerPrefs.GetFloat(K_SENSI,  2f);
        invertYAxis       = PlayerPrefs.GetInt  (K_INVERTY, 0) == 1;
        cursorTrailEnabled = PlayerPrefs.GetInt (K_CURSOR_TRAIL, 0) == 1;
        cursorSize         = PlayerPrefs.GetFloat(K_CURSOR_SIZE, 1f);
        smoothMouse        = PlayerPrefs.GetInt (K_SMOOTH_MOUSE, 0) == 1;
        cursorStyle        = PlayerPrefs.GetInt (K_CURSOR_STYLE, 0);
        cursorHighlight    = PlayerPrefs.GetInt (K_CURSOR_HL, 0) == 1;

        int maxRes = Screen.resolutions.Length - 1;
        resolutionIdx = Mathf.Clamp(PlayerPrefs.GetInt(K_RESOL, maxRes), 0, maxRes);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(K_MASTER,  masterVolume);
        PlayerPrefs.SetFloat(K_MUSIC,   musicVolume);
        PlayerPrefs.SetFloat(K_SFX,     sfxVolume);
        PlayerPrefs.SetFloat(K_PLAYER,  playerVolume);
        PlayerPrefs.SetInt  (K_QUALITY, qualityLevel);
        PlayerPrefs.SetInt  (K_FULLSCR, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt  (K_RESOL,   resolutionIdx);
        PlayerPrefs.SetFloat(K_SENSI,   mouseSensitivity);
        PlayerPrefs.SetInt  (K_INVERTY,  invertYAxis       ? 1 : 0);
        PlayerPrefs.SetInt  (K_CURSOR_TRAIL, cursorTrailEnabled ? 1 : 0);
        PlayerPrefs.SetFloat(K_CURSOR_SIZE,  cursorSize);
        PlayerPrefs.SetInt  (K_SMOOTH_MOUSE, smoothMouse   ? 1 : 0);
        PlayerPrefs.SetInt  (K_CURSOR_STYLE, cursorStyle);
        PlayerPrefs.SetInt  (K_CURSOR_HL,    cursorHighlight ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("[Settings] Da luu cai dat.");
    }

    public void ResetToDefault()
    {
        masterVolume       = 1f;
        musicVolume        = 0.8f;
        sfxVolume          = 1f;
        playerVolume       = 1f;
        qualityLevel       = 2;
        fullscreen         = true;
        resolutionIdx      = Screen.resolutions.Length - 1;
        mouseSensitivity   = 2f;
        invertYAxis        = false;
        cursorTrailEnabled = false;
        cursorSize         = 1f;
        smoothMouse        = false;
        cursorStyle        = 0;
        cursorHighlight    = false;
        SaveSettings();
        ApplyAll();
        Debug.Log("[Settings] Da dat lai mac dinh.");
    }

    // ──────────────────────────────────────
    //   Apply
    // ──────────────────────────────────────
    public void ApplyAll()
    {
        ApplyAudio();
        ApplyGraphics();
        ApplyMouse();
        ApplyCursor();
    }

    public void ApplyAudio()
    {
        masterVolume = NormalizeVolume(masterVolume);
        musicVolume = NormalizeVolume(musicVolume);
        sfxVolume = NormalizeVolume(sfxVolume);
        playerVolume = NormalizeVolume(playerVolume);

        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", VolumeToDb(masterVolume));
            audioMixer.SetFloat("MusicVolume",  VolumeToDb(musicVolume));
            audioMixer.SetFloat("SFXVolume",    VolumeToDb(sfxVolume));
            audioMixer.SetFloat("PlayerVolume", VolumeToDb(playerVolume));
        }
        
        // Đặt âm lượng của toàn môi trường game, giới hạn tuyệt đối 0 đến 1.
        AudioListener.volume = Mathf.Clamp01(masterVolume);
    }

    // Thủ thuật chống quá tải dải âm làm Engine tự tắt tiếng (Audio Blowout Break)
    private float NormalizeVolume(float vol)
    {
        if (vol > 1f) vol /= 100f; // Chống lỗi người dùng set thanh Slider 0-100 thay vì 0-1
        return Mathf.Clamp(vol, 0.0001f, 1f);
    }

    public void ApplyGraphics()
    {
        QualitySettings.SetQualityLevel(qualityLevel, true);
        var resolutions = Screen.resolutions;
        if (resolutionIdx >= 0 && resolutionIdx < resolutions.Length)
        {
            var res = resolutions[resolutionIdx];
            Screen.SetResolution(res.width, res.height, fullscreen);
        }
        else
        {
            Screen.fullScreen = fullscreen;
        }
    }

    public void ApplyMouse()
    {
        // Dong bo voi ThirdPersonCamera neu co
        var cam = FindFirstObjectByType<ThirdPersonCamera>();
        if (cam != null)
        {
            var field = typeof(ThirdPersonCamera).GetField("mouseSensitivity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(cam, mouseSensitivity);
        }
    }

    public void ApplyCursor()
    {
        // Tim CursorEffectController trong scene neu co
        var ctl = FindFirstObjectByType<CursorEffectController>();
        if (ctl == null) return;
        ctl.SetTrailEnabled(cursorTrailEnabled);
        ctl.SetSize(cursorSize);
        ctl.SetHighlight(cursorHighlight);
        ctl.SetStyle(cursorStyle);
        ctl.SetSmooth(smoothMouse);
    }

    // ──────────────────────────────────────
    //   Helpers
    // ──────────────────────────────────────
    float VolumeToDb(float linear)
    {
        linear = Mathf.Max(linear, 0.0001f);
        return Mathf.Log10(linear) * 20f;
    }
}
