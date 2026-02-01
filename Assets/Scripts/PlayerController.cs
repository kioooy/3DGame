using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runSpeedMultiplier = 3f;  // Shift = chạy nhanh gấp 3
    [SerializeField] float jumpForce = 7f;
    [SerializeField] float gravity = -20f;
    [SerializeField] Rigidbody rb;
    [SerializeField] LayerMask groundLayer = ~0;
    [SerializeField] float groundCheckOffset = 0.1f;    // Điểm bắt đầu raycast (trên chân)
    [SerializeField] float groundCheckLength = 0.5f;    // Độ dài raycast xuống đất

    Vector3 _moveInput;
    bool _isRunning;
    bool _wantsToJump;
    float _verticalVelocity;
    bool _isGrounded;
    bool _wasGrounded;  // Trạng thái ground frame trước
    bool _triggerJump;  // Flag để trigger animation jump
    bool _justLanded;   // Vừa tiếp đất

    void Start()
    {
        if (rb != null)
            rb.useGravity = false;  // Tự xử lý trọng lực trong script
    }

    void Update()
    {
        // Check ground trong Update để animator có giá trị chính xác ngay lập tức
        _isGrounded = IsGrounded();
        
        var kb = Keyboard.current;
        if (kb != null)
        {
            float h = (kb.dKey.isPressed ? 1f : 0f) + (kb.aKey.isPressed ? -1f : 0f);
            float v = (kb.wKey.isPressed ? 1f : 0f) + (kb.sKey.isPressed ? -1f : 0f);
            _moveInput = new Vector3(h, 0f, v).normalized;

            _isRunning = kb.leftShiftKey.isPressed;
            
            // Xử lý nhảy ngay trong Update để không bị delay
            if (kb.spaceKey.wasPressedThisFrame && _isGrounded)
            {
                _wantsToJump = true;
                _triggerJump = true;  // Set flag để trigger animation
            }
        }

        // Phát hiện khi vừa tiếp đất
        _justLanded = _isGrounded && !_wasGrounded;
        
        // Set animator trong Update để đồng bộ với Animator
        UpdateAnimator();
        
        // Lưu trạng thái ground cho frame sau
        _wasGrounded = _isGrounded;
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        
        // Khi vừa tiếp đất, force chuyển về Blend Tree state
        if (_justLanded)
        {
            animator.ResetTrigger("Jump");
            animator.SetBool("IsJumping", false);
            // Force play Blend Tree state
            animator.Play("Blend Tree", 0, 0f);
            Debug.Log("Just landed - forcing Blend Tree state");
        }
        
        // Luôn set các giá trị di chuyển - để blend tree hoạt động đúng
        animator.SetFloat("MoveX", _moveInput.x);
        animator.SetFloat("MoveY", _moveInput.z);
        animator.SetFloat("Speed", (_isRunning && _moveInput.sqrMagnitude > 0.01f) ? 1f : 0f);
        
        animator.SetBool("IsJumping", !_isGrounded);
        
        // Trigger jump animation
        if (_triggerJump)
        {
            animator.SetTrigger("Jump");
            _triggerJump = false;
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
        // Xử lý nhảy và trọng lực (tự quản lý, tránh xung đột với Rigidbody)
        if (_isGrounded)
        {
            if (_wantsToJump)
            {
                _verticalVelocity = jumpForce;
                _wantsToJump = false;
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

    // Debug: Vẽ raycast trong Scene view
    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawLine(origin, origin + Vector3.down * groundCheckLength);
    }
}
