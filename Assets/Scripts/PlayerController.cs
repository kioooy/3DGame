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

    Vector3 _moveInput;
    bool _isRunning;
    float _verticalVelocity;
    bool _isGrounded;
    bool _isJumping;
    
    // Interaction
    private PickableItem _currentLookingItem;
    private bool _inventoryOpen = false;
    private bool _hasLoggedItemDataWarning = false;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        if (cameraTransform == null)
            cameraTransform = Camera.main?.transform;
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
    }

    void Update()
    {
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

            // Pickup item với E
            if (kb.eKey.wasPressedThisFrame && _currentLookingItem != null && !_inventoryOpen)
            {
                TryPickupItem();
            }

            // Disable movement khi inventory mở
            if (!_inventoryOpen)
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

        animator.SetFloat("MoveX", _moveInput.x);
        animator.SetFloat("MoveY", _moveInput.z);
        
        float speedVal = (_moveInput.sqrMagnitude > 0.01f) ? (_isRunning ? 1f : 0.5f) : 0f;
        animator.SetFloat("Speed", speedVal);
        
        if (_isGrounded && !_isJumping) {
             animator.SetBool("Jump", false);
             animator.SetBool("IsJumping", false);
        }
    }

    private void FixedUpdate()
    {
        Vector3 moveDir = (transform.forward * _moveInput.z + transform.right * _moveInput.x);
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.01f) moveDir.Normalize();

        float speed = _isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;
        Vector3 horizontalMove = moveDir * speed * Time.fixedDeltaTime;
        Vector3 verticalMove = Vector3.up * _verticalVelocity * Time.fixedDeltaTime;

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

        // Use RaycastAll to detect ALL colliders including triggers
        RaycastHit[] hits = Physics.RaycastAll(ray, interactionRange);
        
        PickableItem closestItem = null;
        float closestDistance = float.MaxValue;
        
        // Find closest PickableItem
        foreach (var hit in hits)
        {
            PickableItem item = hit.collider.GetComponent<PickableItem>();
            if (item != null && hit.distance < closestDistance)
            {
                closestItem = item;
                closestDistance = hit.distance;
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
}
