using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Phát một video intro toàn màn hình. Khi video kết thúc (hoặc người chơi bỏ qua),
/// sẽ load sang gameSceneName. Dùng ở MainMenu – không cần PlayerController.
/// </summary>
public class IntroVideoController : MonoBehaviour
{
    [Header("Video Core")]
    public VideoPlayer videoPlayer;
    public RawImage displayImage;

    [Header("UI Logic")]
    public GameObject videoCanvas;
    public Button skipButton;

    [Header("Scene To Load")]
    /// <summary>Tên scene game cần load sau khi video kết thúc.</summary>
    public string gameSceneName = "StylizedNatureLite_Demo";

    void Start()
    {
        // 1. Ẩn con trỏ (KHÔNG lock để nút Skip vẫn click được)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = false;

        // 2. Lắng nghe sự kiện kết thúc video
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipIntro);
        }

        // 3. Chuẩn bị xong rồi mới Play (tránh màn hình trắng)
        // NOTE: phải dùng Start() thay vì Awake() vì MainMenuManager
        // gán videoPlayer SAU khi AddComponent<IntroVideoController>() trả về.
        // Awake() chạy TRONG lúc AddComponent → videoPlayer vẫn null.
        StartCoroutine(PrepareAndPlay());
    }

    System.Collections.IEnumerator PrepareAndPlay()
    {
        if (videoPlayer == null) yield break;

        // Tắt playOnAwake để tránh phát trước khi RT sẵn sàng
        videoPlayer.playOnAwake = false;
        videoPlayer.Stop();

        // Xóa RenderTexture cũ nếu có
        if (displayImage != null && displayImage.texture is RenderTexture oldRt)
        {
            displayImage.texture = null;
            videoPlayer.targetTexture = null;
            oldRt.Release();
            Destroy(oldRt);
        }

        // Chuẩn bị video (resolve kích thước thực)
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;

        // Tạo RT với đúng kích thước của video
        uint w = videoPlayer.width  > 0 ? videoPlayer.width  : (uint)Screen.width;
        uint h = videoPlayer.height > 0 ? videoPlayer.height : (uint)Screen.height;
        RenderTexture rt = new RenderTexture((int)w, (int)h, 0, RenderTextureFormat.ARGB32);
        rt.Create();

        videoPlayer.targetTexture = rt;
        if (displayImage != null) displayImage.texture = rt;

        // Bắt đầu phát
        videoPlayer.Play();
    }

    void Update()
    {
        if (Keyboard.current != null)
        {
            // Giữ Alt → hiện con trỏ (dùng None chứ không dùng Locked để click vẫn hoạt động)
            bool altHeld = Keyboard.current.leftAltKey.isPressed || Keyboard.current.rightAltKey.isPressed;
            Cursor.visible = altHeld;
            Cursor.lockState = CursorLockMode.None; // luôn None để UI nhận click

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                SkipIntro();
            }
        }
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        FinishIntro();
    }

    public void SkipIntro()
    {
        FinishIntro();
    }

    private void FinishIntro()
    {
        // 1. Giải phóng Video
        if (videoPlayer != null)
        {
            videoPlayer.Stop();
            videoPlayer.loopPointReached -= OnVideoFinished;
        }

        // Giải phóng Render Texture chống tràn RAM
        if (displayImage != null && displayImage.texture != null)
        {
            var oldRT = displayImage.texture as RenderTexture;
            displayImage.texture = null;
            if (videoPlayer != null) videoPlayer.targetTexture = null;

            if (oldRT != null)
            {
                oldRT.Release();
                Destroy(oldRT);
            }
        }

        // 2. Tiêu hủy giao diện Video
        if (videoCanvas != null)
        {
            Destroy(videoCanvas);
        }
        else
        {
            Destroy(gameObject);
        }

        // 3. Load scene game 
        Debug.Log($"[IntroVideo] Video kết thúc → Load scene: {gameSceneName}");
        SceneManager.LoadScene(gameSceneName);
    }
}

