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
            }
        }

        // Lỡ đụng rào (do tốc độ trễ hoặc lỗi AI) -> Được xử lý chắc chắn hơn bên trong OnTriggerEnter
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.name.Contains("Obstacle"))
        {
            // Kiểm tra cao độ: Nếu đang nhảy cao hơn rào chắn thì bỏ qua
            if (transform.position.y > other.transform.position.y + 0.5f) return;

            // Vấp chướng ngại vật => té
            _currentSpeed = 0f;
            
            // Xóa rào chắn đi để khỏi đụng lại (Vỡ rào)
            other.gameObject.SetActive(false);
        }
    }
}
