using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField] Animator animator;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runSpeedMultiplier = 3f;
    [SerializeField] float jumpForce = 8f;
    [SerializeField] float gravity = -25f;
    [SerializeField] Rigidbody rb;

    Vector3 _moveInput;
    bool _isRunning;
    float _verticalVelocity;
    bool _isGrounded;
    bool _isJumping;
    int _groundContactCount;

    void Awake()
    {
        // Tự gán Animator nếu chưa gán trong Inspector (tránh lỗi "Animator has not been initialized")
        if (animator == null)
        {
            animator = GetComponent<Animator>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }
    }

    void Start()
    {
        if (rb != null)
        {
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }
    }

    void Update()
    {
        // Ground = có ít nhất 1 contact với mặt đất
        _isGrounded = _groundContactCount > 0;

        var kb = Keyboard.current;
        if (kb != null)
        {
            // Input di chuyển
            float h = (kb.dKey.isPressed ? 1f : 0f) + (kb.aKey.isPressed ? -1f : 0f);
            float v = (kb.wKey.isPressed ? 1f : 0f) + (kb.sKey.isPressed ? -1f : 0f);
            _moveInput = new Vector3(h, 0f, v).normalized;

            _isRunning = kb.leftShiftKey.isPressed;
            
            // Nhảy - chỉ khi đang chạm đất và không đang nhảy
            if (kb.spaceKey.wasPressedThisFrame && _isGrounded && !_isJumping)
            {
                Jump();
            }
        }

        // Áp dụng gravity
        if (_isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = 0f;
        }
        else
        {
            _verticalVelocity += gravity * Time.deltaTime;
            _verticalVelocity = Mathf.Max(_verticalVelocity, -30f);
        }

        // Reset trạng thái nhảy khi chạm đất
        if (_isGrounded && _isJumping && _verticalVelocity <= 0)
        {
            _isJumping = false;
        }

        UpdateAnimator();
    }

    void Jump()
    {
        _verticalVelocity = jumpForce;
        _isJumping = true;
        _groundContactCount = 0;  // Force không chạm đất khi nhảy
        
        if (animator != null)
        {
            animator.SetTrigger("Jump");
            animator.SetBool("IsJumping", true);
        }
        
        Debug.Log("JUMP!");
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        
        // Khi chạm đất và không nhảy
        if (_isGrounded && !_isJumping)
        {
            animator.SetBool("IsJumping", false);
            animator.ResetTrigger("Jump");
            
            // Force về Blend Tree nếu bị stuck ở Jump state
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Jump"))
            {
                animator.Play("Locomotion", 0, 0f);
            }
        }
        
        // Set các giá trị di chuyển - luôn hoạt động
        animator.SetFloat("MoveX", _moveInput.x);
        animator.SetFloat("MoveY", _moveInput.z);
        animator.SetFloat("Speed", (_isRunning && _moveInput.sqrMagnitude > 0.01f) ? 1f : 0f);
    }

    private void FixedUpdate()
    {
        // Di chuyển ngang
        Vector3 moveDir = (transform.forward * _moveInput.z + transform.right * _moveInput.x);
        moveDir.y = 0f;
        if (moveDir.sqrMagnitude > 0.01f)
            moveDir.Normalize();

        float speed = _isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;
        Vector3 horizontalMove = moveDir * speed * Time.fixedDeltaTime;
        Vector3 verticalMove = Vector3.up * _verticalVelocity * Time.fixedDeltaTime;

        rb.MovePosition(rb.position + horizontalMove + verticalMove);
    }

    // Khi bắt đầu chạm collider
    void OnCollisionEnter(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            // Nếu normal hướng lên = đang đứng trên vật
            if (contact.normal.y > 0.5f)
            {
                _groundContactCount++;
                Debug.Log($"Ground ENTER: {collision.gameObject.name}, count={_groundContactCount}");
                break;
            }
        }
    }

    // Khi rời collider
    void OnCollisionExit(Collision collision)
    {
        _groundContactCount--;
        if (_groundContactCount < 0) _groundContactCount = 0;
        Debug.Log($"Ground EXIT: {collision.gameObject.name}, count={_groundContactCount}");
    }
}
