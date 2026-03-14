using UnityEngine;

/// <summary>
/// Hiển thị bảng hướng dẫn nhỏ ở góc trên bên phải màn hình.
/// Tự động biến mất sau khoảng thời gian cài đặt.
/// Có thể mở lại từ Settings bằng cách gọi TutorialManager.Show().
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Cấu hình hiển thị")]
    [Tooltip("Thời gian tự động ẩn bảng hướng dẫn (giây)")]
    public float displayDuration = 12f;

    [Tooltip("Thời gian hiệu ứng Fade in/out (giây)")]
    public float fadeDuration = 0.6f;

    // Trạng thái nội bộ
    private bool _isVisible = false;
    private float _alpha = 0f;
    private float _visibleTimer = 0f;
    private bool _fadingIn = false;
    private bool _fadingOut = false;

    // Giao diện OnGUI
    private GUIStyle _panelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _entryStyle;
    private GUIStyle _closeStyle;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        // Hiển thị lần đầu vào game nếu chưa xem
        if (PlayerPrefs.GetInt("HasSeenTutorial", 0) == 0)
        {
            Show();
            PlayerPrefs.SetInt("HasSeenTutorial", 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Gọi hàm này từ SettingsUI để mở lại hướng dẫn
    /// </summary>
    public void Show()
    {
        _isVisible = true;
        _fadingIn = true;
        _fadingOut = false;
        _alpha = 0f;
        _visibleTimer = displayDuration;
    }

    public void Hide()
    {
        if (_isVisible)
        {
            _fadingOut = true;
            _fadingIn = false;
        }
    }

    void Update()
    {
        if (!_isVisible) return;

        // Fade In
        if (_fadingIn)
        {
            _alpha += Time.unscaledDeltaTime / fadeDuration;
            if (_alpha >= 1f)
            {
                _alpha = 1f;
                _fadingIn = false;
            }
        }
        // Đang hiển thị đầy đủ -> đếm ngược rồi tự hide
        else if (!_fadingOut)
        {
            _visibleTimer -= Time.unscaledDeltaTime;
            if (_visibleTimer <= 0f)
            {
                _fadingOut = true;
            }
        }

        // Fade Out
        if (_fadingOut)
        {
            _alpha -= Time.unscaledDeltaTime / fadeDuration;
            if (_alpha <= 0f)
            {
                _alpha = 0f;
                _fadingOut = false;
                _isVisible = false;
            }
        }
    }

    void OnGUI()
    {
        if (!_isVisible || _alpha <= 0f) return;
        InitStyles();

        float panelWidth  = 320f;
        float panelHeight = 285f;
        float margin      = 16f;
        float x = Screen.width - panelWidth - margin;
        float y = margin;

        // Áp dụng độ mờ (alpha) toàn bộ khung
        Color prev = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, _alpha);

        Rect panelRect = new Rect(x, y, panelWidth, panelHeight);
        GUI.Box(panelRect, "", _panelStyle);

        GUILayout.BeginArea(panelRect);
        GUILayout.Space(10);

        GUILayout.BeginHorizontal();
        GUILayout.Label("📖 HƯỚNG DẪN", _titleStyle);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("✕", _closeStyle, GUILayout.Width(28), GUILayout.Height(28)))
        {
            Hide();
        }
        GUILayout.Space(8);
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        GUILayout.Label("• W/A/S/D — Di chuyển nhân vật",        _entryStyle);
        GUILayout.Label("• Chuột — Xoay Camera góc nhìn",         _entryStyle);
        GUILayout.Label("• Space — Nhảy",                         _entryStyle);
        GUILayout.Label("• F — Tương tác / Nói chuyện NPC",       _entryStyle);
        GUILayout.Label("• E — Nhặt đồ",                          _entryStyle);
        GUILayout.Label("• Giữ T — Vòng Cảm Xúc (Emote)",        _entryStyle);
        GUILayout.Label("• Tab — Mở Túi Đồ (Inventory)",          _entryStyle);
        GUILayout.Label("• ESC — Tạm Dừng / Cài Đặt",             _entryStyle);

        GUILayout.EndArea();

        GUI.color = prev;
    }

    private void InitStyles()
    {
        if (_panelStyle == null)
        {
            _panelStyle = new GUIStyle(GUI.skin.box);
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.08f, 0.1f, 0.13f, 0.88f));
            tex.Apply();
            _panelStyle.normal.background = tex;
            _panelStyle.border = new RectOffset(6, 6, 6, 6);
        }

        if (_titleStyle == null)
        {
            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 15;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
            _titleStyle.margin = new RectOffset(10, 0, 0, 0);
        }

        if (_entryStyle == null)
        {
            _entryStyle = new GUIStyle(GUI.skin.label);
            _entryStyle.fontSize = 13;
            _entryStyle.normal.textColor = new Color(0.92f, 0.92f, 0.92f);
            _entryStyle.margin = new RectOffset(14, 0, 1, 1);
        }

        if (_closeStyle == null)
        {
            _closeStyle = new GUIStyle(GUI.skin.button);
            _closeStyle.fontSize = 14;
            _closeStyle.fontStyle = FontStyle.Bold;
            _closeStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
        }
    }

    // Dev utility
    public static void ResetTutorial()
    {
        PlayerPrefs.DeleteKey("HasSeenTutorial");
        PlayerPrefs.Save();
    }
}
