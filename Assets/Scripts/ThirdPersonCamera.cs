using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Camera góc nhìn thứ 3 - bám theo nhân vật, đứng phía sau.
/// Gắn vào Main Camera. Gán Target = object nhân vật.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Mục tiêu")]
    [SerializeField] Transform target;               // Nhân vật cần theo dõi

    [Header("Khoảng cách camera (phong cách God of War)")]
    [SerializeField] float distance = 4f;            // Khoảng cách phía sau (gần, intimate)
    [SerializeField] float minDistance = 2f;         // Zoom in tối đa (lăn chuột lên)
    [SerializeField] float maxDistance = 12f;        // Zoom out tối đa (lăn chuột xuống)
    [SerializeField] float zoomSpeed = 2f;           // Tốc độ zoom khi lăn chuột
    [SerializeField] float height = 3.2f;            // Độ cao camera (cao hơn vai, nhìn xuống)
    [SerializeField] float lookAtHeight = 1.4f;      // Điểm camera nhìn vào (lưng/vai nhân vật)
    [SerializeField] float followSpeed = 10f;        // Tốc độ bám theo

    [Header("Xoay chuột")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float minVerticalAngle = -25f;  // Nhìn xuống tối đa
    [SerializeField] float maxVerticalAngle = 35f;   // Nhìn lên tối đa (God of War hạn chế nhìn lên)

    float _horizontalAngle;   // Góc xoay ngang (quanh nhân vật)
    float _verticalAngle;     // Góc xoay dọc (lên/xuống)
    float _currentDistance;   // Khoảng cách hiện tại (dùng cho zoom)

    void Start()
    {
        if (!MainMenuManager.IsMenuActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (target != null)
        {
            _horizontalAngle = target.eulerAngles.y;
            _verticalAngle = 12f;  // Góc nhìn xuống nhẹ (God of War style)
        }
        _currentDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Dừng xoay Camera và Zoom nếu Menu đang bật để chuột có thể bấm nút
        if (MainMenuManager.IsMenuActive) return;

        // Đọc input chuột
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            _horizontalAngle += delta.x * mouseSensitivity;
            _verticalAngle -= delta.y * mouseSensitivity;
            _verticalAngle = Mathf.Clamp(_verticalAngle, minVerticalAngle, maxVerticalAngle);

            // Lăn chuột: lên = zoom in (gần nhân vật), xuống = zoom out (xa nhân vật)
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
            {
                _currentDistance -= Mathf.Sign(scroll) * zoomSpeed;
                _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
            }
        }

        // Tính vị trí camera: phía sau nhân vật, offset theo góc
        Quaternion rotation = Quaternion.Euler(_verticalAngle, _horizontalAngle, 0f);
        Vector3 offset = rotation * new Vector3(0f, height, -_currentDistance);
        Vector3 targetPosition = target.position + offset;

        // Di chuyển camera mượt mà
        transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * lookAtHeight);   // Nhìn vào lưng/vai (God of War)

        // Đồng bộ xoay nhân vật theo hướng nhìn ngang (để di chuyển đúng hướng)
        Quaternion targetRotation = Quaternion.Euler(0f, _horizontalAngle, 0f);
        var rb = target.GetComponent<Rigidbody>();
        if (rb != null)
            rb.MoveRotation(targetRotation);
        else
            target.rotation = targetRotation;
    }

    /// <summary>
    /// Góc ngang hiện tại - dùng cho PlayerController để di chuyển theo hướng nhìn.
    /// </summary>
    public float HorizontalAngle => _horizontalAngle;
}
