using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý logic cho màn hình Main Menu độc lập.
/// Nhấn Play sẽ load Scene chứa Game chính.
/// Nhấn Settings sẽ mở bảng Cài Đặt có sẵn.
/// Nhấn Exit sẽ thoát game.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Menu UI Root")]
    public GameObject mainMenuCanvas;

    [Header("Scene Transition")]
    public string gameSceneName = "StylizedNatureLite_Demo";

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
    /// Bắt đầu game bằng cách hiện cốt truyện (Story sẽ tự load Scene sau khi xong)
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

        // Tạo ra hệ thống Cốt truyện
        Debug.Log("Đang hiển thị cốt truyện...");
        GameObject storyManagerObj = new GameObject("StoryIntroManager_Auto");
        StoryIntroManager storyManager = storyManagerObj.AddComponent<StoryIntroManager>();
        
        // Truyền tên Scene cần load sang cho Story Manager
        storyManager.gameSceneName = this.gameSceneName;
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
