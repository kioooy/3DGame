using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] Animator animator;
    [SerializeField] Rigidbody rb;

    [Header("Movement Settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runSpeedMultiplier = 3f;
    [SerializeField] float jumpForce = 8f;
    [SerializeField] float gravity = -25f;

    [Header("Ground Check")]
    [SerializeField] float groundCheckDistance = 0.4f;
    [SerializeField] Vector3 groundCheckOffset = new Vector3(0, 0.1f, 0);
    [SerializeField] LayerMask groundLayer; // Layer cho ground (không bao gồm items)

    [Header("Interaction Settings")]
    [SerializeField] float interactionRange = 3.5f;
    [SerializeField] LayerMask itemLayer;
    [SerializeField] Transform cameraTransform;
    
    [Header("Equipment")]
    [SerializeField] private PlayerEquipment playerEquipment;

    [Header("Footstep Audio")]
    [SerializeField] private AudioClip[] footstepClips;
    [SerializeField] private float walkStepInterval = 0.45f;
    [SerializeField] private float runStepInterval  = 0.25f;
    [Range(0f, 1f)]
    [SerializeField] private float footstepVolume = 0.55f;
    private AudioSource _footstepSource;
    private float _stepTimer = 0f;

    Vector3 _moveInput;
    bool _isRunning;
    float _verticalVelocity;
    bool _isGrounded;
    bool _isJumping;
    
    // Interaction
    private PickableItem _currentLookingItem;
    private bool _inventoryOpen = false;
    public bool isDialoguing = false; // Skyrim-like conversation pause flag

    // Emote System
    private float _tKeyHoldTime = 0f;
    private bool _tKeyHeld = false;
    private const float EMOTE_HOLD_THRESHOLD = 0.25f; // Thời gian giữ T để mở Emote Menu
    
    // Delegate / Event nhắc nhở PlayerController là đã bị can thiệp logic Emote


    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;

        // Setup AudioSource for footsteps (2D, no spatial blend)
        _footstepSource = gameObject.AddComponent<AudioSource>();
        _footstepSource.spatialBlend = 0f;
        _footstepSource.playOnAwake  = false;
        _footstepSource.loop         = false;
        _footstepSource.volume       = footstepVolume;
    }

    void Start()
    {
        if (rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        if (animator != null)
        {
            animator.SetBool("Jump", false);
            animator.SetBool("IsJumping", false);
        }

        // --- Minimap Marker ---
        MinimapMarker marker = gameObject.AddComponent<MinimapMarker>();
        marker.markerColor = Color.blue; // Player

        // --- Load Position if Returning from Racing Minigame ---
        if (PlayerPrefs.GetInt("HasSavedPostRacePosition", 0) == 1)
        {
            float x = PlayerPrefs.GetFloat("PlayerRawPosX", transform.position.x);
            float y = PlayerPrefs.GetFloat("PlayerRawPosY", transform.position.y);
            float z = PlayerPrefs.GetFloat("PlayerRawPosZ", transform.position.z);
            
            transform.position = new Vector3(x, y, z);
            
            // Xóa cờ để lần load scene sau hoặc lúc bật game mới không bị dịch chuyển bậy
            PlayerPrefs.SetInt("HasSavedPostRacePosition", 0);
        }
    }

    void Update()
    {
        // Chặn hoàn toàn mọi thao tác nếu Main Menu đang bật
        if (MainMenuManager.IsMenuActive)
        {
            _moveInput = Vector3.zero;
            _isRunning = false;
            UpdateAnimator();
            return;
        }

        // Ground check - ignore items layer
        if (groundLayer.value != 0)
        {
            _isGrounded = Physics.Raycast(transform.position + groundCheckOffset, Vector3.down, groundCheckDistance, groundLayer);
        }
        else
        {
            // Fallback: check everything except items
            int itemLayerMask = LayerMask.GetMask("Item");
            int everythingExceptItems = ~itemLayerMask;
            _isGrounded = Physics.Raycast(transform.position + groundCheckOffset, Vector3.down, groundCheckDistance, everythingExceptItems);
        }

        // Check inventory state
        _inventoryOpen = InventoryUI.Instance != null && InventoryUI.Instance.IsOpen;

        // Detect pickable items
        DetectPickableItems();

        var kb = Keyboard.current;
        if (kb != null)
        {
            // Toggle inventory với Tab
            if (kb.tabKey.wasPressedThisFrame)
            {
                if (InventoryUI.Instance != null)
                    InventoryUI.Instance.ToggleInventory();
            }

            // Toggle quest panel với J
            if (kb.jKey.wasPressedThisFrame)
            {
                Debug.Log("PlayerController: Phím J được nhấn!");
                if (QuestUIManager.Instance != null)
                {
                    Debug.Log("PlayerController: Gọi QuestUIManager.Instance.ToggleQuestPanel()");
                    QuestUIManager.Instance.ToggleQuestPanel();
                }
                else
                {
                    Debug.LogWarning("PlayerController: Lỗi! QuestUIManager.Instance bị null!");
                }
            }

            // --- Gắn Input Nhặt đồ (E) ---
            if (kb.eKey.wasPressedThisFrame && _currentLookingItem != null && !_inventoryOpen)
            {
                TryPickupItem();
            }

            // --- Xử lý phím T: Short press = Xài lại Emote cũ, Long press = Mở Radial Menu ---
            if (kb.tKey.wasPressedThisFrame)
            {
                _tKeyHoldTime = 0f;
                _tKeyHeld = true;
            }

            if (kb.tKey.isPressed && _tKeyHeld && !_inventoryOpen && !isDialoguing)
            {
                _tKeyHoldTime += Time.deltaTime;
                
                // Mở giao diện Emote sau thời gian Hold
                if (_tKeyHoldTime >= EMOTE_HOLD_THRESHOLD)
                {
                    if (EmoteUIManager.Instance != null && !EmoteUIManager.IsEmoteMenuOpen)
                    {
                        Debug.Log("[PlayerController] Emote Threshold Reached! Opening Radial Menu...");
                        EmoteUIManager.Instance.OpenRadialMenu();
                    }
                    else if (EmoteUIManager.Instance == null)
                    {
                        Debug.LogWarning("[PlayerController] EmoteUIManager.Instance is NULL! Vui lòng chạy Tool Setup lại.");
                    }
                }
            }

            if (kb.tKey.wasReleasedThisFrame)
            {
                if (_tKeyHeld && _tKeyHoldTime < EMOTE_HOLD_THRESHOLD && !_inventoryOpen && !isDialoguing)
                {
                    // Lặp lại Emote cũ
                    if (EmoteUIManager.Instance != null && !EmoteUIManager.IsEmoteMenuOpen)
                    {
                        EmoteUIManager.Instance.PlayLastEmote();
                    }
                }

                _tKeyHeld = false;
                _tKeyHoldTime = 0f;

                // Đóng menu nếu đang bật
                if (EmoteUIManager.Instance != null && EmoteUIManager.IsEmoteMenuOpen)
                {
                    EmoteUIManager.Instance.CloseRadialMenu();
                }
            }

            // Disable movement khi inventory mở, hội thoại, HOẶC ĐANG MỞ EMOTE MENU
            bool isMenuOpened = _inventoryOpen || isDialoguing || EmoteUIManager.IsEmoteMenuOpen;
            if (!isMenuOpened)
            {
                // Hotbar selection (1-9)
                HandleHotbarInput(kb);
                
                // Throw item (left mouse)
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame)
                {
                    TryThrowItem();
                }
                
                float h = (kb.dKey.isPressed ? 1f : 0f) + (kb.aKey.isPressed ? -1f : 0f);
                float v = (kb.wKey.isPressed ? 1f : 0f) + (kb.sKey.isPressed ? -1f : 0f);
                _moveInput = new Vector3(h, 0f, v).normalized;
                _isRunning = kb.leftShiftKey.isPressed;

                if (kb.spaceKey.wasPressedThisFrame && _isGrounded)
                {
                    Jump();
                }

                // --- Hủy Emote khi nhấn di chuyển (Có hướng đi) hoặc Nhảy ---
                if (_moveInput.sqrMagnitude > 0.01f || _isJumping)
                {
                    if (EmoteUIManager.Instance != null)
                    {
                        EmoteUIManager.Instance.CancelEmote();
                    }
                }

                // --- Tiếng bước chân ---
                HandleFootsteps();
            }
            else
            {
                _moveInput = Vector3.zero;
                _isRunning = false;
            }
        }

        if (_isGrounded && _verticalVelocity <= 0)
        {
            _verticalVelocity = -1f;
            if (_isJumping)
            {
                _isJumping = false;
                if (animator != null)
                {
                    animator.SetBool("IsJumping", false);
                    animator.SetBool("Jump", false);
                }
            }
        }
        else if (!_isGrounded)
        {
            _verticalVelocity += gravity * Time.deltaTime;
        }

        UpdateAnimator();
    }

    void Jump()
    {
        _verticalVelocity = jumpForce;
        _isJumping = true;

        if (animator != null)
        {
            animator.SetBool("Jump", true);
            animator.SetBool("IsJumping", true); 
        }
    }

    void UpdateAnimator()
    {
        if (animator == null) return;

        // QUAN TRỌNG: Giải quyết lỗi rung rắc camera (Root motion vs Code)
        // Vì hiện tại nhân vật đã dồn toàn thân xoay mặt về đích (Genshin Style), 
        // chiều chuyển động cục bộ của cơ thể luôn là "Đi thẳng về phía trước".
        // Ta không cho phép Animator kích hoạt dáng "Đi lùi / Bước ngang" nữa,
        // nếu không Root Motion của animation đi lùi sẽ giật ngược lại đường đi của script.
        
        float moveMagnitude = _moveInput.sqrMagnitude > 0.01f ? 1f : 0f;
        
        animator.SetFloat("MoveX", 0f);
        animator.SetFloat("MoveY", moveMagnitude);
        
        float speedVal = (moveMagnitude > 0.01f) ? (_isRunning ? 1f : 0.5f) : 0f;
        animator.SetFloat("Speed", speedVal);
        
        if (_isGrounded && !_isJumping) {
             animator.SetBool("Jump", false);
             animator.SetBool("IsJumping", false);
        }
    }

    private void FixedUpdate()
    {
        // 1. Tính toán hướng di chuyển dựa trên Camera
        Vector3 moveDir = Vector3.zero;
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            moveDir = (camForward * _moveInput.z + camRight * _moveInput.x);
        }
        else
        {
            moveDir = (transform.forward * _moveInput.z + transform.right * _moveInput.x);
            moveDir.y = 0f;
        }

        if (moveDir.sqrMagnitude > 0.01f) moveDir.Normalize();

        // 2. Tính toán vận tốc
        float speed = _isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;
        Vector3 horizontalMove = moveDir * speed * Time.fixedDeltaTime;
        Vector3 verticalMove = Vector3.up * _verticalVelocity * Time.fixedDeltaTime;

        // 3. Xoay nhân vật mượt mà theo hướng di chuyển (Nếu có bấm nút di chuyển)
        if (moveDir.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDir);
            // Tốc độ xoay (Lerp) 15f có thể điều chỉnh cho mượt hơn
            rb.MoveRotation(Quaternion.Slerp(transform.rotation, targetRotation, 15f * Time.fixedDeltaTime));
        }

        // 4. Di chuyển vị trí vật lý
        rb.MovePosition(rb.position + horizontalMove + verticalMove);
    }

    /// <summary>
    /// Raycast để detect pickable items
    /// </summary>
    void DetectPickableItems()
    {
        if (cameraTransform == null)
        {
            Debug.LogWarning("PlayerController: Camera Transform chưa được assign!");
            return;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        
        PickableItem previousItem = _currentLookingItem;

        // Vì Camera ở góc nhìn thứ 3 nằm tít sau lưng nhân vật, tia quét phải đủ dài để vượt qua nhân vật
        float maxRayDistance = Vector3.Distance(cameraTransform.position, transform.position) + interactionRange;

        // Use RaycastAll to detect ALL colliders including triggers
        // Cần truyền QueryTriggerInteraction.Collide để quét trúng Càn Khôn Đại Na Di (bên trong Trigger) của Tool ItemSpawner
        RaycastHit[] hits = Physics.RaycastAll(ray, maxRayDistance, Physics.AllLayers, QueryTriggerInteraction.Collide);
        
        PickableItem closestItem = null;
        float closestHitDistance = float.MaxValue;
        
        // Find closest PickableItem
        foreach (var hit in hits)
        {
            PickableItem item = hit.collider.GetComponentInParent<PickableItem>();
            if (item != null)
            {
                // Chỉ nhặt được nếu vật thể nằm trong tầm với CỦA NHÂN VẬT (không phải camera)
                float distanceToPlayer = Vector3.Distance(transform.position, hit.point);
                if (distanceToPlayer <= interactionRange && hit.distance < closestHitDistance)
                {
                    closestItem = item;
                    closestHitDistance = hit.distance;
                }
            }
        }
        
        _currentLookingItem = closestItem;

        // Update highlight and prompt
        if (_currentLookingItem != null)
        {
            _currentLookingItem.Highlight(true);
            
            if (PickupPromptUI.Instance != null && _currentLookingItem.itemData != null)
            {
                PickupPromptUI.Instance.ShowPrompt(_currentLookingItem.itemData.itemName);
            }
        }

        // Remove highlight from previous item
        if (previousItem != null && previousItem != _currentLookingItem)
        {
            previousItem.Highlight(false);
            if (PickupPromptUI.Instance != null)
            {
                PickupPromptUI.Instance.HidePrompt();
            }
        }

        // Hide prompt if no item
        if (_currentLookingItem == null && PickupPromptUI.Instance != null)
        {
            PickupPromptUI.Instance.HidePrompt();
        }
    }

    /// <summary>
    /// Thử nhặt item hiện tại
    /// </summary>
    void TryPickupItem()
    {
        if (_currentLookingItem == null) return;

        bool success = _currentLookingItem.TryPickup();
        if (success)
        {
            _currentLookingItem = null;
            if (PickupPromptUI.Instance != null)
            {
                PickupPromptUI.Instance.HidePrompt();
            }
        }
    }
    
    /// <summary>
    /// Handle hotbar input (keys 1-9)
    /// </summary>
    void HandleHotbarInput(Keyboard kb)
    {
        var hotbarUI = FindFirstObjectByType<HotbarUI>();
        if (hotbarUI == null) return;
        
        if (kb.digit1Key.wasPressedThisFrame) hotbarUI.SelectSlot(0);
        else if (kb.digit2Key.wasPressedThisFrame) hotbarUI.SelectSlot(1);
        else if (kb.digit3Key.wasPressedThisFrame) hotbarUI.SelectSlot(2);
        else if (kb.digit4Key.wasPressedThisFrame) hotbarUI.SelectSlot(3);
        else if (kb.digit5Key.wasPressedThisFrame) hotbarUI.SelectSlot(4);
        else if (kb.digit6Key.wasPressedThisFrame) hotbarUI.SelectSlot(5);
        else if (kb.digit7Key.wasPressedThisFrame) hotbarUI.SelectSlot(6);
        else if (kb.digit8Key.wasPressedThisFrame) hotbarUI.SelectSlot(7);
        else if (kb.digit9Key.wasPressedThisFrame) hotbarUI.SelectSlot(8);
    }
    
    /// <summary>
    /// Try to throw equipped item
    /// </summary>
    void TryThrowItem()
    {
        if (playerEquipment == null)
        {
            playerEquipment = GetComponent<PlayerEquipment>();
        }
        
        if (playerEquipment == null || !playerEquipment.HasEquippedItem)
        {
            return;
        }
        
        // Get throw direction (camera forward)
        Vector3 throwDirection = cameraTransform != null ? 
            cameraTransform.forward : 
            transform.forward;
        
        playerEquipment.ThrowItem(throwDirection);
    }

    /// <summary>
    /// Phát tiếng bước chân khi player di chuyển trên mặt đất
    /// </summary>
    void HandleFootsteps()
    {
        bool isMoving = _moveInput.sqrMagnitude > 0.01f && _isGrounded && !isDialoguing;
        if (!isMoving)
        {
            _stepTimer = 0f;
            return;
        }

        _stepTimer += Time.deltaTime;
        float interval = _isRunning ? runStepInterval : walkStepInterval;

        if (_stepTimer >= interval)
        {
            _stepTimer = 0f;
            PlayFootstep();
        }
    }

    void PlayFootstep()
    {
        if (_footstepSource == null || footstepClips == null || footstepClips.Length == 0) return;
        AudioClip clip = footstepClips[Random.Range(0, footstepClips.Length)];
        if (clip != null)
            _footstepSource.PlayOneShot(clip, footstepVolume);
    }
}
