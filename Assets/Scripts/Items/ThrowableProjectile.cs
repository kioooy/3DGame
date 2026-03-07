using UnityEngine;

/// <summary>
/// Component cho projectile khi ném items
/// Xử lý physics, collision, và damage
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ThrowableProjectile : MonoBehaviour
{
    [Header("Settings")]
    public ItemData itemData;
    public float lifetime = 5f;
    
    private Rigidbody _rb;
    private bool _hasHit = false;
    
    void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Tắt isTrigger để cục đá có thể rớt và đập vào mặt đất (Physics Collision)
        Collider[] cols = GetComponentsInChildren<Collider>();
        foreach (Collider c in cols)
        {
            c.isTrigger = false;
        }

        // Tắt khả năng nhặt lại trên không
        PickableItem pickable = GetComponent<PickableItem>();
        if (pickable != null) Destroy(pickable);
    }
    
    void Start()
    {
        // Auto destroy sau lifetime
        Destroy(gameObject, lifetime);
    }
    
    /// <summary>
    /// Khởi tạo projectile với lực ném
    /// </summary>
    public void Initialize(ItemData data, Vector3 throwDirection, float force)
    {
        itemData = data;
        
        if (_rb != null)
        {
            _rb.AddForce(throwDirection * force, ForceMode.Impulse);
            // Thêm một chút spin cho realistic
            _rb.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
        }
    }
    
    void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;
        _hasHit = true;
        
        // TODO: Apply damage nếu va chạm với enemy
        // var enemy = collision.gameObject.GetComponent<Enemy>();
        // if (enemy != null && itemData != null)
        // {
        //     enemy.TakeDamage(itemData.throwDamage);
        // }
        
        Debug.Log($"[ThrowableProjectile] {itemData?.itemName} hit {collision.gameObject.name}");
        
        // Destroy sau khi va chạm
        Destroy(gameObject, 0.1f);
    }
}
