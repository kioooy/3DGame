using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.Animations;

/// <summary>
/// Điều khiển chế độ cưỡi Xén Tóc sau khi thắng minigame Vật Tay.
/// Gắn vào GameObject của Xén Tóc.
/// </summary>
public class MountXenTocController : MonoBehaviour
{
    [Header("Vị trí ngồi (tương đối so với Xén Tóc)")]
    [Tooltip("Offset vị trí player ngồi trên lưng Xén Tóc")]
    public Vector3 riderOffset = new Vector3(0f, 1.8f, 0.2f);

    [Header("Tốc độ Bay")]
    public float flySpeed      = 18f;
    public float verticalSpeed = 10f;
    public float rotationSpeed = 120f;    // Độ/giây xoay ngang
    [Range(0f, 1f)]
    public float smoothing     = 0.12f;   // Độ mượt di chuyển

    [Header("Bẻ Lái Xoay Vòng (Phím A/D)")]
    public float turnSpeed = 150f;

    [Header("Thể lực Bay (Kéo Thanh Vàng vào đây)")]
    public UnityEngine.UI.Slider staminaSlider;
    public float maxStamina = 100f;
    public float staminaDrainRate = 18f;
    public float staminaRecoverRate = 10f;
    private float _currentStamina;

    [Header("Hiệu ứng")]
    [Tooltip("Particle phát ra khi bay (tùy chọn)")]
    public ParticleSystem flyParticle;

    [Header("Hoạt ảnh (Gán tự động qua Tool)")]
    public AnimationClip xenTocFlyingClip;
    public AnimationClip playerSittingClip;

    // Trạng thái
    public static bool IsRiding { get; private set; }

    private Transform        _player;
    private PlayerController _playerCtrl;
    private CharacterController _playerCC;
    private Animator         _xenTocAnimator;
    private Rigidbody        _xenTocRb;

    private Vector3 _velocity;
    private bool    _mounted;

    // Đồ thị Animation Override
    private PlayableGraph _xenTocGraph;
    private PlayableGraph _playerGraph;

    void Awake()
    {
        _xenTocAnimator = GetComponent<Animator>();
        _xenTocRb       = GetComponent<Rigidbody>();

        // Tắt Rigidbody gravity khi bay — sẽ bật lại khi bỏ cưỡi
        if (_xenTocRb != null)
        {
            _xenTocRb.useGravity = false; // Tắt hẳn để tự kiểm soát từ script
        }
    }

    // ──────────────────────────────────────────────────
    // Gọi từ XenTocNPC khi player bấm "Cưỡi Xén Tóc"
    // ──────────────────────────────────────────────────
    public void Mount(Transform playerTransform)
    {
        if (_mounted) return;

        _player    = playerTransform;
        _playerCtrl = _player.GetComponent<PlayerController>();
        _playerCC   = _player.GetComponent<CharacterController>();

        // Tắt toàn bộ physics/input/gravity của player
        if (_playerCtrl != null) _playerCtrl.enabled = false;
        if (_playerCC   != null) _playerCC.enabled   = false;

        // Dùng transform tay thay vì SetParent để Xén Tóc ko bóp méo scale của Dế Mèn (do model scale x100)
        _player.position = transform.position + transform.up * riderOffset.y + transform.forward * riderOffset.z;
        _player.rotation = transform.rotation;

        // Khởi tạo thể lực
        _currentStamina = maxStamina;
        if (staminaSlider != null)
        {
            staminaSlider.maxValue = maxStamina;
            staminaSlider.value = _currentStamina;
        }

        // Tắt gravity của Xén Tóc
        if (_xenTocRb != null)
        {
            _xenTocRb.useGravity  = false;
            _xenTocRb.isKinematic = true; // Dùng script di chuyển trực tiếp
        }

        // Unlock cursor để thể hiện đang trong chế độ đặc biệt
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (_xenTocAnimator != null)
        {
            _xenTocAnimator.applyRootMotion = false; // Tắt Root Motion để tự do bay lên không bị animation khóa trục Y
        }

        if (flyParticle != null) flyParticle.Play();

        // ── Kích hoạt Hoạt Ảnh (PlayableGraph) ──
        if (xenTocFlyingClip != null && _xenTocAnimator != null)
        {
            PlayOverrideAnimation(_xenTocAnimator, xenTocFlyingClip, ref _xenTocGraph);
        }
        
        Animator playerAnim = _player.GetComponent<Animator>();
        if (playerSittingClip != null && playerAnim != null)
        {
            PlayOverrideAnimation(playerAnim, playerSittingClip, ref _playerGraph);
        }

        _mounted   = true;
        IsRiding   = true;

        Debug.Log("[MountXenToc] 🪲 Đã cưỡi lên Xén Tóc! Dùng WASD để điều khiển, Q/E hoặc Scroll lên/xuống. Nhấn F để xuống.");
    }

    private void PlayOverrideAnimation(Animator anim, AnimationClip clip, ref PlayableGraph graph)
    {
        if (anim == null || clip == null) return;
        
        if (graph.IsValid()) graph.Destroy();

        graph = PlayableGraph.Create();
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var playableOutput = AnimationPlayableOutput.Create(graph, "CustomAnimation", anim);
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        
        playableOutput.SetSourcePlayable(clipPlayable);
        graph.Play();
    }


    void Update()
    {
        if (!_mounted) return;

        var kb    = Keyboard.current;
        var mouse = Mouse.current;

        if (kb == null) return;

        // ── Nhấn F hoặc ESC → Xuống cưỡi ──
        if (kb.fKey.wasPressedThisFrame || (kb.escapeKey.wasPressedThisFrame && IsRiding))
        {
            Dismount();
            return;
        }

        // ── Tính toán Thể Lực và Tăng Tốc ──
        bool isBoosting = kb.shiftKey.isPressed || (mouse != null && mouse.rightButton.isPressed);
        float currentSpeed = flySpeed;

        if (isBoosting && _currentStamina > 0)
        {
            _currentStamina -= staminaDrainRate * Time.deltaTime;
            currentSpeed = flySpeed * 2.5f; // Bay nhanh gấp 2.5 lần
        }
        else
        {
            _currentStamina += staminaRecoverRate * Time.deltaTime;
        }
        
        _currentStamina = Mathf.Clamp(_currentStamina, 0, maxStamina);
        if (staminaSlider != null) staminaSlider.value = _currentStamina;

        // ── 1. Đọc Phím A/D để Bẻ Lái Xoay Vòng CẢ CAMERA VÀ XÉN TÓC (Yaw) ──
        float steer = 0f;
        if (kb.aKey.isPressed) steer -= 1f;
        if (kb.dKey.isPressed) steer += 1f;
        
        float targetYaw = transform.eulerAngles.y;
        float targetPitch = 0f;

        if (Camera.main != null)
        {
            // Nếu bấm A/D -> Ép cả Camera xoay theo để Camera luôn ở sau lưng
            if (steer != 0f)
            {
                ThirdPersonCamera camScript = Camera.main.GetComponent<ThirdPersonCamera>();
                if (camScript != null)
                {
                    camScript.horizontalAngle += steer * turnSpeed * Time.deltaTime;
                }
            }

            // ── 2. Lấy Góc nhìn Chuẩn từ Camera ──
            targetPitch = Camera.main.transform.eulerAngles.x;
            if (targetPitch > 180f) targetPitch -= 360f;
            targetPitch = Mathf.Clamp(targetPitch, -80f, 80f); // Ép giới hạn góc lộn nhào

            targetYaw = Camera.main.transform.eulerAngles.y;
        }

        // Xoay Xén Tóc mượt mà: Luôn bám sát chính xác góc nhòm của Camera!
        Quaternion targetRot = Quaternion.Euler(targetPitch, targetYaw, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 8f * Time.deltaTime);

        // ── 3. Điều khiển Dịch Chuyển (Tiến Lùi / Thăng Giáng) ──
        float moveZ = 0f;
        if (kb.wKey.isPressed) moveZ = 1f;
        if (kb.sKey.isPressed) moveZ = -1f;

        float moveY = 0f;
        if (kb.spaceKey.isPressed) moveY = 1f;
        if (kb.leftCtrlKey.isPressed || kb.cKey.isPressed) moveY = -1f;

        // Tính vận tốc:
        // + Bay tới/lui theo hướng HỘP SỌ XÉNTÓC (transform.forward) (Nếu Box sọ ngước lên trời -> Sẽ bay hướng lên)
        // + Bay Thăng/Giáng tuyệt đối theo trục dọc thế giới (Vector3.up)
        Vector3 finalVelocity = (transform.forward * moveZ * currentSpeed) + (Vector3.up * moveY * verticalSpeed);

        // Di chuyển Xén Tóc trong không gian
        transform.position += finalVelocity * Time.deltaTime;

        // ── Cập nhật tọa độ Player bám sát lưng Xén Tóc (Bất Chấp Scale Xén Tóc lớn) ──
        _player.position = transform.position + transform.up * riderOffset.y + transform.forward * riderOffset.z;
        _player.rotation = transform.rotation;
    }

    // ──────────────────────────────────────────────────
    // Xuống cưỡi
    // ──────────────────────────────────────────────────
    public void Dismount()
    {
        if (!_mounted) return;
        _mounted   = false;
        IsRiding   = false;

        // Đặt player đứng ngay dưới Xén Tóc
        _player.position = transform.position + Vector3.down * 1.5f;
        _player.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // Bật lại PlayerController
        if (_playerCC   != null) _playerCC.enabled   = true;
        if (_playerCtrl != null) _playerCtrl.enabled = true;

        // Bật lại Rigidbody gravity
        if (_xenTocRb != null)
        {
            _xenTocRb.isKinematic = false;
            _xenTocRb.useGravity  = true ;
        }

        // Dừng Particle
        if (flyParticle != null) flyParticle.Stop();

        if (_xenTocAnimator != null)
        {
            _xenTocAnimator.applyRootMotion = true; // Trả lại Root Motion
        }

        // Khóa cursor lại như bình thường
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        // Dừng Animation Override
        if (_xenTocGraph.IsValid()) _xenTocGraph.Destroy();
        if (_playerGraph.IsValid()) _playerGraph.Destroy();

        _velocity  = Vector3.zero;

        Debug.Log("[MountXenToc] 🪲 Đã xuống khỏi Xén Tóc!");
    }

    void OnDisable()
    {
        if (_mounted) Dismount();
    }
}
