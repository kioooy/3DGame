# 🎮 Hướng Dẫn Setup Hệ Thống Inventory

Hệ thống inventory đã được implement hoàn chỉnh! Làm theo các bước sau để setup trong Unity.

---

## 📋 Bước 1: Tạo UI Components

### 1.1. Tạo Inventory UI Canvas
1. Mở Unity Editor
2. Vào menu **Window > Inventory > Setup Inventory UI**
3. Click **"Create Inventory UI Canvas"**
4. Một Canvas mới sẽ được tạo với:
   - Inventory Panel (800x600)
   - Grid Layout (5 columns)
   - Title "INVENTORY"
   - Slot Prefab tại `Assets/Prefabs/UI/InventorySlot.prefab`

### 1.2. Tạo Inventory Manager
1. Trong cùng window, click **"Create Inventory Manager"**
2. GameObject "InventoryManager" sẽ xuất hiện trong scene
3. (Optional) Điều chỉnh `Inventory Size` trong Inspector (default: 20 slots)

### 1.3. Tạo Pickup Prompt UI
1. Click **"Create Pickup Prompt UI"**
2. Prompt UI sẽ được thêm vào Canvas

### 1.4. Assign Slot Prefab
1. Select **InventoryCanvas** trong Hierarchy
2. Tìm component **InventoryUI**
3. Assign **Slot Prefab**: Kéo prefab từ `Assets/Prefabs/UI/InventorySlot.prefab` vào field

---

## 🪨 Bước 2: Tạo Sample Items

### 2.1. Tạo Items Tự Động
1. Vào menu **Window > Inventory > Create Sample Items**
2. Click **"Create All Sample Items"**
3. Các items sau sẽ được tạo:
   - **Đá** (Stone) - Resource, max stack 99
   - **Gỗ** (Wood) - Resource, max stack 99
   - **Vàng** (Gold) - Material, max stack 50
   - **Táo** (Apple) - Consumable, max stack 20

### 2.2. Kiểm Tra Files
- **ItemData**: `Assets/Resources/Items/*.asset`
- **Prefabs**: `Assets/Prefabs/Items/Pickable*.prefab`

---

## 🎯 Bước 3: Setup Player Controller

### 3.1. Assign Camera Reference
1. Select **Player** object trong scene
2. Tìm component **PlayerController**
3. Assign **Camera Transform**: Kéo Main Camera vào field

### 3.2. Setup Item Layer
1. Trong **PlayerController** Inspector
2. Tìm field **Item Layer**
3. Select layer **"Item"** (đã được tạo tự động)
4. Điều chỉnh **Interaction Range** nếu cần (default: 3.5 units)

---

## 🔧 Bước 4: Fix Audio Listener (Nếu Có Lỗi)

### 4.1. Thêm AudioListenerFixer
1. Select **Main Camera** hoặc bất kỳ GameObject nào
2. Add Component: **AudioListenerFixer**
3. Script sẽ tự động xóa duplicate Audio Listeners
4. Chỉ giữ lại 1 Audio Listener trên Main Camera

---

## 🧪 Bước 5: Test Trong Scene

### 5.1. Thêm Items Vào Scene
1. Kéo prefabs từ `Assets/Prefabs/Items/` vào scene
2. Đặt chúng gần player để test
3. Mỗi item sẽ tự động rotate

### 5.2. Play Mode Testing
1. **Nhấn Play**
2. **Di chuyển** player đến gần item
3. **Nhìn vào item** → Thấy prompt "Nhấn [E] để nhặt [Tên Item]"
4. **Nhấn E** → Item biến mất, vào inventory
5. **Nhấn Tab** → Mở inventory UI
6. **Drag & drop** items giữa các slots
7. **Hover** vào item → Thấy tooltip
8. **Nhấn Tab** hoặc **ESC** → Đóng inventory

---

## ⚙️ Customization

### Thay Đổi Số Lượng Slots
```
InventoryManager > Inventory Size = 30
```

### Thay Đổi Grid Layout
```
InventoryCanvas > InventoryPanel > SlotsContainer > Grid Layout Group
- Cell Size: (80, 80)
- Spacing: (10, 10)
- Constraint Count: 5 (số cột)
```

### Tạo Item Mới
1. Right-click trong Project
2. **Create > Inventory > Item Data**
3. Điền thông tin:
   - Item Name (tiếng Việt)
   - Item Type
   - Max Stack Size
   - Description
4. Tạo prefab 3D model
5. Add component **PickableItem**
6. Assign ItemData vào PickableItem
7. Set Layer = **Item**

### Thay Đổi Phím Tắt
Sửa trong `InventoryUI.cs` và `PlayerController.cs`:
- **Tab** → Mở/đóng inventory
- **E** → Nhặt item
- **ESC** → Đóng inventory

---

## 🐛 Troubleshooting

### Items Không Highlight Khi Nhìn Vào
- Kiểm tra **Item Layer** đã được assign trong PlayerController
- Kiểm tra **Camera Transform** đã được assign
- Kiểm tra item prefab có layer = **Item**

### Inventory UI Không Hiện
- Kiểm tra **Slot Prefab** đã được assign trong InventoryUI
- Kiểm tra Canvas có **InventoryUI** component
- Kiểm tra **InventoryManager** có trong scene

### Không Nhặt Được Item
- Kiểm tra **InventoryManager** có trong scene
- Kiểm tra item có component **PickableItem**
- Kiểm tra item có **ItemData** được assign
- Kiểm tra Collider của item là **Trigger**

### Drag & Drop Không Hoạt Động
- Kiểm tra mỗi slot có component **ItemDragHandler**
- Kiểm tra Canvas có **GraphicRaycaster**
- Kiểm tra EventSystem có trong scene

---

## 🎨 Next Steps

Sau khi setup xong, bạn có thể:
- ✅ Tạo thêm items mới
- ✅ Thêm icons đẹp cho items (assign vào ItemData.itemIcon)
- ✅ Thêm sound effects khi pickup/drop
- ✅ Implement crafting system
- ✅ Implement equipment system
- ✅ Save/Load inventory data

---

**Chúc bạn code vui vẻ! 🚀**
