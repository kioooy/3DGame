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
    [SerializeField] float lookAtHeight = 1.4f;      // Điểm camera nhìn vào (lưng/vai nhân vật)

    [Header("Xoay chuột")]
    [SerializeField] float mouseSensitivity = 2f;
    [SerializeField] float minVerticalAngle = -70f;  // Nhìn lên trời (Góc quay âm)
    [SerializeField] float maxVerticalAngle = 70f;   // Nhìn xuống đất

    [Header("First Person")]
    [SerializeField] bool isFirstPerson = false;
    [SerializeField] float fpHeightOffset = 1.6f;     // Chiều cao mắt nhân vật
    [SerializeField] float fpForwardOffset = 0.2f;    // Tiến lên một chút khỏi tâm nhân vật để tránh cạ mặt vào model

    public float horizontalAngle;   // Góc xoay ngang (quanh nhân vật/mắt)
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
            horizontalAngle = target.eulerAngles.y;
            _verticalAngle = 12f;  // Góc nhìn mặc định
        }
        _currentDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Dừng xoay Camera và Zoom nếu Menu đang bật để chuột có thể bấm nút
        if (MainMenuManager.IsMenuActive) return;
        
        // Dừng xoay Camera nếu Emote Menu (vòng tròn) đang bật
        if (EmoteUIManager.Instance != null && EmoteUIManager.IsEmoteMenuOpen) return;
        
        // Dừng xoay Camera nếu Pause Menu đang bật
        if (PauseMenuManager.IsPaused) return;
        
        // Dừng xoay Camera nếu đang chơi Minigame Caro
        if (CaroGameManager.Instance != null && CaroGameManager.Instance.IsGameActive) return;

        // Dừng xoay Camera nếu đang chơi Minigame Vật Tay (Audition)
        if (ArmWrestlingManager.Instance != null && ArmWrestlingManager.Instance.IsGameActive) return;

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
            horizontalAngle += delta.x * currentSensitivity;
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
        Quaternion rotation = Quaternion.Euler(_verticalAngle, horizontalAngle, 0f);

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
            float currentHitDistance = _currentDistance;
            bool didHit = false;

            // Dùng RaycastAll để xuyên qua chính người chơi và chỉ dừng ở tường/đất
            // Đồng thời QueryTriggerInteraction.Ignore giúp Camera đi xuyên qua Trigger (ví dụ: vòng item pickable)
            RaycastHit[] hits = Physics.RaycastAll(pivotPosition, offset.normalized, _currentDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            
            foreach (RaycastHit hitInfo in hits)
            {
                // Bỏ qua nếu hit trúng chính nhân vật đang follow (Mấu chốt chống giật lag zoom)
                // Cấu trúc nhân vật phải nằm trong hoặc bằng với target
                if (hitInfo.transform == target || hitInfo.transform.IsChildOf(target)) continue;
                
                // Cập nhật vị trí vật cản gần camera nhất (do RaycastAll không sắp xếp sẵn trật tự gần xa)
                if (hitInfo.distance < currentHitDistance)
                {
                    currentHitDistance = hitInfo.distance;
                    didHit = true;
                }
            }

            if (didHit)
            {
                // Nếu tia chạm phải vật thể (đất, tường...), đưa camera lại gần điểm chạm
                // Trừ đi một xíu (vd 0.2f) để góc camera không bị kẹt sát vào tường
                targetPosition = pivotPosition + offset.normalized * (currentHitDistance - 0.2f);

                // Đảm bảo không bị zoom vào quá gần tâm khiến camera chui tọt vào người
                if (currentHitDistance < 0.2f)
                {
                    targetPosition = pivotPosition + offset.normalized * 0.2f;
                }
            }

            // Gắn trực tiếp vị trí camera (KHÔNG Lerp) để tránh giật lag do lệch nhịp với FixedUpdate của vật lý nhân vật.
            transform.position = targetPosition;

            // Luôn hướng về phía trước theo đúng trục quay
            transform.rotation = rotation;
        }

        // (Đã xoá phần ép nhân vật xoay theo hướng nhìn để nhân vật có thể tự do di chuyển)
    }

    /// <summary>
    /// Góc ngang hiện tại - dùng cho PlayerController để di chuyển theo hướng nhìn.
    /// </summary>
    public float HorizontalAngle => _horizontalAngle;
}
