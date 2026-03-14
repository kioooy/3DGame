using UnityEngine;
using UnityEngine.InputSystem;

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

    [Header("Hiệu ứng")]
    [Tooltip("Particle phát ra khi bay (tùy chọn)")]
    public ParticleSystem flyParticle;

    // Trạng thái
    public static bool IsRiding { get; private set; }

    private Transform        _player;
    private PlayerController _playerCtrl;
    private CharacterController _playerCC;
    private Animator         _xenTocAnimator;
    private Rigidbody        _xenTocRb;

    private Vector3 _velocity;
    private bool    _mounted;

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

        // Tắt toàn bộ physics/input của player
        if (_playerCtrl != null) _playerCtrl.enabled = false;
        if (_playerCC   != null) _playerCC.enabled   = false;

        // Gán player làm con của Xén Tóc (ngồi lên lưng)
        _player.SetParent(transform);
        _player.localPosition = riderOffset;
        _player.localRotation = Quaternion.identity;

        // Tắt gravity của Xén Tóc
        if (_xenTocRb != null)
        {
            _xenTocRb.useGravity  = false;
            _xenTocRb.isKinematic = true; // Dùng script di chuyển trực tiếp
        }

        // Unlock cursor để thể hiện đang trong chế độ đặc biệt
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        if (flyParticle != null) flyParticle.Play();

        _mounted   = true;
        IsRiding   = true;

        Debug.Log("[MountXenToc] 🪲 Đã cưỡi lên Xén Tóc! Dùng WASD để điều khiển, Q/E hoặc Scroll lên/xuống. Nhấn F để xuống.");
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

        // ── Đọc Input di chuyển (WASD) ──
        float h = 0f, v = 0f, vert = 0f;
        if (kb.aKey.isPressed) h -= 1f;
        if (kb.dKey.isPressed) h += 1f;
        if (kb.wKey.isPressed) v += 1f;
        if (kb.sKey.isPressed) v -= 1f;

        // Q/E hoặc Scroll chuột lên/xuống
        if (kb.qKey.isPressed)            vert += 1f;
        if (kb.eKey.isPressed)            vert -= 1f;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll > 0)  vert += 1f;
            if (scroll < 0)  vert -= 1f;
        }

        // ── Xoay Xén Tóc theo input ngang ──
        transform.Rotate(Vector3.up, h * rotationSpeed * Time.deltaTime, Space.World);

        // ── Tính hướng bay theo góc nhìn của Xén Tóc ──
        Vector3 forward  = transform.forward;
        Vector3 up       = Vector3.up;
        Vector3 desiredVelocity = forward * v * flySpeed + up * vert * verticalSpeed;

        // ── Di chuyển mượt ──
        _velocity      = Vector3.Lerp(_velocity, desiredVelocity, smoothing / Time.deltaTime * Time.deltaTime * 10f);
        transform.position += _velocity * Time.deltaTime;

        // ── Nghiêng thân Xén Tóc khi bay lên/xuống ──
        float tiltAngle = Mathf.Clamp(vert * -15f, -25f, 25f);
        transform.localEulerAngles = new Vector3(tiltAngle, transform.localEulerAngles.y, 0f);
    }

    // ──────────────────────────────────────────────────
    // Xuống cưỡi
    // ──────────────────────────────────────────────────
    public void Dismount()
    {
        if (!_mounted) return;
        _mounted   = false;
        IsRiding   = false;

        // Tách player ra khỏi Xén Tóc
        _player.SetParent(null);

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

        // Khóa cursor lại như bình thường
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        _velocity  = Vector3.zero;

        Debug.Log("[MountXenToc] 🪲 Đã xuống khỏi Xén Tóc!");
    }

    void OnDisable()
    {
        if (_mounted) Dismount();
    }
}
