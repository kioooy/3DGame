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

    [Header("Audio Settings")]
    public AudioClip[] footstepSFX;
    public AudioClip impactSFX;
    public AudioClip jumpSFX;
    private AudioSource _audioSource;
    private float _footstepTimer = 0f;
    private float _footstepInterval = 0.3f;

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
            
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 1.0f; // 3D Sound
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
                
                // Phát tiếng nhảy
                if (jumpSFX != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(jumpSFX, 0.7f);
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
                    
                    // FORCE chuyển sang trạng thái chạy ngay lập tức để cắt ngắn animation nhảy
                    // Sử dụng CrossFadeInFixedTime(0) để ghi đè tức thì. Tên state chính xác là "Run".
                    animator.CrossFadeInFixedTime("Run", 0f);
                    animator.Update(0); // Buộc Animator cập nhật ngay lập tức trong frame này
                    
                    // Cập nhật luôn các thông số chạy
                    bool isMoving = _currentSpeed > 0.1f;
                    float animSpeed = isMoving ? 1f : 0f;
                    if (_hasSpeed) animator.SetFloat(speedFloat, animSpeed);
                    if (_hasIsRunning) animator.SetBool("IsRunning", isMoving);
                }
            }
            transform.position = pos;
        }

        // Cập nhật Animation & Footsteps
        if (animator != null && !_isJumping)
        {
            bool isMoving = _currentSpeed > 0.1f;
            float animSpeed = isMoving ? 1f : 0f;
            if (_hasSpeed) animator.SetFloat(speedFloat, animSpeed);
            if (_hasMoveY) animator.SetFloat(moveYFloat, animSpeed);
            if (_hasIsRunning) animator.SetBool("IsRunning", isMoving);

            // Audio: Footsteps
            if (isMoving && footstepSFX != null && footstepSFX.Length > 0)
            {
                _footstepTimer -= Time.deltaTime;
                if (_footstepTimer <= 0)
                {
                    _audioSource.PlayOneShot(footstepSFX[Random.Range(0, footstepSFX.Length)], 0.5f);
                    // Tốc độ chân tỉ lệ với tốc độ chạy
                    _footstepTimer = Mathf.Max(0.15f, _footstepInterval - (_currentSpeed / maxSpeed) * 0.15f);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("Obstacle"))
        {
            // Debug để kiểm tra tên object và vị trí
            Debug.Log($"[RacingPlayer] Va chạm với: {other.name} tại Y={transform.position.y}, Obstacle Y={other.transform.position.y}");

            // Nếu đang nhảy thật sự cao hẳn qua rào thì mới bỏ qua
            // Thích nghi: Rào thường có scale Y=1, base Y=0 -> đỉnh rào là 0.5
            // Nếu chân player (pos.y) > 0.6 thì coi như qua
            if (_isJumping && transform.position.y > other.transform.position.y + 0.6f) 
            {
                Debug.Log("[RacingPlayer] Bỏ qua va chạm vì đang nhảy cao.");
                return;
            }

            // Vấp chướng ngại vật => mất đà
            _currentSpeed = 0f;
            
            // Âm thanh va chạm
            if (impactSFX != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(impactSFX, 0.9f);
                Debug.Log("[RacingPlayer] Phát âm thanh va chạm!");
            }

            // Xóa rào chắn đi để khỏi đụng lại (Vỡ rào)
            other.gameObject.SetActive(false);
        }
    }
}
