using UnityEngine;

/// <summary>
/// Camera dành riêng cho Minimap.
/// Sẽ bám theo x và z của Target (người chơi) để vẽ bản đồ từ trên cao xuống.
/// </summary>
public class MinimapCamera : MonoBehaviour
{
    [Tooltip("Mục tiêu cần theo dõi (thường là Player)")]
    public Transform target;

    [Tooltip("Độ cao của camera so với mục tiêu")]
    public float height = 50f;

    [Tooltip("Có xoay camera theo hướng nhìn của mục tiêu không?")]
    public bool rotateWithTarget = false;

    [Tooltip("Phím tắt để bật/tắt Minimap")]
    public KeyCode toggleKey = KeyCode.M;

    [Tooltip("GameObject UI của Minimap")]
    public GameObject minimapUI;

    // Optional: Nếu target chưa gán, tự động tìm Player bằng Tag
    void Start()
    {
        if (target == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }
        
        // Cố gắng tự tìm UI Minimap nếu chưa gán
        if (minimapUI == null)
        {
            var ui = GameObject.Find("MinimapUI");
            if (ui != null) minimapUI = ui;
        }

        // --- Culling Mask Setup ---
        Camera mainCam = Camera.main;
        Camera miniCam = GetComponent<Camera>();

        int minimapLayer = LayerMask.NameToLayer("MinimapIcon");
        if (minimapLayer == -1) minimapLayer = 8; // fallback to Layer 8

        if (mainCam != null)
        {
            // Ẩn layer MinimapIcon khỏi Main Camera
            mainCam.cullingMask &= ~(1 << minimapLayer);
        }
        
        if (miniCam != null)
        {
            // Hiển thị layer MinimapIcon trên Minimap Camera
            miniCam.cullingMask |= (1 << minimapLayer);
        }
    }

    void Update()
    {
        // Toggle Minimap bằng phím tắt
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.mKey.wasPressedThisFrame)
        {
            ToggleMinimap();
        }
#else
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleMinimap();
        }
#endif
    }

    void ToggleMinimap()
    {
        if (minimapUI != null)
        {
            minimapUI.SetActive(!minimapUI.activeSelf);
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Bám theo toạ độ X, Z của target. Giữ nguyên độ cao Y (hoặc target.Y + height).
        Vector3 newPosition = target.position;
        newPosition.y = height; // Hoặc newPosition.y += height; tuỳ vào địa hình
        transform.position = newPosition;

        // Nếu muốn map xoay theo người chơi
        if (rotateWithTarget)
        {
            // Chỉ lấy góc xoay trục Y của target
            transform.rotation = Quaternion.Euler(90f, target.eulerAngles.y, 0f);
        }
        else
        {
            // Cố định hướng Bắc (Z+) lên trên
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        }
    }
}
