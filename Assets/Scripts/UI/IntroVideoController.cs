using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class IntroVideoController : MonoBehaviour
{
    [Header("Video Core")]
    public VideoPlayer videoPlayer;
    public RawImage displayImage;

    [Header("UI Logic")]
    public GameObject videoCanvas;
    public Button skipButton;

    private PlayerController _playerController;

    void Awake()
    {
        // 1. Tạo tĩnh RenderTexture cho màn hình
        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.ARGB32);
        rt.Create();
        if (videoPlayer != null) videoPlayer.targetTexture = rt;
        if (displayImage != null) displayImage.texture = rt;

        // 2. Mở khóa chuột để ấn nút Skip
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Khóa PlayerController để không đi lại lung tung
        _playerController = GameObject.FindAnyObjectByType<PlayerController>();
        if (_playerController != null)
        {
            _playerController.enabled = false;
        }

        // 4. Lắng nghe sự kiện kết thúc
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoFinished;
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(SkipIntro);
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SkipIntro();
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

        // 2. Bật lại PlayerController
        if (_playerController != null)
        {
            _playerController.enabled = true;
        }

        // 3. Khôi phục lại trạng thái chuột (Khóa chuột vào tâm để bắn/điều khiển)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 4. Tiêu hủy giao diện Video luôn cho nhẹ RAM
        if (videoCanvas != null)
        {
            Destroy(videoCanvas);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
