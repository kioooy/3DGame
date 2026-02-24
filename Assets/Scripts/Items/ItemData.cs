using UnityEngine;

/// <summary>
/// ScriptableObject chứa thông tin của một loại vật phẩm
/// Tạo instances trong Assets/Resources/Items/
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    [Tooltip("Tên vật phẩm (tiếng Việt)")]
    public string itemName = "Vật phẩm mới";
    
    [Tooltip("Icon hiển thị trong inventory")]
    public Sprite itemIcon;
    
    [Tooltip("Loại vật phẩm")]
    public ItemType itemType = ItemType.Resource;
    
    [Header("Stack Settings")]
    [Tooltip("Số lượng tối đa trong 1 slot")]
    [Range(1, 999)]
    public int maxStackSize = 99;
    
    [Header("Description")]
    [TextArea(3, 6)]
    [Tooltip("Mô tả vật phẩm")]
    public string description = "Một vật phẩm thú vị...";
    
    [Header("3D Model")]
    [Tooltip("Prefab 3D model cho vật phẩm trong world")]
    public GameObject worldModelPrefab;
    
    [Tooltip("Prefab 3D model khi cầm trên tay")]
    public GameObject handModelPrefab;
    
    [Header("Equipment")]
    [Tooltip("Item có thể cầm trên tay không")]
    public bool isEquippable = true;
    
    [Header("Throwable Settings")]
    [Tooltip("Item có thể ném không")]
    public bool isThrowable = false;
    
    [Tooltip("Lực ném (càng cao ném càng xa)")]
    [Range(1f, 50f)]
    public float throwForce = 15f;
    
    [Tooltip("Sát thương khi ném trúng")]
    [Range(0, 100)]
    public int throwDamage = 10;
    
    [Tooltip("Prefab cho projectile khi ném (nếu null sẽ dùng worldModelPrefab)")]
    public GameObject projectilePrefab;
    
    /// <summary>
    /// Kiểm tra xem item có thể stack được không
    /// </summary>
    public bool IsStackable => maxStackSize > 1;
}
