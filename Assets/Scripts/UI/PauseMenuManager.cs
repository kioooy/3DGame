using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PauseMenuManager : MonoBehaviour
{
    public static PauseMenuManager Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    private GUIStyle _panelStyle;
    private GUIStyle _titleStyle;
    private GUIStyle _buttonStyle;

    private CursorLockMode _previousLockMode;
    private bool _previousCursorVisible;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        Update_StatusTimer();

        // Không thao tác nếu đang xem Tutorial
        if (Time.timeScale == 0f && !IsPaused) return;

        bool escPressed = false;
        var kb = Keyboard.current;
        if (kb != null && kb.escapeKey.wasPressedThisFrame) escPressed = true;

        if (escPressed)
        {
            // Kiểm tra xem Bảng Settings có đang bật không
            bool isSettingsOpen = false;
            if (SettingsUI.Instance != null && SettingsUI.Instance.settingsPanel != null)
            {
                isSettingsOpen = SettingsUI.Instance.settingsPanel.activeSelf;
            }

            if (!isSettingsOpen)
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        IsPaused = !IsPaused;

        if (IsPaused)
        {
            Time.timeScale = 0f;
            _previousLockMode = Cursor.lockState;
            _previousCursorVisible = Cursor.visible;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (UIAudioFeedback.Instance != null) UIAudioFeedback.Play(UIAudioFeedback.SoundType.Hover);
        }
        else
        {
            Time.timeScale = 1f;

            // Chỉ khóa lại nếu không mở Inventory và không mở Emote
            bool shouldLock = true;
            if (InventoryUI.Instance != null && InventoryUI.Instance.IsOpen) shouldLock = false;
            if (EmoteUIManager.Instance != null && EmoteUIManager.IsEmoteMenuOpen) shouldLock = false;

            if (shouldLock)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = _previousLockMode;
                Cursor.visible = _previousCursorVisible;
            }
            
            if (UIAudioFeedback.Instance != null) UIAudioFeedback.Play(UIAudioFeedback.SoundType.Hover);
        }
    }

    void OnGUI()
    {
        if (!IsPaused) return;
        
        // Ẩn Menu Pause nếu đang vào trong SettingsUI
        if (SettingsUI.Instance != null && SettingsUI.Instance.settingsPanel != null && SettingsUI.Instance.settingsPanel.activeSelf)
        {
            return;
        }

        InitStyles();

        float width  = 300f;
        float height = 540f; // Tăng thêm 90px cho 2 nút mới
        float x = (Screen.width - width) / 2f;
        float y = (Screen.height - height) / 2f;

        Rect panelRect = new Rect(x, y, width, height);

        GUI.Box(panelRect, "", _panelStyle);

        GUILayout.BeginArea(panelRect);
        
        GUILayout.Space(20);
        GUILayout.Label("TẠM DỪNG", _titleStyle);
        GUILayout.Space(20);

        // Thông báo Save/Load (hiển thị tạm thời)
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            GUILayout.Label(_statusMessage, _statusStyle);
            GUILayout.Space(8);
        }
        else
        {
            GUILayout.Space(26); // Giữ layout ổn định khi không có thông báo
        }

        DrawButton("TIẾP TỤC",    () => { TogglePause(); });
        DrawButton("LƯU GAME",    OnSaveClicked);
        DrawButton("TẢI GAME",    OnLoadClicked);
        DrawButton("HƯỚNG DẪN",  OnTutorialClicked);
        DrawButton("CÀI ĐẶT",    () => { if (SettingsUI.Instance != null) SettingsUI.Instance.Open(); });
        
        GUILayout.FlexibleSpace();
        DrawButton("THOÁT",       QuitGame);

        GUILayout.Space(20);
        GUILayout.EndArea();
    }

    // ── Handlers ──────────────────────────────────────
    private string _statusMessage = "";
    private float  _statusTimer   = 0f;
    private GUIStyle _statusStyle;

    private void SetStatus(string msg, float duration = 2.5f)
    {
        _statusMessage = msg;
        _statusTimer   = duration;
    }

    private void Update_StatusTimer()
    {
        if (_statusTimer > 0f)
        {
            _statusTimer -= Time.unscaledDeltaTime;
            if (_statusTimer <= 0f) _statusMessage = "";
        }
    }

    private void OnSaveClicked()
    {
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame();
            SetStatus("✅ Đã lưu game!");
        }
        else
        {
            SetStatus("⚠ SaveLoadManager chưa có trong Scene!");
        }
    }

    private void OnLoadClicked()
    {
        if (SaveLoadManager.Instance != null)
        {
            bool ok = SaveLoadManager.Instance.LoadGame();
            SetStatus(ok ? "✅ Đã tải game!" : "⚠ Chưa có dữ liệu lưu!");
            if (ok) TogglePause();
        }
        else
        {
            SetStatus("⚠ SaveLoadManager chưa có trong Scene!");
        }
    }

    private void OnTutorialClicked()
    {
        if (TutorialManager.Instance != null)
        {
            TogglePause(); // Đóng Pause Menu trước
            TutorialManager.Instance.Show();
        }
    }


    private void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void DrawButton(string text, System.Action onClick)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(text, _buttonStyle, GUILayout.Width(200), GUILayout.Height(45)))
        {
            onClick?.Invoke();
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.Space(15);
    }

    private void InitStyles()
    {
        if (_panelStyle == null)
        {
            _panelStyle = new GUIStyle(GUI.skin.box);
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.1f, 0.9f));
            tex.Apply();
            _panelStyle.normal.background = tex;
        }

        if (_titleStyle == null)
        {
            _titleStyle = new GUIStyle(GUI.skin.label);
            _titleStyle.fontSize = 28;
            _titleStyle.fontStyle = FontStyle.Bold;
            _titleStyle.normal.textColor = Color.white;
            _titleStyle.alignment = TextAnchor.MiddleCenter;
        }

        if (_buttonStyle == null)
        {
            _buttonStyle = new GUIStyle(GUI.skin.button);
            _buttonStyle.fontSize = 18;
            _buttonStyle.fontStyle = FontStyle.Bold;
            _buttonStyle.normal.textColor = Color.white;
            
            Texture2D btnTex = new Texture2D(1, 1);
            btnTex.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.35f, 1f));
            btnTex.Apply();
            _buttonStyle.normal.background = btnTex;

            Texture2D hoverTex = new Texture2D(1, 1);
            hoverTex.SetPixel(0, 0, new Color(0.4f, 0.6f, 0.4f, 1f));
            hoverTex.Apply();
            _buttonStyle.hover.background = hoverTex;
        }

        if (_statusStyle == null)
        {
            _statusStyle = new GUIStyle(GUI.skin.label);
            _statusStyle.fontSize = 14;
            _statusStyle.fontStyle = FontStyle.Bold;
            _statusStyle.normal.textColor = new Color(0.3f, 1f, 0.4f);
            _statusStyle.alignment = TextAnchor.MiddleCenter;
        }
    }
}
