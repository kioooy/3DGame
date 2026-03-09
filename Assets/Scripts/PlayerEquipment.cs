using UnityEngine;
using System.Collections;

/// <summary>
/// Quản lý item đang cầm trên tay và xử lý việc ném items
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform handTransform;
    [SerializeField] private Transform throwOrigin; // Vị trí spawn projectile
    
    [Header("Settings")]
    [SerializeField] private float throwForceMultiplier = 1f;
    
    private ItemData _currentEquippedItem;
    private GameObject _currentHandModel;
    private int _currentEquippedSlotIndex = -1;
    
    private Animator _playerAnimator;
    private bool _isThrowing = false;
    
    void Awake()
    {
        // Tạo hand transform nếu chưa có
        if (handTransform == null)
        {
            GameObject handObj = new GameObject("HandTransform");
            handObj.transform.SetParent(transform);
            handObj.transform.localPosition = new Vector3(0.3f, 1.2f, 0.5f); // Vị trí tay phải
            handTransform = handObj.transform;
        }
        
        // Tạo throw origin nếu chưa có
        if (throwOrigin == null)
        {
            GameObject throwObj = new GameObject("ThrowOrigin");
            throwObj.transform.SetParent(transform);
            // Vị trí nằm nhích sang tay phải (0.4f), cao ngang ngực/vai (1f), đẩy về phía trước mặt xíu (0.5f)
            throwObj.transform.localPosition = new Vector3(0.4f, 1f, 0.5f); 
            throwOrigin = throwObj.transform;
        }

        _playerAnimator = GetComponentInChildren<Animator>();
        if (_playerAnimator == null && transform.parent != null)
        {
            _playerAnimator = transform.parent.GetComponentInChildren<Animator>();
        }
    }
    
    /// <summary>
    /// Equip item từ inventory slot
    /// </summary>
    public void EquipItem(ItemData itemData, int slotIndex)
    {
        if (itemData == null || !itemData.isEquippable)
        {
            Debug.LogWarning($"[PlayerEquipment] Cannot equip {itemData?.itemName}");
            return;
        }
        
        // Unequip item hiện tại
        UnequipItem();
        
        _currentEquippedItem = itemData;
        _currentEquippedSlotIndex = slotIndex;
        
        // Hiển thị model trên tay
        if (itemData.handModelPrefab != null)
        {
            _currentHandModel = Instantiate(itemData.handModelPrefab, handTransform);
            _currentHandModel.transform.localPosition = Vector3.zero;
            _currentHandModel.transform.localRotation = Quaternion.identity;
        }
        
        Debug.Log($"[PlayerEquipment] Equipped {itemData.itemName}");
    }
    
    /// <summary>
    /// Bỏ item đang cầm
    /// </summary>
    public void UnequipItem()
    {
        if (_currentHandModel != null)
        {
            Destroy(_currentHandModel);
            _currentHandModel = null;
        }
        
        _currentEquippedItem = null;
        _currentEquippedSlotIndex = -1;
    }
    
    /// <summary>
    /// Ném item đang cầm
    /// </summary>
    public void ThrowItem(Vector3 throwDirection)
    {
        if (_currentEquippedItem == null || !_currentEquippedItem.isThrowable || _isThrowing)
        {
            Debug.LogWarning($"[PlayerEquipment] Cannot throw {_currentEquippedItem?.itemName}");
            return;
        }
        
        StartCoroutine(ThrowItemRoutine(throwDirection));
    }

    private IEnumerator ThrowItemRoutine(Vector3 throwDirection)
    {
        _isThrowing = true;

        if (_playerAnimator != null)
        {
            _playerAnimator.SetTrigger("Throw");
            
            // Xóa animation Emote (Nếu đang nhảy) để ưu tiên Throw
            if (EmoteUIManager.Instance != null && EmoteUIManager.IsEmoteMenuOpen == false)
            {
               EmoteUIManager.Instance.CancelEmote(); 
            }
        }

        // Tạm chờ animation vung tay ra sau và ném đi (đo lường Animation Throw kéo dài khoảng 0.7s - 0.8s)
        yield return new WaitForSeconds(0.7f);
        
        // Lấy prefab cho projectile
        GameObject prefab = _currentEquippedItem.projectilePrefab;
        if (prefab == null)
        {
            prefab = _currentEquippedItem.worldModelPrefab;
        }
        
        if (prefab == null)
        {
            Debug.LogError($"[PlayerEquipment] No prefab for throwing {_currentEquippedItem.itemName}");
            _isThrowing = false;
            yield break;
        }
        
        // Spawn projectile
        Vector3 spawnPos = throwOrigin != null ? throwOrigin.position : transform.position + Vector3.up * 1.5f;
        GameObject projectileObj = Instantiate(prefab, spawnPos, Quaternion.identity);
        
        // Add ThrowableProjectile component nếu chưa có
        ThrowableProjectile projectile = projectileObj.GetComponent<ThrowableProjectile>();
        if (projectile == null)
        {
            projectile = projectileObj.AddComponent<ThrowableProjectile>();
        }
        
        // Ensure Rigidbody
        Rigidbody rb = projectileObj.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = projectileObj.AddComponent<Rigidbody>();
        }
        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        
        // Initialize và throw
        float totalForce = _currentEquippedItem.throwForce * throwForceMultiplier;
        projectile.Initialize(_currentEquippedItem, throwDirection, totalForce);
        
        Debug.Log($"[PlayerEquipment] Threw {_currentEquippedItem.itemName} with force {totalForce}");
        
        // Remove 1 item từ inventory
        if (InventoryManager.Instance != null && _currentEquippedSlotIndex >= 0)
        {
            InventoryManager.Instance.RemoveItemFromSlot(_currentEquippedSlotIndex, 1);
            
            // Nếu hết item, unequip
            var slot = InventoryManager.Instance.GetSlot(_currentEquippedSlotIndex);
            if (slot == null || slot.IsEmpty)
            {
                UnequipItem();
            }
        }

        // Chờ nốt animation throw kết thúc hoàn toàn (thêm 0.3s) rồi mới cho ném tiếp
        yield return new WaitForSeconds(0.3f);
        _isThrowing = false;
    }
    
    public ItemData CurrentEquippedItem => _currentEquippedItem;
    public int CurrentEquippedSlotIndex => _currentEquippedSlotIndex;
    public bool HasEquippedItem => _currentEquippedItem != null;
}
