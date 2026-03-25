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

    [Header("Âm Thanh Bay")]
    [Tooltip("Tiếng cánh bay (loop tự động)")]
    public AudioClip flySound;
    [Range(0f, 1f)]
    public float flySoundVolume = 0.55f;
    private AudioSource _flyAudio;

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

        // Khởi tạo AudioSource riêng cho tiếng bay
        _flyAudio = gameObject.AddComponent<AudioSource>();
        _flyAudio.spatialBlend = 0f;   // 2D
        _flyAudio.loop         = true;
        _flyAudio.playOnAwake  = false;
        _flyAudio.volume       = 0f;

        // Tắt Rigidbody gravity khi bay — sẽ bật lại khi bỏ cưỡi
        if (_xenTocRb != null)
        {
            _xenTocRb.useGravity = false;
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

        // Phát tiếng bay loop
        if (_flyAudio != null && flySound != null)
        {
            _flyAudio.clip   = flySound;
            _flyAudio.volume = flySoundVolume;
            _flyAudio.Play();
        }

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


    // Cache để FixedUpdate và LateUpdate dùng chung
    private Vector3 _cachedVelocity;

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
            currentSpeed = flySpeed * 2.5f;
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

        // ── 2. Xoay thân Xén Tóc (A/D) – chỉ xoay trục Y để không gây roll ──
        if (horizontal != 0f)
        {
            transform.Rotate(Vector3.up, horizontal * turnSpeed * Time.deltaTime, Space.World);
        }

        // ── 3. Đi Tiến/Lùi theo hướng mặt của thú (W/S) ──
        Vector3 moveDir = transform.forward * vertical;

        // ── 4. Điều Khiển Thăng Giáng + Vật Lý Pitch ──
        float moveY = 0f;
        bool holdingSpace  = kb.spaceKey.isPressed;
        bool holdingCrouch = kb.leftCtrlKey.isPressed || kb.cKey.isPressed;

        if (holdingSpace)        moveY =  1f;
        else if (holdingCrouch)  moveY = -1f;
        else                     moveY = -0.4f;

        // Pitch (nghiêng mũi) theo trạng thái bay
        float targetPitch;
        if (holdingSpace)       targetPitch = -30f;
        else if (holdingCrouch) targetPitch =  45f;
        else                    targetPitch =  25f;

        // Slerp pitch, roll luôn = 0
        float currentYaw      = transform.eulerAngles.y;
        Quaternion targetRot  = Quaternion.Euler(targetPitch, currentYaw, 0f);
        transform.rotation    = Quaternion.Slerp(transform.rotation, targetRot, 5f * Time.deltaTime);

        // Cache velocity để FixedUpdate apply
        _cachedVelocity = (moveDir * currentSpeed) + (Vector3.up * moveY * verticalSpeed);
    }

    // Physics apply – đồng bộ với engine, không gây desync
    void FixedUpdate()
    {
        if (!_mounted || _xenTocRb == null) return;
        _xenTocRb.linearVelocity = _cachedVelocity;
    }

    // Cập nhật vị trí player SAU khi Rigidbody đã giải quyết xong → không giật
    void LateUpdate()
    {
        if (!_mounted || _player == null) return;
        _player.position = transform.position + transform.up * riderOffset.y + transform.forward * riderOffset.z;
        _player.rotation = transform.rotation;
    }

    // ──────────────────────────────────────────────────
    // Bảng hướng dẫn phím – hiện góc dưới phải khi cưỡi
    // ──────────────────────────────────────────────────
    private static readonly (string key, string desc)[] _guideLines = {
        ("W / S",     "Tiến / Lùi"),
        ("A / D",     "Rẽ trái / Rẽ phải"),
        ("SPACE",     "Bay lên ▲"),
        ("CTRL / C",  "Hạ xuống ▼"),
        ("SHIFT",     "Tăng tốc ⚡"),
        ("F / ESC",   "Xuống cưỡi"),
    };

    private GUIStyle _guideBox;
    private GUIStyle _guideTitle;
    private GUIStyle _guideRow;

    void OnGUI()
    {
        if (!_mounted) return;

        // Khởi style lần đầu
        if (_guideBox == null)
        {
            _guideBox = new GUIStyle(GUI.skin.box)
            {
                padding   = new RectOffset(14, 14, 10, 10),
                alignment = TextAnchor.UpperLeft
            };
            _guideBox.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.6f));

            _guideTitle = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
            };
            _guideTitle.normal.textColor = new Color(1f, 0.85f, 0.2f);

            _guideRow = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
            };
            _guideRow.normal.textColor = Color.white;
        }

        // Kích thước panel
        float panelW  = 240f;
        float rowH    = 20f;
        float titleH  = 26f;
        float padV    = 10f;
        float panelH  = titleH + _guideLines.Length * rowH + padV * 2f;
        float margin  = 16f;
        float x = Screen.width  - panelW - margin;
        float y = Screen.height - panelH - margin;

        // Vẽ panel nền
        GUI.Box(new Rect(x, y, panelW, panelH), GUIContent.none, _guideBox);

        // Tiêu đề
        GUI.Label(new Rect(x, y + padV, panelW, titleH), "🪲  Điều khiển Xén Tóc", _guideTitle);

        // Từng phím
        float rowY = y + padV + titleH;
        foreach (var (key, desc) in _guideLines)
        {
            // Key – màu vàng
            GUI.Label(new Rect(x + 12, rowY, 90, rowH),
                      key,
                      new GUIStyle(_guideRow) { normal = { textColor = new Color(1f, 0.85f, 0.25f) }, fontStyle = FontStyle.Bold });
            // Mô tả
            GUI.Label(new Rect(x + 108, rowY, panelW - 108 - 12, rowH), desc, _guideRow);
            rowY += rowH;
        }
    }

    private static Texture2D MakeTex(int w, int h, Color col)
    {
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        var tex = new Texture2D(w, h);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
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
        // Dừng tiếng bay
        if (_flyAudio != null) _flyAudio.Stop();

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
