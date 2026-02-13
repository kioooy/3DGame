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
    
    /// <summary>
    /// Kiểm tra xem item có thể stack được không
    /// </summary>
    public bool IsStackable => maxStackSize > 1;
}
