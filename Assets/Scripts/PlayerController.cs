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

    Vector3 _moveInput;
    bool _isRunning;
    float _verticalVelocity;
    bool _isGrounded;
    bool _isJumping;

    void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        if (rb == null)
            rb = GetComponent<Rigidbody>();
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

        var kb = Keyboard.current;
        if (kb != null)
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
}
