using UnityEngine;

/// <summary>
/// Component gắn vào các vật phẩm có thể nhặt trong world
/// </summary>
[RequireComponent(typeof(Collider))]
public class PickableItem : MonoBehaviour
{
    [Header("Item Data")]
    [Tooltip("Loại vật phẩm này")]
    public ItemData itemData;
    
    [Tooltip("Số lượng vật phẩm")]
    [Range(1, 999)]
    public int quantity = 1;
    
    [Header("Visual Settings")]
    [Tooltip("Tự động xoay vật phẩm")]
    public bool autoRotate = true;
    
    [Tooltip("Tốc độ xoay (degrees/second)")]
    public float rotationSpeed = 50f;
    
    [Tooltip("Màu highlight khi player nhìn vào")]
    public Color highlightColor = new Color(1f, 1f, 0.5f, 1f);
    
    private Renderer[] _renderers;
    private Color[] _originalColors;
    private bool _isHighlighted = false;
    
    void Awake()
    {
        // Set collider as trigger to avoid physics conflicts
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
        
        // Lưu renderers và màu gốc
        _renderers = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i].material != null)
            {
                // Check if material has _Color property (some shaders don't)
                if (_renderers[i].material.HasProperty("_Color"))
                {
                    _originalColors[i] = _renderers[i].material.color;
                }
                else
                {
                    _originalColors[i] = Color.white;
                }
            }
        }
        
        Debug.Log($"[PickableItem] {gameObject.name} initialized as trigger");
    }
    
    void Update()
    {
        // Auto rotate
        if (autoRotate)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }
    }
    
    /// <summary>
    /// Highlight vật phẩm khi player nhìn vào
    /// </summary>
    public void Highlight(bool enable)
    {
        if (_isHighlighted == enable) return;
        _isHighlighted = enable;
        
        if (_renderers == null || _renderers.Length == 0)
        {
            Debug.LogWarning($"[PickableItem] {gameObject.name}: No renderers found for highlight!");
            return;
        }
        
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material != null)
            {
                Material mat = _renderers[i].material;
                if (enable)
                {
                    // Lên màu vàng kim cực mạnh với Emission thay vì đổi màu vật lý thông thường
                    mat.SetColor("_EmissionColor", highlightColor * 2.5f); // 2.5f là cường độ sáng
                    mat.EnableKeyword("_EMISSION");
                    
                    if (mat.HasProperty("_Color")) 
                    {
                        mat.color = Color.Lerp(_originalColors[i], highlightColor, 0.5f);
                    }
                }
                else
                {
                    // Trả về bình thường
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                    
                    if (mat.HasProperty("_Color"))
                    {
                        mat.color = _originalColors[i];
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Nhặt vật phẩm này
    /// </summary>
    public bool TryPickup()
    {
        Debug.Log($"[PickableItem] TryPickup called on {gameObject.name}");
        
        if (itemData == null)
        {
            Debug.LogError($"[PickableItem] ❌ '{gameObject.name}' KHÔNG CÓ ItemData! Vui lòng assign ItemData trong Inspector!");
            Debug.LogError($"[PickableItem] → Chọn GameObject '{gameObject.name}' → Inspector → PickableItem component → Kéo ItemData vào field 'Item Data'");
            return false;
        }
        
        Debug.Log($"[PickableItem] ItemData OK: {itemData.itemName}");
        
        // Check if InventoryManager exists
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[PickableItem] ❌ InventoryManager.Instance is null! Make sure InventoryManager exists in the scene.");
            return false;
        }
        
        Debug.Log($"[PickableItem] InventoryManager OK, calling AddItem({itemData.itemName}, {quantity})");
        
        // Thử thêm vào inventory
        bool success = InventoryManager.Instance.AddItem(itemData, quantity);
        
        if (success)
        {
            Debug.Log($"[PickableItem] ✅ Successfully picked up {quantity}x {itemData.itemName}");
            
            // Nếu là đá thì check vụ làm nhiệm vụ nhặt đá
            if (QuestUIManager.Instance != null && itemData != null && itemData.itemName.ToLower().Contains("đá"))
            {
                QuestUIManager.Instance.CompleteQuest("pickup_stone");
            }

            // TODO: Play pickup sound/effect
            Destroy(gameObject);
            return true;
        }
        else
        {
            Debug.LogWarning($"[PickableItem] ⚠️ Failed to add {itemData.itemName} to inventory (inventory full?)");
        }
        
        return false;
    }
    
    void OnDestroy()
    {
        // Cleanup materials nếu cần
        foreach (var renderer in _renderers)
        {
            if (renderer != null && renderer.material != null)
            {
                Destroy(renderer.material);
            }
        }
    }
}
