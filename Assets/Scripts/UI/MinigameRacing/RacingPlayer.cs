using UnityEngine;
using UnityEngine.InputSystem;

public class RacingPlayer : MonoBehaviour
{
    [Header("Movement Settings")]
    public float accelerationPerPress = 1.0f; // Tăng tốc độ mỗi lần bấm đúng
    public float maxSpeed = 12f; // Vận tốc tối đa
    public float speedDecay = 3f; // Tốc độ trôi tuột nếu ngừng ấn
    public float jumpForce = 6f; // Lực nhảy (giả lập)
    public float gravity = 15f; // Trọng lực (giả lập)
    public float yGroundLevel = 1f; // Độ cao chạm đất (mặc định của Capsule)

    public Animator animator;
    public string speedFloat = "Speed";
    public string moveYFloat = "MoveY";
    public string jumpBool = "Jump";
    public string isJumpingBool = "IsJumping";

    private bool _hasSpeed;
    private bool _hasMoveY;
    private bool _hasJump;
    private bool _hasIsJumping;
    private bool _hasIsRunning;

    private float _currentSpeed = 0f;
    private bool _canRun = false;
    private int _lastKeyPressed = 0; // 0=None, 1=Left, 2=Right

    private bool _isJumping = false;
    private float _yVelocity = 0f;

    public void EnableRunning(bool state)
    {
        _canRun = state;
        if (!state)
        {
            _currentSpeed = 0f;
            _lastKeyPressed = 0;
            if (animator != null) 
            {
                if (_hasSpeed) animator.SetFloat(speedFloat, 0f);
                if (_hasMoveY) animator.SetFloat(moveYFloat, 0f);
                if (_hasIsRunning) animator.SetBool("IsRunning", false);
            }
        }
    }

    void Awake()
    {
        if (animator == null) 
            animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        // Nhớ vị trí Y gốc để biết đâu là mặt đất
        yGroundLevel = transform.position.y;
        
        if (animator != null)
        {
            foreach (var param in animator.parameters)
            {
                if (param.name == speedFloat) _hasSpeed = true;
                if (param.name == moveYFloat) _hasMoveY = true;
                if (param.name == jumpBool) _hasJump = true;
                if (param.name == isJumpingBool) _hasIsJumping = true;
                if (param.name == "IsRunning") _hasIsRunning = true;
            }
        }
    }

    void Update()
    {
        if (!_canRun) return;

        var kb = Keyboard.current;
        if (kb != null)
        {
            // Nhảy
            if (kb.spaceKey.wasPressedThisFrame && !_isJumping)
            {
                _isJumping = true;
                _yVelocity = jumpForce;
                if (animator != null) 
                {
                    if (_hasJump) animator.SetBool(jumpBool, true);
                    if (_hasIsJumping) animator.SetBool(isJumpingBool, true);
                }
            }

            // Luân phiên Trái Phải
            bool pressedLeft = kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame;
            bool pressedRight = kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame;

            if (pressedLeft && _lastKeyPressed != 1)
            {
                _currentSpeed += accelerationPerPress;
                _lastKeyPressed = 1;
            }
            else if (pressedRight && _lastKeyPressed != 2)
            {
                _currentSpeed += accelerationPerPress;
                _lastKeyPressed = 2;
            }

            if (_currentSpeed > maxSpeed)
            {
                _currentSpeed = maxSpeed;
            }
        }

        // Tự động suy giảm đà nếu ngừng ấn
        if (_currentSpeed > 0)
        {
            _currentSpeed -= speedDecay * Time.deltaTime;
            if (_currentSpeed < 0) _currentSpeed = 0;
        }

        // ====== DI CHUYỂN Z ======
        transform.position += transform.forward * _currentSpeed * Time.deltaTime;

        // ====== DI CHUYỂN Y (JUMP) ======
        if (_isJumping)
        {
            _yVelocity -= gravity * Time.deltaTime;
            Vector3 pos = transform.position;
            pos.y += _yVelocity * Time.deltaTime;

            // Chạm đất
            if (pos.y <= yGroundLevel)
            {
                pos.y = yGroundLevel;
                _isJumping = false;
                _yVelocity = 0;
                if (animator != null)
                {
                    if (_hasJump) animator.SetBool(jumpBool, false);
                    if (_hasIsJumping) animator.SetBool(isJumpingBool, false);
                }
            }
            transform.position = pos;
        }

        // Cập nhật Animation
        if (animator != null && !_isJumping)
        {
            float animSpeed = _currentSpeed > 0.1f ? 1f : 0f;
            if (_hasSpeed) animator.SetFloat(speedFloat, animSpeed);
            if (_hasMoveY) animator.SetFloat(moveYFloat, animSpeed);
            if (_hasIsRunning) animator.SetBool("IsRunning", _currentSpeed > 0.1f);
        }

        // Xử lý va chạm chướng ngại vật sài OnTriggerEnter thay vì Raycast trong hàm Update
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("Obstacle"))
        {
            // Kiểm tra cao độ: Nếu đang nhảy cao hơn rào chắn thì bỏ qua
            if (transform.position.y > other.transform.position.y + 0.5f) return;

            // Vấp chướng ngại vật => mất đà
            _currentSpeed = 0f;
            
            // Xóa rào chắn đi để khỏi đụng lại (Vỡ rào)
            other.gameObject.SetActive(false);
        }
    }
}
