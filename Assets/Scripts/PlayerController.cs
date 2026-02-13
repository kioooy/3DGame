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

    [Header("Interaction Settings")]
    [SerializeField] float interactionRange = 3.5f;
    [SerializeField] LayerMask itemLayer;
    [SerializeField] Transform cameraTransform;

    Vector3 _moveInput;
    bool _isRunning;
    float _verticalVelocity;
    bool _isGrounded;
    bool _isJumping;
    
    // Interaction
    private PickableItem _currentLookingItem;
    private bool _inventoryOpen = false;

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
        _isGrounded = Physics.Raycast(transform.position + groundCheckOffset, Vector3.down, groundCheckDistance);

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
        RaycastHit hit;

        PickableItem previousItem = _currentLookingItem;

        // Raycast với hoặc không có layer mask
        bool hitSomething = false;
        if (itemLayer.value != 0)
        {
            hitSomething = Physics.Raycast(ray, out hit, interactionRange, itemLayer);
        }
        else
        {
            // Nếu chưa setup layer, raycast tất cả
            hitSomething = Physics.Raycast(ray, out hit, interactionRange);
        }

        if (hitSomething)
        {
            PickableItem item = hit.collider.GetComponent<PickableItem>();
            if (item != null)
            {
                _currentLookingItem = item;
                item.Highlight(true);

                // Show pickup prompt
                if (PickupPromptUI.Instance != null && item.itemData != null)
                {
                    PickupPromptUI.Instance.ShowPrompt(item.itemData.itemName);
                }
                else
                {
                    if (item.itemData == null)
                    {
                        Debug.LogWarning($"PickableItem '{item.gameObject.name}' không có ItemData!");
                    }
                    if (PickupPromptUI.Instance == null)
                    {
                        Debug.LogWarning("PickupPromptUI.Instance is null! Make sure PickupPromptUI exists in the scene.");
                    }
                }
            }
            else
            {
                _currentLookingItem = null;
            }
        }
        else
        {
            _currentLookingItem = null;
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
}
