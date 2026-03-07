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
    [SerializeField] float minVerticalAngle = -70f;  // Nhìn lên trời (Góc quay âm)
    [SerializeField] float maxVerticalAngle = 70f;   // Nhìn xuống đất

    [Header("First Person")]
    [SerializeField] bool isFirstPerson = false;
    [SerializeField] float fpHeightOffset = 1.6f;     // Chiều cao mắt nhân vật
    [SerializeField] float fpForwardOffset = 0.2f;    // Tiến lên một chút khỏi tâm nhân vật để tránh cạ mặt vào model

    float _horizontalAngle;   // Góc xoay ngang (quanh nhân vật/mắt)
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
            _verticalAngle = 12f;  // Góc nhìn mặc định
        }
        _currentDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Dừng xoay Camera và Zoom nếu Menu đang bật để chuột có thể bấm nút
        if (MainMenuManager.IsMenuActive) return;

        // Đọc phím V để đổi góc nhìn
        var kb = Keyboard.current;
        if (kb != null && kb.vKey.wasPressedThisFrame)
        {
            isFirstPerson = !isFirstPerson;
        }

        // Đọc input chuột
        var mouse = Mouse.current;
        if (mouse != null)
        {
            float currentSensitivity = mouseSensitivity;
            if (SettingsManager.Instance != null)
            {
                currentSensitivity = SettingsManager.Instance.mouseSensitivity;
            }

            Vector2 delta = mouse.delta.ReadValue();
            _horizontalAngle += delta.x * currentSensitivity;
            _verticalAngle -= delta.y * currentSensitivity;
            _verticalAngle = Mathf.Clamp(_verticalAngle, minVerticalAngle, maxVerticalAngle);

            // Chỉ zoom trong góc nhìn thứ 3
            if (!isFirstPerson)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    _currentDistance -= Mathf.Sign(scroll) * zoomSpeed;
                    _currentDistance = Mathf.Clamp(_currentDistance, minDistance, maxDistance);
                }
            }
        }

        // Tính toán vòng xoay chung cho camera
        Quaternion rotation = Quaternion.Euler(_verticalAngle, _horizontalAngle, 0f);

        if (isFirstPerson)
        {
            // First Person View
            Vector3 eyePosition = target.position + Vector3.up * fpHeightOffset;
            
            // Đẩy camera ra phía trước mặt nhân vật một khoảng (fpForwardOffset)
            float dynamicForwardPush = fpForwardOffset;
            
            // Khi gập cổ nhìn xuống (góc _verticalAngle dương), đẩy camera xa ra phía trước mặt thêm chút nữa 
            // để mắt không bị lọt vào ngực trần của nhân vật
            if (_verticalAngle > 0)
            {
                dynamicForwardPush += (_verticalAngle / maxVerticalAngle) * 0.4f; 
            }

            Vector3 forwardOffset = rotation * Vector3.forward * dynamicForwardPush;
            
            // Camera nằm ở mắt và xoay theo hướng nhìn tự do
            transform.position = eyePosition + forwardOffset;
            transform.rotation = rotation;
            
            // Có thể cần ẩn model nhân vật đi ở góc nhìn FPP (tuỳ chọn)
        }
        else
        {
            // Third Person View (Skyrim Style)
            // Tâm nhìn là ngang ngực / vai nhân vật
            Vector3 pivotPosition = target.position + Vector3.up * lookAtHeight;

            // Camera lùi lại phía sau THEO HƯỚNG QUAY CỦA NÓ (rotation * Vector3.back)
            Vector3 offset = rotation * new Vector3(0f, 0f, -_currentDistance);
            Vector3 targetPosition = pivotPosition + offset;

            // --- Camera Collision (Chống xuyên tường/đất) ---
            // Bắn một tia từ tâm nhìn (pivot) ra vị trí camera mong muốn
            RaycastHit hit;
            // Dùng LayerMask mặc định, hoặc cấu hình layer riêng nếu cần
            if (Physics.Raycast(pivotPosition, offset.normalized, out hit, _currentDistance))
            {
                // Nếu tia chạm phải vật thể (đất, tường...), đưa camera lại gần điểm chạm
                // Trừ đi một xíu (vd 0.2f) để góc camera không bị kẹt sát vào tường
                targetPosition = hit.point - offset.normalized * 0.2f;

                // Đảm bảo không bị zoom vào quá gần tâm (xuyên qua người)
                if (Vector3.Distance(pivotPosition, targetPosition) < 0.2f)
                {
                    targetPosition = pivotPosition + offset.normalized * 0.2f;
                }
            }

            // Di chuyển camera mượt mà
            transform.position = Vector3.Lerp(transform.position, targetPosition, followSpeed * Time.deltaTime);

            // Luôn hướng về phía trước theo đúng trục quay
            transform.rotation = Quaternion.Lerp(transform.rotation, rotation, followSpeed * Time.deltaTime);
        }

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
