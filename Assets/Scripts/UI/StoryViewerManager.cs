using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StoryViewerManager : MonoBehaviour
{
    public static StoryViewerManager Instance;

    [Header("UI References")]
    public GameObject storyPanel;
    public Image storyImage;
    public Button prevButton;
    public Button nextButton;
    public Button closeButton;
    public TextMeshProUGUI pageText;

    private Sprite[] currentStoryImages;
    private int currentIndex = 0;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        if (storyPanel != null)
            storyPanel.SetActive(false);

        if (prevButton != null) prevButton.onClick.AddListener(ShowPreviousPage);
        if (nextButton != null) nextButton.onClick.AddListener(ShowNextPage);
        if (closeButton != null) closeButton.onClick.AddListener(CloseStory);
    }

    /// <summary>
    /// Hiển thị bộ truyện
    /// </summary>
    public void ShowStory(Sprite[] images)
    {
        if (images == null || images.Length == 0)
        {
            Debug.LogWarning("StoryViewerManager: Không có hình ảnh truyện nào để hiển thị.");
            return;
        }

        currentStoryImages = images;
        currentIndex = 0;
        
        UpdateUI();

        if (storyPanel != null)
            storyPanel.SetActive(true);

        // Khoá chuột để bấm UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseStory()
    {
        if (storyPanel != null)
            storyPanel.SetActive(false);

        // Khóa lại chuột nếu DialogManager/Quest không yêu cầu mở
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Cần đảm bảo BackgroundMusicManager tiếp tục phát nhạc nếu có
        if (BackgroundMusicManager.Instance != null)
            BackgroundMusicManager.Instance.ResumeMusic();
    }

    private void ShowPreviousPage()
    {
        if (currentIndex > 0)
        {
            currentIndex--;
            UpdateUI();
        }
    }

    private void ShowNextPage()
    {
        if (currentStoryImages != null && currentIndex < currentStoryImages.Length - 1)
        {
            currentIndex++;
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (currentStoryImages == null || currentStoryImages.Length == 0) return;

        if (storyImage != null)
            storyImage.sprite = currentStoryImages[currentIndex];

        if (pageText != null)
            pageText.text = $"{currentIndex + 1} / {currentStoryImages.Length}";

        if (prevButton != null)
            prevButton.interactable = (currentIndex > 0);

        if (nextButton != null)
            nextButton.interactable = (currentIndex < currentStoryImages.Length - 1);
    }
}
