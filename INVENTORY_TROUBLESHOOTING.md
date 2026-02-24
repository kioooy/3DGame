# Hướng Dẫn Sửa Lỗi Inventory System

## Các Lỗi Hiện Tại

### 1. NullReferenceException: InventoryManager.Instance is null

**Nguyên nhân:** Không có GameObject với component `InventoryManager` trong scene.

**Cách sửa:**
1. Trong Unity, tạo một Empty GameObject mới (Right-click → Create Empty)
2. Đặt tên là `InventoryManager`
3. Add Component → `InventoryManager`
4. Đảm bảo GameObject này được active trong scene

### 2. InventoryUI: Missing references!

**Nguyên nhân:** Component `InventoryUI` thiếu các references cần thiết.

**Cách sửa:**
1. Tìm GameObject có component `InventoryUI` trong scene
2. Trong Inspector, kiểm tra các field sau:
   - **inventoryPanel**: Assign GameObject chứa UI panel của inventory
   - **slotsContainer**: Assign Transform chứa các slot (thường là một Panel hoặc Grid Layout Group)
   - **slotPrefab**: Assign prefab của UI slot (tạo từ Tools → Create Inventory Slot Prefab)

### 3. Không hiện "E" để nhặt đá

**Nguyên nhân:** Thiếu `PickupPromptUI` hoặc `PickableItem` chưa được setup đúng.

**Cách sửa:**

#### A. Kiểm tra PickupPromptUI
1. Tìm GameObject có component `PickupPromptUI` trong Canvas
2. Kiểm tra các field:
   - **promptPanel**: GameObject chứa UI prompt
   - **promptText**: TextMeshProUGUI hiển thị text "Nhấn [E] để nhặt..."

#### B. Kiểm tra PickableItem trên đá
1. Select GameObject của đá (PT_Generic_Rock_01)
2. Kiểm tra component `PickableItem`:
   - **itemData**: Phải assign ScriptableObject ItemData (tạo từ Tools → Create Item)
   - **quantity**: Số lượng (mặc định 1)
3. Kiểm tra Collider:
   - Phải có BoxCollider hoặc SphereCollider
   - **Is Trigger** phải được check ✓

#### C. Kiểm tra PlayerController
1. Select Player GameObject
2. Kiểm tra component `PlayerController`:
   - **cameraTransform**: Assign Main Camera transform
   - **interactionRange**: Khoảng cách nhặt đồ (mặc định 3.5)
   - **itemLayer**: (Optional) Layer của items để tối ưu raycast

### 4. BoxCollider negative scale warning

**Nguyên nhân:** GameObject "PT_Generic_Rock_01" có scale âm.

**Cách sửa:**
1. Select GameObject "PT_Generic_Rock_01"
2. Trong Inspector, kiểm tra Transform → Scale
3. Đảm bảo tất cả giá trị X, Y, Z đều dương (> 0)
4. Nếu cần flip object, dùng Rotation thay vì negative scale

## Công Cụ Kiểm Tra Tự Động

Sử dụng tool **Inventory Setup Validator** để kiểm tra setup:

1. Trong Unity, vào menu: **Tools → Validate Inventory Setup**
2. Tool sẽ hiển thị:
   - ✓ Components đã setup đúng (màu xanh)
   - ✗ Components thiếu (màu đỏ)
   - ⚠ Components có nhưng thiếu references (màu vàng)
3. Làm theo hướng dẫn trong tool để fix các vấn đề

## Checklist Setup Hoàn Chỉnh

- [ ] **InventoryManager** GameObject exists và active
- [ ] **InventoryUI** có đầy đủ references (panel, container, prefab)
- [ ] **PickupPromptUI** có đầy đủ references (panel, text)
- [ ] **PlayerController** có cameraTransform assigned
- [ ] Tất cả **PickableItem** có ItemData assigned
- [ ] Tất cả **PickableItem** có Collider với Is Trigger = true
- [ ] Không có GameObject nào có negative scale

## Test Sau Khi Sửa

1. Chạy game (Play mode)
2. Di chuyển player đến gần đá
3. Kiểm tra:
   - Có hiện text "Nhấn [E] để nhặt..." không?
   - Nhấn E có nhặt được không?
   - Mở inventory (Tab) có thấy item không?
4. Kiểm tra Console không còn lỗi đỏ

## Nếu Vẫn Lỗi

Kiểm tra Console log để xem thông báo chi tiết:
- "InventoryManager.Instance is null!" → Thiếu InventoryManager
- "PickupPromptUI.Instance is null!" → Thiếu PickupPromptUI
- "PickableItem không có ItemData!" → Chưa assign ItemData
- "Camera Transform chưa được assign!" → Chưa assign camera trong PlayerController
