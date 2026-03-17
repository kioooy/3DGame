using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class StoryIntroManager : MonoBehaviour
{
    [Header("Story UI Elements (Tự động tạo)")]
    public GameObject storyCanvas;
    public TextMeshProUGUI storyText;
    public Button skipButton;
    public Button nextButton;
    public Image backgroundImage;
    
    // Tự động kích hoạt khi game chạy, tương tự như BackgroundMusicManager
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void AutoInitialize()
    {
        // Tạm thời chưa muốn nó tự chạy ở mọi Scene, chúng ta sẽ gọi nó từ MainMenu.
    }
    
    [Header("Story Settings")]
    public string gameSceneName = "SampleScene";
    public float textSpeed = 0.05f;
    
    [TextArea(3, 10)]
    public string[] storyLines;
    
    [Header("Optional: Story Images")]
    public Sprite[] storyImages;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    void Start()
    {
        // Require at least one line of story
        if (storyLines == null || storyLines.Length == 0)
        {
            storyLines = new string[] { 
                "Ở một bờ đầm nhỏ, nơi cây cỏ xanh rì và dòng nước hiền hòa...", 
                "Có một chú Dế Mèn trẻ tuổi, mang trong mình sức mạnh và vẻ ngoài oai vệ.",
                "Nhưng sự kiêu ngạo đã khiến Mèn lầm lỗi, vô tình phá hỏng tổ ấm của người bạn Dế Choắt hiền lành.",
                "Hối hận vì sai lầm của mình, Mèn quyết định lên đường tìm lại Choắt để nói lời xin lỗi...",
                "...và bắt đầu một hành trình phiêu lưu kỳ thú cùng những người bạn mới."
            };
        }

        // Tự động tạo UI nếu người dùng chưa kéo thả
        if (storyCanvas == null)
        {
            CreateStoryUI();
        }

        if (skipButton != null) skipButton.onClick.AddListener(SkipStory);
        if (nextButton != null) nextButton.onClick.AddListener(OnNextButtonClicked);

        // Bắt đầu kể chuyện
        StartStory();
    }

    void CreateStoryUI()
    {
        // 1. Tạo Canvas
        GameObject canvasObj = new GameObject("StoryCanvas_Auto");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // Hiển thị trên cùng
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();
        storyCanvas = canvasObj;

        // 2. Tạo Background (Nền đen)
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        backgroundImage = bgObj.AddComponent<Image>();
        backgroundImage.color = new Color(0, 0, 0, 1f); // Đen xì
        RectTransform bgRect = backgroundImage.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 3. Tạo Text kể chuyện
        GameObject textObj = new GameObject("StoryText");
        textObj.transform.SetParent(canvasObj.transform, false);
        storyText = textObj.AddComponent<TextMeshProUGUI>();
        storyText.color = Color.white;
        storyText.fontSize = 32;
        storyText.lineSpacing = 15;
        storyText.alignment = TextAlignmentOptions.Center;
        storyText.textWrappingMode = TextWrappingModes.Normal;
        RectTransform txtRect = storyText.GetComponent<RectTransform>();
        txtRect.anchorMin = new Vector2(0.1f, 0.2f);
        txtRect.anchorMax = new Vector2(0.9f, 0.8f);
        txtRect.sizeDelta = Vector2.zero;

        // 4. Tạo Nút Skip
        GameObject skipObj = new GameObject("SkipButton");
        skipObj.transform.SetParent(canvasObj.transform, false);
        Image skipImg = skipObj.AddComponent<Image>();
        skipImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        skipButton = skipObj.AddComponent<Button>();
        RectTransform skipRect = skipObj.GetComponent<RectTransform>();
        skipRect.anchorMin = new Vector2(1, 1);
        skipRect.anchorMax = new Vector2(1, 1);
        skipRect.pivot = new Vector2(1, 1);
        skipRect.anchoredPosition = new Vector2(-20, -20);
        skipRect.sizeDelta = new Vector2(120, 50);
        
        GameObject skipTextObj = new GameObject("Text");
        skipTextObj.transform.SetParent(skipObj.transform, false);
        TextMeshProUGUI skipTxt = skipTextObj.AddComponent<TextMeshProUGUI>();
        skipTxt.text = "Bỏ qua";
        skipTxt.color = Color.white;
        skipTxt.fontSize = 20;
        skipTxt.alignment = TextAlignmentOptions.Center;
        RectTransform skipTxtRect = skipTxt.GetComponent<RectTransform>();
        skipTxtRect.anchorMin = Vector2.zero;
        skipTxtRect.anchorMax = Vector2.one;
        skipTxtRect.sizeDelta = Vector2.zero;

        // 5. Tạo Nút Next
        GameObject nextObj = new GameObject("NextButton");
        nextObj.transform.SetParent(canvasObj.transform, false);
        Image nextImg = nextObj.AddComponent<Image>();
        nextImg.color = new Color(0.22f, 0.75f, 0.97f, 1f); // Màu xanh accent #38BDF8
        nextButton = nextObj.AddComponent<Button>();
        RectTransform nextRect = nextObj.GetComponent<RectTransform>();
        nextRect.anchorMin = new Vector2(0.5f, 0);
        nextRect.anchorMax = new Vector2(0.5f, 0);
        nextRect.pivot = new Vector2(0.5f, 0);
        nextRect.anchoredPosition = new Vector2(0, 50);
        nextRect.sizeDelta = new Vector2(240, 60);

        GameObject nextTextObj = new GameObject("Text");
        nextTextObj.transform.SetParent(nextObj.transform, false);
        TextMeshProUGUI nextTxt = nextTextObj.AddComponent<TextMeshProUGUI>();
        nextTxt.text = "Tiếp Theo >>";
        nextTxt.color = new Color(0.05f, 0.05f, 0.1f); // Chữ tối trên nền sáng
        nextTxt.fontSize = 24;
        nextTxt.fontStyle = FontStyles.Bold;
        nextTxt.alignment = TextAlignmentOptions.Center;
        RectTransform nextTxtRect = nextTxt.GetComponent<RectTransform>();
        nextTxtRect.anchorMin = Vector2.zero;
        nextTxtRect.anchorMax = Vector2.one;
        nextTxtRect.sizeDelta = Vector2.zero;
        
        // Cần đảm bảo EventSystem tồn tại để bấm được nút
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    public void StartStory()
    {
        if (storyCanvas != null) storyCanvas.SetActive(true);
        currentLineIndex = 0;
        
        ShowCurrentLine();
    }

    void ShowCurrentLine()
    {
        if (currentLineIndex >= storyLines.Length)
        {
            FinishStory();
            return;
        }

        // Đổi hình nền nếu có mảng hình và index hợp lệ
        if (backgroundImage != null && storyImages != null && currentLineIndex < storyImages.Length && storyImages[currentLineIndex] != null)
        {
            backgroundImage.sprite = storyImages[currentLineIndex];
        }

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeText(storyLines[currentLineIndex]));
    }

    IEnumerator TypeText(string line)
    {
        isTyping = true;
        storyText.text = "";
        
        // Hiện nút Next ẩn đi tạm thời khi đang gõ
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        foreach (char c in line.ToCharArray())
        {
            storyText.text += c;
            yield return new WaitForSeconds(textSpeed);
        }

        isTyping = false;
        
        // Hiện nút Next (hoặc đổi chữ thành Play nếu là dòng cuối)
        if (nextButton != null) 
        {
            nextButton.gameObject.SetActive(true);
            var btnText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = (currentLineIndex == storyLines.Length - 1) ? "Bắt Đầu" : "Tiếp Theo >>";
            }
        }
    }

    // Xử lý khi bấm nút Next (hoặc click chuột vào màn hình)
    public void OnNextButtonClicked()
    {
        if (isTyping)
        {
            // Bấm lần 1: Bỏ qua hiệu ứng gõ chữ, hiện full luôn
            if (typingCoroutine != null) StopCoroutine(typingCoroutine);
            storyText.text = storyLines[currentLineIndex];
            isTyping = false;
            
            if (nextButton != null) 
            {
                nextButton.gameObject.SetActive(true);
                var btnText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = (currentLineIndex == storyLines.Length - 1) ? "Bắt Đầu" : "Tiếp Theo >>";
            }
        }
        else
        {
            // Bấm lần 2: Qua dòng tiếp theo
            currentLineIndex++;
            ShowCurrentLine();
        }
    }

    void Update()
    {
        // Cho phép bấm Space hoặc Click chuột để Next
        var kb = UnityEngine.InputSystem.Keyboard.current;
        var mouse = UnityEngine.InputSystem.Mouse.current;

        bool skipPressed = (kb != null && kb.spaceKey.wasPressedThisFrame) || 
                           (mouse != null && mouse.leftButton.wasPressedThisFrame);

        if (skipPressed)
        {
            // Tránh việc nhấn trúng UI button bị gọi 2 lần
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            OnNextButtonClicked();
        }
    }

    public void SkipStory()
    {
        FinishStory();
    }

    void FinishStory()
    {
        if (storyCanvas != null) storyCanvas.SetActive(false);
        Debug.Log("Loading Scene: " + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }
}
