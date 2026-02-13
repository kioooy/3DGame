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
        // Đảm bảo collider là trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
        
        // Lưu renderers và màu gốc
        _renderers = GetComponentsInChildren<Renderer>();
        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i].material != null)
                _originalColors[i] = _renderers[i].material.color;
        }
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
        
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i] != null && _renderers[i].material != null)
            {
                _renderers[i].material.color = enable ? highlightColor : _originalColors[i];
            }
        }
    }
    
    /// <summary>
    /// Nhặt vật phẩm này
    /// </summary>
    public bool TryPickup()
    {
        if (itemData == null)
        {
            Debug.LogWarning($"PickableItem '{gameObject.name}' không có ItemData!");
            return false;
        }
        
        // Check if InventoryManager exists
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager.Instance is null! Make sure InventoryManager exists in the scene.");
            return false;
        }
        
        // Thử thêm vào inventory
        bool success = InventoryManager.Instance.AddItem(itemData, quantity);
        
        if (success)
        {
            // TODO: Play pickup sound/effect
            Destroy(gameObject);
            return true;
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
