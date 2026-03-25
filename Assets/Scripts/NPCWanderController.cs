using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Cho phép NPC tự di chuyển lang thang trong một bán kính quanh vị trí ban đầu.
/// Hoạt động với Rigidbody hoặc CharacterController (hoặc cả hai đều không có).
/// Gắn component này vào bất kỳ NPC GameObject nào thông qua NPCWanderTool.
/// </summary>
public class NPCWanderController : MonoBehaviour
{
    [Header("Phạm Vi Lang Thang")]
    [Tooltip("Bán kính tối đa NPC có thể đi từ vị trí ban đầu (m)")]
    public float wanderRadius = 6f;

    [Header("Tốc Độ")]
    [Tooltip("Tốc độ di chuyển (m/s)")]
    public float moveSpeed = 2.5f;
    [Tooltip("Tốc độ xoay mặt về hướng di chuyển (°/s)")]
    public float rotationSpeed = 180f;

    [Header("Khoảng Dừng")]
    [Tooltip("Dừng ít nhất bao lâu tại điểm đến (giây)")]
    public float minWaitTime = 1.5f;
    [Tooltip("Dừng nhiều nhất bao lâu tại điểm đến (giây)")]
    public float maxWaitTime = 4f;

    [Header("Phát Hiện Mặt Đất")]
    [Tooltip("Layer được coi là mặt đất")]
    public LayerMask groundLayer = ~0;

    [Header("Trạng Thái (Readonly)")]
    [SerializeField] private bool _isWaiting = false;
    [SerializeField] private Vector3 _targetPoint;

    // Cache
    private Vector3        _origin;
    private Rigidbody      _rb;
    private CharacterController _cc;
    private float          _waitTimer;

    // ─────────────────────────────────────────────────────────────
    void Start()
    {
        _origin = transform.position;
        _rb     = GetComponent<Rigidbody>();
        _cc     = GetComponent<CharacterController>();

        if (_rb != null)
        {
            _rb.freezeRotation = true; // Script tự xoay
            _rb.useGravity     = true;
        }

        PickNewTarget();
    }

    void Update()
    {
        if (_isWaiting)
        {
            _waitTimer -= Time.deltaTime;
            if (_waitTimer <= 0f)
            {
                _isWaiting = false;
                PickNewTarget();
            }
            return;
        }

        MoveToTarget();
    }

    // ─────────────────────────────────────────────────────────────
    void MoveToTarget()
    {
        Vector3 flatTarget = new Vector3(_targetPoint.x, transform.position.y, _targetPoint.z);
        Vector3 dir        = (flatTarget - transform.position).normalized;
        float   dist       = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(_targetPoint.x,       _targetPoint.z));

        // Đã đến đích → dừng, chờ rồi chọn điểm mới
        if (dist < 0.3f)
        {
            _isWaiting = true;
            _waitTimer = Random.Range(minWaitTime, maxWaitTime);
            StopMovement();
            return;
        }

        // Xoay mặt về hướng đi
        if (dir != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation   = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Di chuyển theo loại component
        if (_cc != null && _cc.enabled)
        {
            Vector3 motion = dir * moveSpeed * Time.deltaTime;
            motion.y = Physics.gravity.y * Time.deltaTime; // Gravity thủ công
            _cc.Move(motion);
        }
        else if (_rb != null)
        {
            _rb.linearVelocity = new Vector3(dir.x * moveSpeed, _rb.linearVelocity.y, dir.z * moveSpeed);
        }
        else
        {
            // Fallback: dịch transform thẳng
            transform.position += dir * moveSpeed * Time.deltaTime;
        }
    }

    void StopMovement()
    {
        if (_rb != null)
            _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
    }

    // ─────────────────────────────────────────────────────────────
    void PickNewTarget()
    {
        // Chọn điểm ngẫu nhiên trong bán kính
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 rand2D   = Random.insideUnitCircle * wanderRadius;
            Vector3 candidate = _origin + new Vector3(rand2D.x, 2f, rand2D.y);

            // Raycast để tìm mặt đất
            if (Physics.Raycast(candidate, Vector3.down, out RaycastHit hit, 6f, groundLayer))
            {
                _targetPoint = hit.point;
                return;
            }
        }

        // Fallback: giữ nguyên Y của origin
        Vector2 fb = Random.insideUnitCircle * wanderRadius;
        _targetPoint = _origin + new Vector3(fb.x, 0f, fb.y);
    }

    // ─────────────────────────────────────────────────────────────
    // Vẽ debug gizmo trong Editor
    void OnDrawGizmosSelected()
    {
#if UNITY_EDITOR
        Vector3 center = Application.isPlaying ? _origin : transform.position;
        Handles.color  = new Color(0.2f, 0.8f, 1f, 0.25f);
        Handles.DrawSolidDisc(center, Vector3.up, wanderRadius);
        Handles.color = new Color(0.2f, 0.8f, 1f, 0.8f);
        Handles.DrawWireDisc(center, Vector3.up, wanderRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _targetPoint);
            Gizmos.DrawSphere(_targetPoint, 0.15f);
        }
#endif
    }
}
