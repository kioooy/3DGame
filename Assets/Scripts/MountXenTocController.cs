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
    public Transform playerSitPoint; 
    private RigidbodyConstraints _originalConstraints;
    private PlayableGraph _playerGraph;

    private void Start()
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

        // Vô hiệu hoá Rigidbody và Collider của Player để tránh Physics Explosion (hai collider đẩy nhau cực mạnh)
        if (_player.TryGetComponent<Rigidbody>(out Rigidbody prb)) prb.isKinematic = true;
        if (_player.TryGetComponent<Collider>(out Collider pcol)) pcol.enabled = false;

        // Dùng transform tay thay vì SetParent để Xén Tóc ko bóp méo scale của Dế Mèn (do model scale x100)
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

        // Disable NPC script to stop wandering
        XenTocNPC npcScript = GetComponent<XenTocNPC>();
        if (npcScript != null)
        {
            npcScript.enabled = false;
        }

        if (_xenTocRb != null)
        {
            _originalConstraints = _xenTocRb.constraints;
            _xenTocRb.useGravity = false;
            _xenTocRb.isKinematic = false;
            // Giải phóng Freeze Position X và Z để Rigidbody.velocity có tác dụng
            _xenTocRb.constraints = RigidbodyConstraints.FreezeRotation; 
        }

        // Tắt script tự di chuyển / Wander của NPC (nếu có) để khỏi tranh giành Velocity với di chuyển bay
        if (TryGetComponent<XenTocNPC>(out XenTocNPC npcAI))
        {
            npcAI.enabled = false;
            if (npcAI.interactionPromptUI != null) npcAI.interactionPromptUI.SetActive(false);
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

        // ── 1. Đọc Phím Di Chuyển (WASD) ──
        float horizontal = 0f;
        if (kb.aKey.isPressed) horizontal = -1f;
        if (kb.dKey.isPressed) horizontal = 1f;

        float vertical = 0f;
        if (kb.wKey.isPressed) vertical = 1f;
        if (kb.sKey.isPressed) vertical = -1f;

        // ── 2. Xoay thân Xén Tóc (A/D) ──
        if (horizontal != 0f)
        {
            transform.Rotate(0, horizontal * turnSpeed * Time.deltaTime, 0);
        }

        // ── 3. Đi Tiến/Lùi theo hướng mặt của thú (W/S) ──
        Vector3 moveDir = transform.forward * vertical;

        // ── 4. Điều Khiển Thăng Giáng (Bay lên / Hạ xuống) ──
        float moveY = 0f;
        if (kb.spaceKey.isPressed) moveY = 1f;
        if (kb.leftCtrlKey.isPressed || kb.cKey.isPressed) moveY = -1f;

        // Tính tổng vận tốc: Phương ngang + Phương đứng
        Vector3 finalVelocity = (moveDir * currentSpeed) + (Vector3.up * moveY * verticalSpeed);

        // Di chuyển Xén Tóc bằng Rigidbody Velocity
        if (_xenTocRb != null)
        {
            _xenTocRb.linearVelocity = finalVelocity;
        }

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

        // Đặt player xuống vị trí an toàn (bên hông Xén Tóc, bù cao độ để không lọt lòng đất)
        _player.position = transform.position + transform.right * 1.5f + Vector3.up * 1f;
        _player.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // Bật lại PlayerController
        if (_playerCC   != null) _playerCC.enabled   = true;
        if (_playerCtrl != null) _playerCtrl.enabled = true;

        // Bật lại script NPC tự di chuyển
        if (TryGetComponent<XenTocNPC>(out XenTocNPC npcAI))
        {
            npcAI.enabled = true;
        }

        // Bật lại Vật lý của Player
        if (_player.TryGetComponent<Rigidbody>(out Rigidbody prb)) prb.isKinematic = false;
        if (_player.TryGetComponent<Collider>(out Collider pcol)) pcol.enabled = true;

        // Bật lại Rigidbody gravity cho Xén Tóc để rớt xuống đất
        if (_xenTocRb != null)
        {
            _xenTocRb.linearVelocity = Vector3.zero; // Xoá dư âm vận tốc của Xén Tóc
            _xenTocRb.isKinematic = false;
            _xenTocRb.useGravity = true; // Bật trọng lực để rơi xuống từ từ
            _xenTocRb.constraints = _originalConstraints; // Trả lại Rigidbody constraints cũ
        }
        
        IsRiding = false;
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

        // Bổ sung: Lưu lại vị trí của Xén Tóc do lúc Đua Xe sẽ Reset lại Scene
        PlayerPrefs.SetFloat("XenTocPosX", transform.position.x);
        PlayerPrefs.SetFloat("XenTocPosY", transform.position.y);
        PlayerPrefs.SetFloat("XenTocPosZ", transform.position.z);
        PlayerPrefs.SetInt("HasSavedXenTocPos", 1);
        PlayerPrefs.Save();

        Debug.Log("[MountXenToc] 🪲 Đã xuống khỏi Xén Tóc!");
    }

    void OnDisable()
    {
        if (_mounted) Dismount();
    }
}
