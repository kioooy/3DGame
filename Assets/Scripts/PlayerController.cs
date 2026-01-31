using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float moveSpeed;
    [SerializeField] float runSpeedMultiplier = 3f;  // Shift = chạy nhanh gấp 3
    [SerializeField] float jumpForce = 6f;
    [SerializeField] float gravity = -20f;
    [SerializeField] Rigidbody rb;
    [SerializeField] LayerMask groundLayer = ~0;
    [SerializeField] float groundCheckOffset = 0.1f;    // Điểm bắt đầu raycast (trên chân)
    [SerializeField] float groundCheckLength = 1.2f;    // Độ dài raycast xuống đất (từ trung tâm đến chân)

    Vector3 _moveInput;
    bool _isRunning;
    bool _wantsToJump;
    float _verticalVelocity;
    bool _isGrounded;
    bool _justJumped;

    void Start()
    {
        if (rb != null)
            rb.useGravity = false;  // Tự xử lý trọng lực trong script
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            float h = (kb.dKey.isPressed ? 1f : 0f) + (kb.aKey.isPressed ? -1f : 0f);
            float v = (kb.wKey.isPressed ? 1f : 0f) + (kb.sKey.isPressed ? -1f : 0f);
            _moveInput = new Vector3(h, 0f, v).normalized;

            _isRunning = kb.leftShiftKey.isPressed;
            if (kb.spaceKey.wasPressedThisFrame)
                _wantsToJump = true;
        }

        // Set animator trong Update để đồng bộ với Animator (chạy trước Animator)
        if (animator != null)
        {
            animator.SetFloat("MoveX", _moveInput.x);
            animator.SetFloat("MoveY", _moveInput.z);
            animator.SetFloat("Speed", (_isRunning && _moveInput.sqrMagnitude > 0.01f) ? 1f : 0f);
            animator.SetBool("IsJumping", !_isGrounded);
            if (_justJumped)
            {
                animator.SetTrigger("Jump");
                _justJumped = false;
            }
        }
    }

    bool IsGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, groundCheckLength, groundLayer);
        foreach (var hit in hits)
        {
            // Bỏ qua nếu trúng chính nhân vật
            if (hit.collider.transform.root == transform.root)
                continue;
            return true;
        }
        return false;
    }

    private void FixedUpdate()
    {
        _isGrounded = IsGrounded();

        // Xử lý nhảy và trọng lực (tự quản lý, tránh xung đột với Rigidbody)
        if (_isGrounded)
        {
            if (_wantsToJump)
            {
                _verticalVelocity = jumpForce;
                _wantsToJump = false;
                _justJumped = true;
            }
            else
            {
                _verticalVelocity = 0f;   // Đứng yên trên mặt đất
            }
        }
        else
        {
            _wantsToJump = false;
            _verticalVelocity += gravity * Time.fixedDeltaTime;
        }

        // Di chuyển ngang
        Vector3 moveDir = (transform.forward * _moveInput.z + transform.right * _moveInput.x);
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.01f)
            moveDir.Normalize();

        float speed = _isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;
        Vector3 moveDelta = (moveDir * speed + Vector3.up * _verticalVelocity) * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + moveDelta);
    }
}
