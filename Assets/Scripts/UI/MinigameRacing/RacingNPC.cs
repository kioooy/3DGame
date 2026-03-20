using UnityEngine;

public class RacingNPC : MonoBehaviour
{
    [Header("Movement Settings")]
    public float baseSpeed = 4f; 
    public float speedVariation = 2f; // Tốc độ có thể cộng trừ ngẫu nhiên
    public float variationChangeInterval = 1.5f; // Bao lâu thì đổi tốc độ 1 lần

    [Header("Jump Settings")]
    public float jumpForce = 6f; // Lực nhảy (giả lập)
    public float gravity = 15f; // Trọng lực (giả lập)
    public float yGroundLevel = 1f; // Độ cao chạm đất
    public float jumpDetectDistance = 2.5f; // Khoảng cách nhìn thấy rào cản để nhảy

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
    private float _targetSpeed = 0f;
    private float _timer = 0f;
    private bool _canRun = false;

    private bool _isJumping = false;
    private float _yVelocity = 0f;

    [Header("Audio Settings")]
    public AudioClip[] footstepSFX;
    public AudioClip impactSFX;
    public AudioClip jumpSFX;
    private AudioSource _audioSource;
    private float _footstepTimer = 0f;
    private float _footstepInterval = 0.35f; // NPC bước chân thưa hơn xíu cho đỡ loạn

    public void EnableRunning(bool state)
    {
        _canRun = state;
        if (state)
        {
            _targetSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        }
        else
        {
            _currentSpeed = 0f;
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
        _audioSource.maxDistance = 20f; // Để NPC ở xa không nghe quá rõ
    }

    void Start()
    {
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

        // Thay đổi tốc độ liên tục để tạo cảm giác hụt hơi / tăng tốc
        _timer += Time.deltaTime;
        if (_timer >= variationChangeInterval)
        {
            _timer = 0f;
            _targetSpeed = baseSpeed + Random.Range(-speedVariation, speedVariation);
        }

        // Lerp vận tốc cho mượt
        _currentSpeed = Mathf.Lerp(_currentSpeed, _targetSpeed, Time.deltaTime * 2f);

        // Di chuyển
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

            // Audio: NPC Footsteps
            if (isMoving && footstepSFX != null && footstepSFX.Length > 0)
            {
                _footstepTimer -= Time.deltaTime;
                if (_footstepTimer <= 0)
                {
                    // NPC phát tiếng chân nhỏ hơn
                    _audioSource.PlayOneShot(footstepSFX[Random.Range(0, footstepSFX.Length)], 0.3f);
                    _footstepTimer = _footstepInterval;
                }
            }
        }

        // ====== AI NHẢY QUA RÀO ======
        CheckObstaclesAndJump();
    }

    private void CheckObstaclesAndJump()
    {
        Vector3 origin = transform.position + Vector3.up * 0.5f; 
        
        // Raycast nhìn xa hơn Player để chuẩn bị nhảy
        if (!_isJumping && Physics.Raycast(origin, transform.forward, out RaycastHit hit, jumpDetectDistance))
        {
            if (hit.collider.name.Contains("Obstacle"))
            {
                // Thấy rào cản thì tự động nhảy
                _isJumping = true;
                _yVelocity = jumpForce;
                if (animator != null) 
                {
                    if (_hasJump) animator.SetBool(jumpBool, true);
                    if (_hasIsJumping) animator.SetBool(isJumpingBool, true);
                }

                // Phát tiếng nhảy cho NPC
                if (jumpSFX != null && _audioSource != null)
                {
                    _audioSource.PlayOneShot(jumpSFX, 0.4f); // NPC nhỏ hơn
                }
            }
        }

        // Lỡ đụng rào (do tốc độ trễ hoặc lỗi AI) -> Được xử lý chắc chắn hơn bên trong OnTriggerEnter
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("Obstacle"))
        {
            // Debug để kiểm tra tên object và vị trí NPC
            Debug.Log($"[RacingNPC] Va chạm với: {other.name} tại Y={transform.position.y}, Obstacle Y={other.transform.position.y}");

            // Nếu đang nhảy cao hẳn qua rào thì mới bỏ qua
            if (_isJumping && transform.position.y > other.transform.position.y + 0.6f) return;

            // Vấp chướng ngại vật => té
            _currentSpeed = 0f;
            
            // Âm thanh va chạm
            if (impactSFX != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(impactSFX, 0.5f); // NPC phát nhỏ hơn
                Debug.Log("[RacingNPC] Phát âm thanh va chạm!");
            }

            // Xóa rào chắn đi để khỏi đụng lại (Vỡ rào)
            other.gameObject.SetActive(false);
        }
    }
}
