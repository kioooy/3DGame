# 🔧 Troubleshooting Guide - Inventory System

## ❌ Vấn Đề: Không Hiện "Nhấn E Để Nhặt"

### Nguyên nhân có thể:

#### 1. **Chưa Assign Camera Transform**
**Kiểm tra:**
- Select **Player** object
- Trong **PlayerController** component
- Field **Camera Transform** phải được assign (kéo Main Camera vào)

**Fix:**
```
Player > PlayerController > Camera Transform = Main Camera
```

#### 2. **Item Không Có ItemData**
**Kiểm tra:**
- Select item trong scene (vd: PickableStone)
- Trong **PickableItem** component
- Field **Item Data** phải có ScriptableObject

**Fix:**
```
PickableStone > PickableItem > Item Data = Stone (ScriptableObject)
```

#### 3. **Item Không Có Collider hoặc Collider Không Phải Trigger**
**Kiểm tra:**
- Select item trong scene
- Phải có component **Collider** (Box/Sphere/Capsule)
- Checkbox **Is Trigger** phải được tick ✅

**Fix:**
```
PickableStone > Box Collider > Is Trigger = ✅
```

#### 4. **PickupPromptUI Chưa Được Tạo**
**Kiểm tra:**
- Trong Hierarchy, tìm **PickupPromptUI** object
- Nếu không có, tạo lại

**Fix:**
```
Window > Inventory > Setup Inventory UI > Create Pickup Prompt UI
```

#### 5. **Khoảng Cách Quá Xa**
**Kiểm tra:**
- Default interaction range = 3.5 units
- Đứng gần item hơn

**Fix (nếu cần tăng range):**
```
Player > PlayerController > Interaction Range = 5.0
```

---

## ❌ Vấn Đề: Inventory Tự Đóng Ngay Sau Khi Mở

### ✅ ĐÃ FIX!
Vấn đề này do **duplicate Tab key handling** trong cả `InventoryUI.cs` và `PlayerController.cs`.

**Đã sửa:** Xóa Tab handling trong `InventoryUI.cs`, chỉ giữ trong `PlayerController.cs`.

---

## 🧪 Debug Steps

### Test 1: Kiểm Tra Raycast
1. Mở Console (Ctrl+Shift+C)
2. Play game
3. Nhìn vào item
4. **Nếu thấy warning**: "Camera Transform chưa được assign!" → Fix bước 1 ở trên
5. **Nếu thấy warning**: "PickableItem không có ItemData!" → Fix bước 2 ở trên

### Test 2: Kiểm Tra Layer (Optional)
1. Select item prefab
2. Ở góc trên bên phải Inspector
3. **Layer** dropdown → Chọn **"Item"**
4. Nếu không có layer "Item":
   ```
   Window > Inventory > Create Sample Items > Setup Item Layer
   ```

### Test 3: Visual Debug Raycast
Thêm code này vào `PlayerController.DetectPickableItems()` để thấy raycast:
```csharp
// Thêm sau dòng: Ray ray = new Ray(...)
Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.red);
```
- Chạy game trong Scene view
- Sẽ thấy tia màu đỏ từ camera

---

## ✅ Checklist Setup Đầy Đủ

- [ ] **InventoryManager** có trong scene
- [ ] **InventoryCanvas** có trong scene với InventoryUI component
- [ ] **PickupPromptUI** có trong scene
- [ ] **Player** có PlayerController với:
  - [ ] Camera Transform assigned
  - [ ] Interaction Range > 0
- [ ] **Item prefabs** có:
  - [ ] PickableItem component
  - [ ] ItemData assigned
  - [ ] Collider với Is Trigger = ✅
  - [ ] Layer = "Item" (optional)
- [ ] **Slot Prefab** assigned trong InventoryUI

---

## 🎯 Quick Fix Commands

### Tạo Lại Toàn Bộ UI
```
1. Window > Inventory > Setup Inventory UI
2. Click "Create Inventory UI Canvas"
3. Click "Create Inventory Manager"
4. Click "Create Pickup Prompt UI"
```

### Tạo Lại Items
```
1. Window > Inventory > Create Sample Items
2. Click "Create All Sample Items"
3. Kéo prefabs từ Assets/Prefabs/Items/ vào scene
```

### Assign References
```
1. InventoryCanvas > InventoryUI > Slot Prefab = Assets/Prefabs/UI/InventorySlot.prefab
2. Player > PlayerController > Camera Transform = Main Camera
3. Player > PlayerController > Item Layer = "Item" (hoặc để Everything)
```

---

## 📞 Vẫn Không Hoạt Động?

Gửi thông tin sau:
1. Screenshot của Player Inspector (PlayerController component)
2. Screenshot của Item Inspector (PickableItem component)
3. Console logs (nếu có)
4. Unity version đang dùng
