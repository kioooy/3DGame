using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

/// <summary>
/// Quản lý logic cho màn hình Main Menu độc lập.
/// Nhấn Play → phát video intro → load scene game.
/// Nhấn Settings sẽ mở bảng Cài Đặt có sẵn.
/// Nhấn Exit sẽ thoát game.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Menu UI Root")]
    public GameObject mainMenuCanvas;

    [Header("Scene Transition")]
    public string gameSceneName = "StylizedNatureLite_Demo";

    [Header("Intro Video")]
    [Tooltip("Kéo file video (.mp4) từ Project vào đây")]
    public VideoClip introVideoClip;

    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button exitButton;

    // Cờ báo Menu đang mở
    public static bool IsMenuActive = false;

    void Start()
    {
        // Add listeners cho cac nut
        if (playButton != null) playButton.onClick.AddListener(PlayGame);
        if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);

        ShowMenu();
    }

    /// <summary>
    /// Hiển thị Menu (trong Scene riêng)
    /// </summary>
    public void ShowMenu()
    {
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(true);
        }

        IsMenuActive = true;

        // Hiện con trỏ chuột
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    /// <summary>
    /// Nhấn Play: ẩn menu, phát video intro rồi load scene game
    /// </summary>
    public void PlayGame()
    {
        IsMenuActive = false;
        Time.timeScale = 1f;

        // Ẩn Menu hiện tại đi
        if (mainMenuCanvas != null)
        {
            mainMenuCanvas.SetActive(false);
        }

        // Nếu không có video clip, load thẳng scene game
        if (introVideoClip == null)
        {
            Debug.LogWarning("[MainMenu] Chưa gán introVideoClip → load scene trực tiếp.");
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        // ── Tạo Canvas toàn màn hình ──────────────────────────────────
        GameObject canvasObj = new GameObject("IntroVideoCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // ── RawImage hiển thị video ───────────────────────────────────
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        var rawImg = bgObj.AddComponent<UnityEngine.UI.RawImage>();
        rawImg.color = Color.white;
        var bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // ── VideoPlayer ───────────────────────────────────────────────
        var vp = bgObj.AddComponent<VideoPlayer>();
        vp.clip = introVideoClip;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.playOnAwake = false; // IntroVideoController.PrepareAndPlay() sẽ tự play sau khi RT sẵn sàng
        vp.isLooping = false;
        vp.audioOutputMode = VideoAudioOutputMode.Direct;

        // ── Nút Skip ─────────────────────────────────────────────────
        GameObject skipObj = new GameObject("SkipButton");
        skipObj.transform.SetParent(canvasObj.transform, false);
        skipObj.AddComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.7f);
        var skipBtn = skipObj.AddComponent<Button>();
        var skipRT = skipObj.GetComponent<RectTransform>();
        skipRT.anchorMin = new Vector2(1, 0);
        skipRT.anchorMax = new Vector2(1, 0);
        skipRT.pivot = new Vector2(1, 0);
        skipRT.anchoredPosition = new Vector2(-50, 50);
        skipRT.sizeDelta = new Vector2(250, 70);

        GameObject skipTxtObj = new GameObject("Text");
        skipTxtObj.transform.SetParent(skipObj.transform, false);
        var skipTxt = skipTxtObj.AddComponent<TMPro.TextMeshProUGUI>();
        skipTxt.text = "BỎ QUA  >>";
        skipTxt.fontSize = 20;
        skipTxt.alignment = TMPro.TextAlignmentOptions.Center;
        skipTxt.color = Color.white;
        var skipTxtRT = skipTxtObj.GetComponent<RectTransform>();
        skipTxtRT.anchorMin = Vector2.zero;
        skipTxtRT.anchorMax = Vector2.one;
        skipTxtRT.offsetMin = Vector2.zero;
        skipTxtRT.offsetMax = Vector2.zero;

        // ── IntroVideoController ──────────────────────────────────────
        var ctrl = canvasObj.AddComponent<IntroVideoController>();
        ctrl.videoCanvas = canvasObj;
        ctrl.videoPlayer = vp;
        ctrl.displayImage = rawImg;
        ctrl.skipButton = skipBtn;
        ctrl.gameSceneName = gameSceneName;

        Debug.Log("[MainMenu] Đang phát video intro...");
    }

    /// <summary>
    /// Mở bảng Settings Manager đã tồn tại
    /// </summary>
    public void OpenSettings()
    {
        if (SettingsUI.Instance != null)
        {
            SettingsUI.Instance.Open();
            Debug.Log("Đang mở bảng Cài Đặt...");
        }
        else
        {
            Debug.LogWarning("Không tìm thấy SettingsUI.Instance trong Scene!");
        }
    }

    /// <summary>
    /// Thoát khỏi ứng dụng
    /// </summary>
    public void ExitGame()
    {
        Debug.Log("Thoát Game!");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

