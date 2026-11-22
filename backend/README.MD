# Warehouse Management API — Hướng dẫn chạy, debug & test Swagger

Backend .NET Clean Architecture: **Domain → Application → Infrastructure → WebAPI**.  
REST + JWT, Swagger UI dùng để test toàn bộ nghiệp vụ kho.

---

## 1. Yêu cầu

| Thành phần | Ghi chú |
|------------|---------|
| .NET SDK | net10 (theo `.csproj`) |
| SQL Server | Ví dụ `localhost,1434` — xem `appsettings.json` |
| IDE | Visual Studio / Rider / **Cursor / VS Code** + C# Dev Kit |

Connection string mặc định:

```text
Server=localhost,1434;Database=WareDbnew1;User Id=sa;Password=123456Aa@;TrustServerCertificate=True;Encrypt=False;
```

---

## 2. Chạy project

```bash
cd backend/src/WarehouseManagement.WebAPI

# (lần đầu / sau khi đổi model)
dotnet ef database update --project ../WarehouseManagement.Infrastructure/WarehouseManagement.Infrastructure.csproj

dotnet run --urls "http://localhost:5080"
```

- Swagger: [http://localhost:5080/swagger](http://localhost:5080/swagger)
- Launch profile mặc định (IDE): `http://localhost:5136` — mở Swagger tương ứng nếu dùng F5.

Khi start, `DataSeeder` tự seed role + user mẫu (idempotent).

---

## 3. Debug bằng IDE (Cursor / VS Code / Visual Studio)

1. Mở folder `backend` hoặc solution.
2. Đặt breakpoint trong Service / Controller cần xem (ví dụ `ImportReceiptService.ConfirmAsync`).
3. Chọn project **WarehouseManagement.WebAPI** → **Start Debugging (F5)**.
4. Gọi API từ Swagger (hoặc REST Client) → IDE dừng tại breakpoint.
5. Xem **Locals / Watch**: `dto`, `receipt`, `stock.Quantity`, exception message.

**Gợi ý breakpoint hữu ích**

| Nghiệp vụ | File gợi ý |
|-----------|------------|
| Login JWT | `AuthService.cs` |
| Cộng/trừ tồn | `StockMovementService.cs` |
| Confirm nhập | `ImportReceiptService.ConfirmAsync` |
| Confirm xuất | `ExportReceiptService.ConfirmAsync` |
| Kiểm kê | `StocktakeService.ConfirmAsync` |
| Lịch sử tồn | `StockTransactionService.cs` |

**Log SQL (tùy chọn):** trong `appsettings.Development.json` tăng log EF:

```json
"Logging": {
  "LogLevel": {
    "Microsoft.EntityFrameworkCore.Database.Command": "Information"
  }
}
```

---

## 4. Swagger + JWT (bắt buộc trước khi gọi API quản trị)

### 4.1. Lấy token

1. Mở Swagger → nhóm **Auth**.
2. `POST /api/v1/auth/admin/login`

```json
{
  "username": "admin",
  "password": "123456"
}
```

3. Copy `data.token` trong response.

### 4.2. Authorize trên Swagger

1. Bấm nút **Authorize** (ổ khóa).
2. Dán: `Bearer {token}` **hoặc** chỉ dán token nếu UI đã ghi `Bearer` sẵn.
3. **Authorize** → **Close**.
4. Các API `[Authorize(Roles = Admin,Manager,Staff)]` gọi được (200 thay vì 401).

### 4.3. Tài khoản seed

| Username | Password | Role | Cổng |
|----------|----------|------|------|
| `admin` | `123456` | Admin | Admin |
| `manager` | `123456` | Manager | Admin |
| `staff` | `123456` | Staff | Admin |
| `customer1` | `123456` | Customer | Customer |

- Admin portal: `POST /api/v1/auth/admin/login` — chỉ Admin/Manager/Staff.
- Customer portal: `POST /api/v1/auth/customer/login` — chỉ Customer.
- Gọi nhầm cổng → 401.

---

## 5. Quy ước API

- Base: `/api/v1/[controller]` (kebab-case: `warehouse-stocks`, `import-receipts`, …).
- Response bọc:

```json
{
  "success": true,
  "message": "...",
  "data": { }
}
```

- Phân trang query: `search`, `pageNumber` (mặc định 1), `pageSize` (mặc định 10, tối đa 100).
- Upload ảnh: `.jpg/.jpeg/.png/.webp`, tối đa **2MB**/file → lưu `wwwroot/uploads/...`.

| HTTP | Ý nghĩa thường gặp |
|------|---------------------|
| 200/201 | Thành công |
| 204 | Xóa thành công (No Content) |
| 400 | Validation |
| 401 | Chưa login / sai cổng |
| 404 | Không tìm thấy |
| 409 | Conflict (trùng mã, không đủ tồn, sai trạng thái phiếu) |

---

## 6. Checklist test toàn bộ tính năng (theo thứ tự)

> Làm lần lượt trên Swagger sau khi đã **Authorize** bằng `admin`.  
> Ghi lại `id` trả về để dùng bước sau (`categoryId`, `unitId`, `warehouseId`, …).

### A. Danh mục nền

| # | API | Body / ghi chú | Kỳ vọng |
|---|-----|----------------|---------|
| 1 | `GET /api/v1/roles` | | Có Admin, Manager, Staff, Customer |
| 2 | `GET /api/v1/users` | | Có user seed |
| 3 | `POST /api/v1/categories` | `{"name":"Thực phẩm","description":"DM test","isActive":true}` | 201 |
| 4 | `POST /api/v1/units` | `{"name":"Thùng","symbol":"thung","isActive":true}` | 201 (hoặc dùng unit seed nếu đã có) |
| 5 | `POST /api/v1/suppliers` | `{"name":"NCC A","phone":"0912345678","isActive":true}` | 201 |
| 6 | `POST /api/v1/customers` | Có thể kèm `"username":"kh02","password":"123456","email":"kh02@email.com"` để tạo User role Customer | 201 |
| 7 | `POST /api/v1/customers/{id}/avatar` | form-data: `file` = ảnh | `avatarUrl` dạng `/uploads/avatars/...` |
| 8 | `POST /api/v1/warehouses` | `{"code":"WH-02","name":"Kho HCM","phone":"0909111222","isActive":true}` | 201 |

### B. Sản phẩm + ảnh

| # | API | Body / ghi chú | Kỳ vọng |
|---|-----|----------------|---------|
| 9 | `POST /api/v1/products` | `sku`, `name`, `price`, `minStock`, `categoryId`, `unitId`, `isActive` | 201 |
| 10 | `POST /api/v1/products/{id}/images` | form-data: `files` (nhiều file) | Nhiều ảnh; ảnh đầu `isPrimary=true` nếu chưa có |
| 11 | `PUT /api/v1/products/{id}/images/{imageId}/primary` | | Đổi ảnh chính |
| 12 | `DELETE /api/v1/products/{id}/images/{imageId}` | | Xóa ảnh + file |

### C. Tồn kho thủ công (WarehouseStocks)

| # | API | Ghi chú | Kỳ vọng |
|---|-----|---------|---------|
| 13 | `POST /api/v1/warehouse-stocks` | `warehouseId`, `productId`, `quantity` | Unique cặp kho+SP; ghi `StockTransactions` type **Manual** |
| 14 | `PUT /api/v1/warehouse-stocks/{id}` | Đổi `quantity` | Có transaction Manual |
| 15 | `GET /api/v1/warehouse-stocks?lowStockOnly=true` | | `isLowStock` khi `quantity <= minStock` |

> Nếu đã có tồn từ nhập kho trước đó thì bỏ qua bước tạo trùng (409).

### D. Nhập kho (ImportReceipts)

**Luồng:** Draft → (sửa) → **Confirm** (cộng tồn) → (tuỳ chọn) **Cancel** (trừ lại).

| # | API | Body / ghi chú | Kỳ vọng |
|---|-----|----------------|---------|
| 16 | `POST /api/v1/import-receipts` | `warehouseId`, `supplierId`, `details:[{productId, quantity, unitPrice}]` | Status **Draft**, mã `PN-yyyyMMdd-xxxx` |
| 17 | `PUT /api/v1/import-receipts/{id}` | Chỉ khi Draft | Thay details |
| 18 | `POST /api/v1/import-receipts/{id}/confirm` | | Status **Confirmed**; tồn tăng; TX type **Import** |
| 19 | `POST /api/v1/import-receipts/{id}/cancel` | Nếu đã Confirm | Hoàn tồn; TX **ImportCancel** |
| 20 | `DELETE /api/v1/import-receipts/{id}` | Chỉ Draft | 204 |

**Test âm:** Confirm lần 2 / sửa phiếu Confirmed → **409**.

### E. Xuất kho (ExportReceipts)

**Luồng:** Draft → **Confirm** (trừ tồn, phải đủ hàng) → Cancel (cộng lại).

| # | API | Body / ghi chú | Kỳ vọng |
|---|-----|----------------|---------|
| 21 | `POST /api/v1/export-receipts` | `warehouseId`, `customerId`, `details:[...]` | Mã `PX-...`, Draft |
| 22 | `POST /api/v1/export-receipts/{id}/confirm` | Đủ tồn | Tồn giảm; TX **Export** |
| 23 | Confirm với `quantity` > tồn | | **409** không đủ tồn |
| 24 | `POST /api/v1/export-receipts/{id}/cancel` | Đã Confirm | Hoàn tồn; TX **ExportCancel** |

### F. Kiểm kê (Stocktakes + StocktakeDetails)

| # | API | Ghi chú | Kỳ vọng |
|---|-----|---------|---------|
| 25 | `POST /api/v1/stocktakes` | `warehouseId`, `details:[{productId, countedQuantity}]` | `systemQuantity` = tồn hiện tại; `difference = counted - system` |
| 26 | `PUT /api/v1/stocktakes/{id}/details/{detailId}` | Chỉ Draft | Cập nhật số đếm, snapshot lại system |
| 27 | `POST /api/v1/stocktakes/{id}/details` | Thêm dòng | Không trùng product trên phiếu |
| 28 | `GET /api/v1/stocktake-details?onlyDiff=true` | Báo cáo lệch | `hasDifference=true` |
| 29 | `POST /api/v1/stocktakes/{id}/confirm` | | Tồn = `countedQuantity`; TX **Stocktake** |
| 30 | `POST /api/v1/stocktakes/{id}/cancel` | Đã Confirm | Hoàn về `systemQuantity`; TX **StocktakeCancel** |

### G. Lịch sử biến động (StockTransactions)

| # | API | Query | Kỳ vọng |
|---|-----|-------|---------|
| 31 | `GET /api/v1/stock-transactions` | `warehouseId`, `productId` | Thấy chuỗi Import / Export / Stocktake / Manual |
| 32 | `GET /api/v1/stock-transactions?type=Import` | | Chỉ nhập |
| 33 | `GET /api/v1/stock-transactions?referenceType=ImportReceipt&referenceId={id}` | | Theo chứng từ |
| 34 | `GET /api/v1/stock-transactions/{id}` | | `quantityBefore` → `quantityChange` → `quantityAfter` |

### H. Cổng Customer (smoke)

| # | API | Body | Kỳ vọng |
|---|-----|------|---------|
| 35 | `POST /api/v1/auth/customer/login` | `customer1` / `123456` | Có token, portal customer |
| 36 | Dùng token Customer gọi `GET /api/v1/products` | | **401/403** (không thuộc AdminPortal) |
| 37 | `admin` login vào `customer/login` | | **401** sai cổng |

---

## 7. Body mẫu nhanh

### Product

```json
{
  "sku": "SP-100",
  "name": "Gao ST25",
  "description": "Gao thom",
  "price": 25000,
  "minStock": 10,
  "categoryId": 1,
  "unitId": 1,
  "isActive": true
}
```

### Import receipt

```json
{
  "warehouseId": 1,
  "supplierId": 1,
  "note": "Nhap lo 1",
  "details": [
    { "productId": 1, "quantity": 50, "unitPrice": 20000 }
  ]
}
```

### Export receipt

```json
{
  "warehouseId": 1,
  "customerId": 1,
  "note": "Xuat don 1",
  "details": [
    { "productId": 1, "quantity": 5, "unitPrice": 30000 }
  ]
}
```

### Stocktake

```json
{
  "warehouseId": 1,
  "note": "Kiem ke thang 11",
  "details": [
    { "productId": 1, "countedQuantity": 48, "note": "Thieu 2" }
  ]
}
```

### Upload (Swagger)

- **Products images:** `POST .../images` → chọn **files** (array), Execute.
- **Customer avatar:** `POST .../avatar` → chọn **file**.

Ảnh sau upload mở: `http://localhost:5080/uploads/products/...` hoặc `/uploads/avatars/...`.

---

## 8. Trạng thái phiếu (Import / Export / Stocktake)

| Status | Ý nghĩa | Sửa / Xóa | Tác động tồn |
|--------|---------|-----------|--------------|
| `Draft` | Nháp | Có | Chưa |
| `Confirmed` | Đã duyệt | Không | Đã cộng/trừ/gán |
| `Cancelled` | Đã hủy | Không | Hoàn (nếu từng Confirm) |

---

## 9. StockTransactionType

| Type | Khi nào |
|------|---------|
| `Import` | Confirm phiếu nhập |
| `ImportCancel` | Hủy phiếu nhập đã Confirm |
| `Export` | Confirm phiếu xuất |
| `ExportCancel` | Hủy phiếu xuất đã Confirm |
| `Stocktake` | Confirm kiểm kê |
| `StocktakeCancel` | Hủy kiểm kê đã Confirm |
| `Manual` | CRUD `warehouse-stocks` |

---

## 10. Lỗi thường gặp khi debug

| Hiện tượng | Nguyên nhân / xử lý |
|------------|---------------------|
| 401 mọi API | Quên Authorize / token hết hạn (120 phút) / sai format Bearer |
| 401 login | Sai mật khẩu hoặc đăng nhập nhầm cổng Admin ↔ Customer |
| 409 SKU / Code / Name | Trùng unique — đổi mã |
| 409 đủ tồn khi xuất | Giảm `quantity` hoặc nhập thêm trước |
| 409 Draft only | Phiếu đã Confirm/Cancelled — tạo phiếu mới |
| Upload 400 | Sai định dạng ảnh / file trống / > 2MB |
| Không thấy Swagger | Chưa `Development` hoặc chưa `UseSwagger` — chạy profile Development |
| Migration fail | SQL Server chưa chạy / sai port `1434` / sai password `sa` |
| Port đang dùng | `lsof -ti:5080 \| xargs kill -9` rồi `dotnet run` lại |

---

## 11. Cấu trúc solution (học / debug)

```text
WarehouseManagement.Domain          Entity, enum
WarehouseManagement.Application     DTO, Interface, Service (nghiệp vụ)
WarehouseManagement.Infrastructure  EF Core, Repository, JWT, File storage, Seeder
WarehouseManagement.WebAPI          Controllers, Swagger, Program.cs
WarehouseManagement.Shared          ApiResponse, Exception, AppRoles, Pagination
```

- Route chung admin: `AdminApiController` → `[Authorize] + api/v1/[controller]`.
- Biến động tồn tập trung: `StockMovementService` (ghi `WarehouseStocks` + `StockTransactions`).

---

## 12. Lệnh hữu ích

```bash
# Build
dotnet build backend/src/WarehouseManagement.WebAPI

# Migration mới (sau khi sửa entity)
cd backend/src/WarehouseManagement.WebAPI
dotnet ef migrations add TenMigration \
  --project ../WarehouseManagement.Infrastructure/WarehouseManagement.Infrastructure.csproj \
  --output-dir Persistence/Migrations
dotnet ef database update \
  --project ../WarehouseManagement.Infrastructure/WarehouseManagement.Infrastructure.csproj

# Chạy cố định port
dotnet run --urls "http://localhost:5080"
```

---

**Gợi ý test nhanh end-to-end:** Login admin → tạo Category/Unit/Warehouse/Supplier/Customer/Product → Import confirm → xem `warehouse-stocks` & `stock-transactions` → Export confirm → Stocktake confirm → `stocktake-details?onlyDiff=true`.
