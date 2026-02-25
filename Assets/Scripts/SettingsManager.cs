using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Singleton quản lý cài đặt game – âm thanh, đồ hoạ, điều khiển.
/// Tự động lưu vào PlayerPrefs và load lại khi khởi động.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Audio Mixer (tuỳ chọn)")]
    public AudioMixer audioMixer;

    // ──────────────────────────────────────
    //   Giá trị cài đặt hiện tại
    // ──────────────────────────────────────
    [HideInInspector] public float masterVolume   = 1f;
    [HideInInspector] public float musicVolume    = 0.8f;
    [HideInInspector] public float sfxVolume      = 1f;

    [HideInInspector] public int   qualityLevel   = 2;   // 0=Thấp 1=TB 2=Cao 3=Rất cao
    [HideInInspector] public bool  fullscreen     = true;
    [HideInInspector] public int   resolutionIdx  = 0;   // index trong Screen.resolutions

    [HideInInspector] public float mouseSensitivity = 2f;
    [HideInInspector] public bool  invertYAxis       = false;

    // ──────────────────────────────────────
    //   Keys PlayerPrefs
    // ──────────────────────────────────────
    const string K_MASTER   = "s_masterVol";
    const string K_MUSIC    = "s_musicVol";
    const string K_SFX      = "s_sfxVol";
    const string K_QUALITY  = "s_quality";
    const string K_FULLSCR  = "s_fullscreen";
    const string K_RESOL    = "s_resolution";
    const string K_SENSI    = "s_sensitivity";
    const string K_INVERTY  = "s_invertY";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
        qualityLevel      = PlayerPrefs.GetInt  (K_QUALITY, 2);
        fullscreen        = PlayerPrefs.GetInt  (K_FULLSCR, 1) == 1;
        mouseSensitivity  = PlayerPrefs.GetFloat(K_SENSI,  2f);
        invertYAxis       = PlayerPrefs.GetInt  (K_INVERTY, 0) == 1;

        int maxRes = Screen.resolutions.Length - 1;
        resolutionIdx = Mathf.Clamp(PlayerPrefs.GetInt(K_RESOL, maxRes), 0, maxRes);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(K_MASTER,  masterVolume);
        PlayerPrefs.SetFloat(K_MUSIC,   musicVolume);
        PlayerPrefs.SetFloat(K_SFX,     sfxVolume);
        PlayerPrefs.SetInt  (K_QUALITY, qualityLevel);
        PlayerPrefs.SetInt  (K_FULLSCR, fullscreen ? 1 : 0);
        PlayerPrefs.SetInt  (K_RESOL,   resolutionIdx);
        PlayerPrefs.SetFloat(K_SENSI,   mouseSensitivity);
        PlayerPrefs.SetInt  (K_INVERTY,  invertYAxis ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log("[Settings] Đã lưu cài đặt.");
    }

    public void ResetToDefault()
    {
        masterVolume     = 1f;
        musicVolume      = 0.8f;
        sfxVolume        = 1f;
        qualityLevel     = 2;
        fullscreen       = true;
        resolutionIdx    = Screen.resolutions.Length - 1;
        mouseSensitivity = 2f;
        invertYAxis      = false;
        SaveSettings();
        ApplyAll();
        Debug.Log("[Settings] Đã đặt lại mặc định.");
    }

    // ──────────────────────────────────────
    //   Apply
    // ──────────────────────────────────────
    public void ApplyAll()
    {
        ApplyAudio();
        ApplyGraphics();
        ApplyMouse();
    }

    public void ApplyAudio()
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("MasterVolume", VolumeToDb(masterVolume));
            audioMixer.SetFloat("MusicVolume",  VolumeToDb(musicVolume));
            audioMixer.SetFloat("SFXVolume",    VolumeToDb(sfxVolume));
        }
        AudioListener.volume = masterVolume;
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
        // Đồng bộ với ThirdPersonCamera nếu có (set field private qua reflection)
        var cam = FindFirstObjectByType<ThirdPersonCamera>();
        if (cam != null)
        {
            var field = typeof(ThirdPersonCamera).GetField("mouseSensitivity",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null) field.SetValue(cam, mouseSensitivity);
        }
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
