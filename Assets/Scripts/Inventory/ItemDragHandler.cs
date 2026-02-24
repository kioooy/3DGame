using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Xử lý drag & drop items giữa các slots
/// </summary>
public class ItemDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [Header("Drag Settings")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private UIInventorySlot _uiSlot;
    private RectTransform _rectTransform;
    private Vector2 _originalPosition;
    private Transform _originalParent;
    private Image _dragImage;
    
    private static GameObject _draggedObject;
    private static UIInventorySlot _draggedSlot;
    
    void Awake()
    {
        _uiSlot = GetComponent<UIInventorySlot>();
        _rectTransform = GetComponent<RectTransform>();
        _dragImage = GetComponent<Image>();
        
        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();
        
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        // Chỉ drag nếu slot có item
        if (_uiSlot == null || _uiSlot.Slot == null || _uiSlot.Slot.IsEmpty)
            return;
        
        _originalPosition = _rectTransform.anchoredPosition;
        _originalParent = transform.parent;
        
        // Set làm child của canvas để render trên top
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
        
        // Semi-transparent khi drag
        canvasGroup.alpha = 0.6f;
        canvasGroup.blocksRaycasts = false;
        
        _draggedObject = gameObject;
        _draggedSlot = _uiSlot;
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (_draggedObject != gameObject) return;
        
        _rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (_draggedObject != gameObject) return;
        
        // Reset về vị trí cũ
        transform.SetParent(_originalParent);
        _rectTransform.anchoredPosition = _originalPosition;
        
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        
        _draggedObject = null;
        _draggedSlot = null;
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // Không drop vào chính nó
        if (_draggedSlot == null || _draggedSlot == _uiSlot)
            return;
        
        // Lấy indices
        int fromIndex = _draggedSlot.SlotIndex;
        int toIndex = _uiSlot.SlotIndex;
        
        // Nếu slot đích trống hoặc cùng loại item -> merge
        if (_uiSlot.Slot.IsEmpty || _uiSlot.Slot.item == _draggedSlot.Slot.item)
        {
            InventoryManager.Instance.MergeSlots(fromIndex, toIndex);
        }
        // Ngược lại -> swap
        else
        {
            InventoryManager.Instance.SwapSlots(fromIndex, toIndex);
        }
    }
}
