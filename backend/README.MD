# Warehouse Management API — Kho nhà thuốc bệnh viện

Backend .NET (Clean Architecture) + JWT cho hệ thống **quản lý kho thuốc / vật tư y tế bệnh viện**:
danh mục thuốc, tồn kho theo kho (thường, lạnh, gây nghiện…), nhập xuất, kiểm kê, nhà cung cấp dược, khoa/phòng nhận thuốc.

**Mọi API quản trị phải có JWT.** Trên Swagger: **login chưa đủ** — còn phải bấm **Authorize**.

| Mục | URL / lệnh |
|-----|------------|
| API | `http://localhost:5136` |
| Swagger UI | [http://localhost:5136/swagger](http://localhost:5136/swagger) |
| Chạy | `dotnet run --launch-profile http` (trong `WarehouseManagement.WebAPI`) |

---

## 0. Test Swagger trong 2 phút (làm đúng thứ tự)

### Bước A — Chạy API

```bash
cd backend/src/WarehouseManagement.WebAPI
dotnet run --launch-profile http
```

Mở trình duyệt: **http://localhost:5136/swagger**  
(Nếu vừa sửa code: **Cmd+Shift+R** hard-refresh.)

### Bước B — Lấy token (Auth)

1. Tìm nhóm **Auth** → `POST /api/v1/auth/admin/login`
2. **Try it out** → body:

```json
{
  "username": "admin",
  "password": "123456"
}
```

3. **Execute** → Response **200**
4. Copy **chỉ** chuỗi `data.token` (bắt đầu bằng `eyJ...`)

### Bước C — Authorize

Sau login thành công, Swagger **tự gắn JWT** (script `auto-authorize.js`).  
Nếu vẫn 401: bấm **Authorize** (ổ khóa) → dán **chỉ token** (`eyJ...`, không gõ `Bearer`) → Authorize → Close.

### Bước D — Kiểm tra đã gắn JWT chưa

`GET /api/v1/products` → **Execute** → mở **Curl**. **Đúng** phải có:

```bash
-H 'Authorization: Bearer eyJ...'
```

- Có dòng này + Code **200** → OK.
- **401** / thiếu `Authorization` → hard-refresh (`Cmd+Shift+R`) → login lại → kiểm tra Console có `[swagger] Đã tự Authorize JWT`.

---

## 1. Yêu cầu môi trường

| Thành phần | Ghi chú |
|------------|---------|
| .NET SDK | net10 |
| SQL Server | Ví dụ `localhost,1434` — xem `appsettings.json` |
| IDE | Visual Studio / Rider / Cursor + C# Dev Kit |

Connection string mẫu:

```text
Server=localhost,1434;Database=WareDbnew1;User Id=sa;Password=123456Aa@;TrustServerCertificate=True;Encrypt=False;
```

Migration (lần đầu / sau đổi model):

```bash
cd backend/src/WarehouseManagement.WebAPI
dotnet ef database update --project ../WarehouseManagement.Infrastructure/WarehouseManagement.Infrastructure.csproj
```

Khi start, `DataSeeder` tự tạo role + user mẫu.

---

## 2. Tài khoản seed (kho nhà thuốc bệnh viện)

| Username | Password | Vai trò demo | Login |
|----------|----------|--------------|-------|
| `admin` | `123456` | Quản trị dược | `POST /api/v1/auth/admin/login` |
| `manager` | `123456` | Trưởng khoa dược | admin/login |
| `staff` | `123456` | Dược sĩ kho | admin/login |
| `customer1` | `123456` | Khoa/phòng nhận thuốc (cổng nhà thuốc BV) | `POST /api/v1/auth/customer/login` (**không** gọi API quản trị) |

Mỗi lần API khởi động, seeder **làm mới nhãn** Categories / Products / Warehouses / Suppliers / Customers sang ngữ cảnh kho dược (kể cả DB đã seed dữ liệu cũ).  
Seeder mục tiêu **~500 bản ghi / bảng** nghiệp vụ (xem §10.7).

**SQL — đối chiếu tài khoản / role seed:**

```sql
-- 4 role hệ thống
SELECT Id, Name FROM Roles ORDER BY Id;

-- User demo đăng nhập (admin / manager / staff / customer1)
SELECT u.Id, u.Username, u.FullName, u.Email, r.Name AS RoleName, u.IsActive
FROM Users u
INNER JOIN Roles r ON r.Id = u.RoleId
WHERE u.Username IN (N'admin', N'manager', N'staff', N'customer1')
ORDER BY u.Username;

-- Đếm user theo role
SELECT r.Name AS RoleName, COUNT(*) AS SoUser
FROM Users u
INNER JOIN Roles r ON r.Id = u.RoleId
GROUP BY r.Name
ORDER BY SoUser DESC;
```

---

## 3. Test CRUD trên Swagger (ví dụ Products)

> Làm **sau khi đã Authorize** (mục 0).  
> Categories / Units / Warehouses / Suppliers… thao tác giống hệt.

### 3.1. GET — danh sách / chi tiết

| API | Cách bấm | Kỳ vọng |
|-----|----------|---------|
| `GET /api/v1/products` | Try it out → **Execute** (không cần điền query; mặc định trang 1, size 10) | **200** |
| `GET /api/v1/products/{id}` | Điền `id` (vd `1`) → Execute | **200** |

### 3.2. POST — tạo

`POST /api/v1/products` → Try it out → body (đổi `sku` nếu trùng):

```json
{
  "sku": "SP-TEST-001",
  "name": "San pham test",
  "description": "Thu Swagger",
  "price": 25000,
  "minStock": 5,
  "categoryId": 1,
  "unitId": 1,
  "isActive": true
}
```

**Execute** → **201** → copy `data.id`.

> Nếu `categoryId` / `unitId` không tồn tại: gọi trước  
> `GET /api/v1/categories` và `GET /api/v1/units` → lấy `data.items[0].id`.

### 3.3. PUT — sửa

`PUT /api/v1/products/{id}` → điền `id` → body (sửa `name` / `price`):

```json
{
  "sku": "SP-TEST-001",
  "name": "San pham test (da sua)",
  "description": "Cap nhat Swagger",
  "price": 30000,
  "minStock": 5,
  "categoryId": 1,
  "unitId": 1,
  "isActive": true
}
```

**Execute** → **200**.

### 3.4. DELETE — xóa

`DELETE /api/v1/products/{id}` → điền `id` → Execute → **204** (không body).  
Gọi lại `GET .../{id}` → **404**.

### 3.5. Bảng nhớ nhanh

| Method | Cần trên Swagger |
|--------|------------------|
| **GET** list | Authorize → Execute |
| **GET** by id | Authorize → điền `id` → Execute |
| **POST** | Authorize → JSON body → Execute |
| **PUT** | Authorize → `id` + JSON body → Execute |
| **DELETE** | Authorize → `id` → Execute |

---

## 4. Quy ước response & mã lỗi

```json
{
  "success": true,
  "message": "...",
  "data": { }
}
```

| HTTP | Ý nghĩa |
|------|---------|
| 200 / 201 | Thành công |
| 204 | Xóa thành công (No Content) |
| 400 | Validation / dữ liệu sai |
| 401 | **Chưa Authorize** / token sai / hết hạn / sai cổng login |
| 403 | Sai role (vd token Customer gọi API Admin) |
| 404 | Không tìm thấy |
| 409 | Trùng mã / không đủ tồn / phiếu không còn Draft |

Base route: `/api/v1/[controller]` (kebab-case: `warehouse-stocks`, `import-receipts`, …).  
Query phân trang: `search`, `pageNumber` (mặc định 1), `pageSize` (mặc định 10).

---

## 5. Lỗi Swagger thường gặp (xử lý ngay)

| Hiện tượng | Nguyên nhân | Cách sửa |
|------------|-------------|----------|
| **401** mọi GET/POST/PUT/DELETE | Quên **Authorize**, hoặc Curl **không** có `Authorization` | Làm lại mục **0 — Bước B + C**. Xem Curl phải có Bearer |
| Login **200** nhưng CRUD vẫn **401** | Chỉ login, chưa bấm ổ khóa Authorize | Bấm **Authorize**, dán token, Authorize → Close |
| Dán `Bearer eyJ...` vào ô Authorize vẫn lỗi | Swagger đã tự thêm `Bearer` → bị `Bearer Bearer ...` | Chỉ dán `eyJ...` |
| Swagger trắng / schema cũ | Cache trình duyệt sau khi sửa API | **Cmd+Shift+R**; hoặc mở `/swagger/index.html` |
| Không mở được Swagger | API chưa chạy / sai cổng | `dotnet run --launch-profile http` → cổng **5136** (không phải 5080) |
| Port đang dùng | Process cũ còn giữ 5136 | `lsof -ti:5136 \| xargs kill -9` rồi `dotnet run` lại |
| **409** SKU / Name | Trùng unique | Đổi `sku` / `name` |
| **400** upload ảnh | Sai định dạng / trống / > 2MB | `.jpg/.jpeg/.png/.webp`, ≤ 2MB |
| Frontend Vite `ECONNREFUSED` | API tắt lúc proxy gọi | Chạy backend 5136 trước; frontend proxy → `127.0.0.1:5136` |

**Tự kiểm tra nhanh bằng terminal** (không cần Swagger):

```bash
# 1) Login
TOKEN=$(curl -s -X POST http://localhost:5136/api/v1/auth/admin/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"123456"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['token'])")

# 2) GET products (phải 200)
curl -s -o /dev/null -w "%{http_code}\n" \
  -H "Authorization: Bearer $TOKEN" \
  'http://localhost:5136/api/v1/products?pageNumber=1&pageSize=5'
```

---

## 6. Checklist test nghiệp vụ (sau khi Authorize admin)

Ghi lại `id` trả về (`categoryId`, `unitId`, `warehouseId`, `productId`, …).

### A. Danh mục nền

| # | API | Ghi chú | Kỳ vọng |
|---|-----|---------|---------|
| 1 | `GET /api/v1/roles` | | Có Admin, Manager, Staff, Customer |
| 2 | `GET /api/v1/users` | | Có user seed |
| 3 | `POST /api/v1/categories` | `{"name":"Thực phẩm","description":"DM test","isActive":true}` | 201 |
| 4 | `POST /api/v1/units` | `{"name":"Thùng","symbol":"thung","isActive":true}` | 201 |
| 5 | `POST /api/v1/suppliers` | `{"name":"NCC A","phone":"0912345678","isActive":true}` | 201 |
| 6 | `POST /api/v1/customers` | Có thể kèm `username`/`password`/`email` tạo User Customer | 201 |
| 7 | `POST /api/v1/customers/{id}/avatar` | form `file` | `/uploads/avatars/...` |
| 8 | `POST /api/v1/warehouses` | `{"code":"WH-02","name":"Kho HCM","isActive":true}` | 201 |

```sql
-- Số liệu danh mục nền (sau seed / sau khi POST thử)
SELECT 'Roles' AS Bang, COUNT(*) AS SoLuong FROM Roles
UNION ALL SELECT 'Users', COUNT(*) FROM Users
UNION ALL SELECT 'Categories', COUNT(*) FROM Categories
UNION ALL SELECT 'Units', COUNT(*) FROM Units
UNION ALL SELECT 'Suppliers', COUNT(*) FROM Suppliers
UNION ALL SELECT 'Customers', COUNT(*) FROM Customers
UNION ALL SELECT 'Warehouses', COUNT(*) FROM Warehouses;

-- Kho vừa tạo (ví dụ WH-02)
SELECT Id, Code, Name, IsActive FROM Warehouses WHERE Code = N'WH-02';
```

### B. Sản phẩm + ảnh

| # | API | Ghi chú | Kỳ vọng |
|---|-----|---------|---------|
| 9 | `POST /api/v1/products` | `sku`, `name`, `price`, `minStock`, `categoryId`, `unitId` | 201 |
| 10 | `POST /api/v1/products/{id}/images` | form `files` | Ảnh; primary |
| 11 | `PUT .../images/{imageId}/primary` | | Đổi ảnh chính |
| 12 | `DELETE .../images/{imageId}` | | Xóa ảnh |

```sql
-- Đếm sản phẩm / ảnh; seed chuẩn SP0001…SP0500
SELECT COUNT(*) AS TongSanPham FROM Products;
SELECT COUNT(*) AS TongAnh FROM ProductImages;
SELECT COUNT(*) AS SpSeedChuan FROM Products WHERE Sku LIKE 'SP[0-9][0-9][0-9][0-9]';

-- Chi tiết 1 SP + ảnh primary (đổi @Id)
DECLARE @Id INT = 1;
SELECT p.Id, p.Sku, p.Name, p.Price, p.MinStock, p.IsActive,
       (SELECT COUNT(*) FROM ProductImages i WHERE i.ProductId = p.Id) AS SoAnh,
       (SELECT TOP 1 ImageUrl FROM ProductImages i WHERE i.ProductId = p.Id AND i.IsPrimary = 1) AS AnhChinh
FROM Products p WHERE p.Id = @Id;
```

### C. Tồn thủ công

| # | API | Kỳ vọng |
|---|-----|---------|
| 13 | `POST /api/v1/warehouse-stocks` | Manual TX |
| 14 | `PUT /api/v1/warehouse-stocks/{id}` | Cập nhật tồn |
| 15 | `GET /api/v1/warehouse-stocks?lowStockOnly=true` | `isLowStock` |

```sql
-- Tóm tắt tồn + tồn thấp (cùng logic API)
SELECT COUNT(*) AS DongTon FROM WarehouseStocks;
SELECT SUM(Quantity) AS TongSoLuong FROM WarehouseStocks;

SELECT COUNT(*) AS TonThap
FROM WarehouseStocks ws
INNER JOIN Products p ON p.Id = ws.ProductId
WHERE ISNULL(ws.MinQuantity, p.MinStock) > 0
  AND ws.Quantity <= ISNULL(ws.MinQuantity, p.MinStock);

SELECT COUNT(*) AS HetHang FROM WarehouseStocks WHERE Quantity <= 0;

-- Giao dịch Manual gần nhất
SELECT TOP 20 Id, WarehouseId, ProductId, QuantityChange, QuantityAfter, Note, CreatedAt
FROM StockTransactions
WHERE [Type] = 7  -- Manual
ORDER BY Id DESC;
```

### D. Nhập kho

Draft → Confirm (cộng tồn) → Cancel (hoàn).

| # | API | Kỳ vọng |
|---|-----|---------|
| 16 | `POST /api/v1/import-receipts` | Draft, mã `PN-...` |
| 17 | `PUT /api/v1/import-receipts/{id}` | Chỉ Draft |
| 18 | `POST .../{id}/confirm` | Confirmed + tồn tăng |
| 19 | `POST .../{id}/cancel` | Hoàn tồn |
| 20 | `DELETE .../{id}` | Chỉ Draft → 204 |

```sql
-- Đếm phiếu nhập theo trạng thái (0 Draft, 1 Confirmed, 2 Cancelled)
SELECT
  CASE Status WHEN 0 THEN N'Draft' WHEN 1 THEN N'Confirmed' WHEN 2 THEN N'Cancelled' END AS TrangThai,
  COUNT(*) AS SoPhieu
FROM ImportReceipts
GROUP BY Status;

-- Chi tiết 1 phiếu (đổi @Code hoặc @Id)
DECLARE @Code NVARCHAR(50) = N'PN-SEED-0001';
SELECT r.Id, r.Code, r.Status, r.WarehouseId, r.SupplierId, r.ReceiptDate,
       (SELECT COUNT(*) FROM ImportReceiptDetails d WHERE d.ImportReceiptId = r.Id) AS SoDong,
       (SELECT SUM(Quantity) FROM ImportReceiptDetails d WHERE d.ImportReceiptId = r.Id) AS TongSl
FROM ImportReceipts r WHERE r.Code = @Code;

-- TX Import / ImportCancel gắn phiếu
SELECT Id, [Type], ProductId, QuantityChange, ReferenceCode, CreatedAt
FROM StockTransactions
WHERE ReferenceType = N'ImportReceipt' AND ReferenceCode = @Code
ORDER BY Id;
```

### E. Xuất kho

| # | API | Kỳ vọng |
|---|-----|---------|
| 21 | `POST /api/v1/export-receipts` | Draft `PX-...` |
| 22 | `POST .../{id}/confirm` | Đủ tồn → trừ |
| 23 | Confirm quá tồn | **409** |
| 24 | `POST .../{id}/cancel` | Hoàn tồn |

```sql
SELECT
  CASE Status WHEN 0 THEN N'Draft' WHEN 1 THEN N'Confirmed' WHEN 2 THEN N'Cancelled' END AS TrangThai,
  COUNT(*) AS SoPhieu
FROM ExportReceipts
GROUP BY Status;

SELECT
  CASE Source WHEN 0 THEN N'Manual' WHEN 1 THEN N'Store' ELSE CONCAT(N'Other_', Source) END AS Nguon,
  COUNT(*) AS SoPhieu
FROM ExportReceipts
GROUP BY Source;

DECLARE @Px NVARCHAR(50) = N'PX-SEED-0001';
SELECT r.Id, r.Code, r.Status, r.Source, r.WarehouseId, r.CustomerId,
       (SELECT SUM(Quantity) FROM ExportReceiptDetails d WHERE d.ExportReceiptId = r.Id) AS TongSl
FROM ExportReceipts r WHERE r.Code = @Px;
```

### F. Kiểm kê

| # | API | Kỳ vọng |
|---|-----|---------|
| 25 | `POST /api/v1/stocktakes` | Snapshot system/counted/diff |
| 26–28 | CRUD details / `onlyDiff=true` | Lệch tồn |
| 29 | `POST .../{id}/confirm` | Gán tồn = counted |
| 30 | `POST .../{id}/cancel` | Hoàn system |

```sql
SELECT
  CASE Status WHEN 0 THEN N'Draft' WHEN 1 THEN N'Confirmed' WHEN 2 THEN N'Cancelled' END AS TrangThai,
  COUNT(*) AS SoPhieu
FROM Stocktakes
GROUP BY Status;

-- Dòng lệch (diff <> 0) trên phiếu gần nhất
SELECT TOP 50 st.Code, d.ProductId, d.SystemQuantity, d.CountedQuantity,
       (d.CountedQuantity - d.SystemQuantity) AS Diff
FROM StocktakeDetails d
INNER JOIN Stocktakes st ON st.Id = d.StocktakeId
WHERE d.CountedQuantity <> d.SystemQuantity
ORDER BY st.Id DESC, ABS(d.CountedQuantity - d.SystemQuantity) DESC;
```

### G. Lịch sử tồn

| # | API | Kỳ vọng |
|---|-----|---------|
| 31–34 | `GET /api/v1/stock-transactions` | Import/Export/Stocktake/Manual |

```sql
SELECT COUNT(*) AS TongGiaoDich FROM StockTransactions;

SELECT
  CASE [Type]
    WHEN 1 THEN 'Import' WHEN 2 THEN 'ImportCancel'
    WHEN 3 THEN 'Export' WHEN 4 THEN 'ExportCancel'
    WHEN 5 THEN 'Stocktake' WHEN 6 THEN 'StocktakeCancel'
    WHEN 7 THEN 'Manual' ELSE CONCAT('Other_', [Type])
  END AS Loai,
  COUNT(*) AS SoLuong,
  SUM(QuantityChange) AS TongThayDoi
FROM StockTransactions
GROUP BY [Type]
ORDER BY SoLuong DESC;
```

### H. Cổng Customer (smoke)

| # | API | Kỳ vọng |
|---|-----|---------|
| 35 | `customer/login` với `customer1` | Token customer |
| 36 | Token Customer → `GET /api/v1/products` | 401/403 |
| 37 | `admin` vào `customer/login` | 401 sai cổng |

```sql
-- Xác nhận customer1 thuộc role Customer
SELECT u.Username, r.Name AS RoleName, u.IsActive
FROM Users u
INNER JOIN Roles r ON r.Id = u.RoleId
WHERE u.Username = N'customer1';
```

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

### Import / Export / Stocktake

```json
{
  "warehouseId": 1,
  "supplierId": 1,
  "note": "Nhap lo 1",
  "details": [{ "productId": 1, "quantity": 50, "unitPrice": 20000 }]
}
```

```json
{
  "warehouseId": 1,
  "customerId": 1,
  "note": "Xuat don 1",
  "details": [{ "productId": 1, "quantity": 5, "unitPrice": 30000 }]
}
```

```json
{
  "warehouseId": 1,
  "note": "Kiem ke",
  "details": [{ "productId": 1, "countedQuantity": 48, "note": "Thieu 2" }]
}
```

Upload: Products `files` / Customer `file` → xem tại `http://localhost:5136/uploads/...`.

---

## 8. Trạng thái phiếu & loại giao dịch

| Status | Giá trị DB | Sửa/Xóa | Tồn |
|--------|------------|---------|-----|
| `Draft` | `0` | Có | Chưa đổi |
| `Confirmed` | `1` | Không | Đã cộng/trừ/gán |
| `Cancelled` | `2` | Không | Đã hoàn (nếu từng Confirm) |

| StockTransactionType | Giá trị DB | Khi nào |
|----------------------|------------|---------|
| `Import` | `1` | Confirm phiếu nhập |
| `ImportCancel` | `2` | Hủy phiếu nhập đã Confirm |
| `Export` | `3` | Confirm phiếu xuất |
| `ExportCancel` | `4` | Hủy phiếu xuất đã Confirm |
| `Stocktake` | `5` | Confirm kiểm kê |
| `StocktakeCancel` | `6` | Hủy kiểm kê đã Confirm |
| `Manual` | `7` | Mở sổ / điều chỉnh `warehouse-stocks` |

```sql
-- Số phiếu theo trạng thái (3 loại chứng từ)
SELECT N'Import' AS LoaiPhieu,
  SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS Draft,
  SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS Confirmed,
  SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS Cancelled
FROM ImportReceipts
UNION ALL
SELECT N'Export',
  SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END),
  SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END),
  SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END)
FROM ExportReceipts
UNION ALL
SELECT N'Stocktake',
  SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END),
  SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END),
  SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END)
FROM Stocktakes;

-- Số giao dịch theo Type (1..7)
SELECT [Type], COUNT(*) AS SoLuong FROM StockTransactions GROUP BY [Type] ORDER BY [Type];
```

---

## 9. Debug IDE

1. Breakpoint trong Service/Controller (vd `ImportReceiptService.ConfirmAsync`).
2. F5 project **WarehouseManagement.WebAPI**.
3. Gọi API từ Swagger → dừng tại breakpoint.

| Nghiệp vụ | File |
|-----------|------|
| Login JWT | `AuthService.cs` |
| Cộng/trừ tồn | `StockMovementService.cs` |
| Confirm nhập/xuất/kiểm kê | `*Service.ConfirmAsync` |

---

## 10. SQL — đếm số lượng (Dashboard, seed, đối chiếu UI)

> Chạy trên SQL Server (SSMS / Azure Data Studio / `sqlcmd`).  
> Database mặc định theo `appsettings.json` (ví dụ `WareDbnew1`).  
> Enum trạng thái phiếu: `0 = Draft`, `1 = Confirmed`, `2 = Cancelled`.  
> **Quy ước README:** chỗ nào nêu số liệu / KPI thì có SQL đối chiếu ngay bên cạnh (mục này + §2, §6, §8, §11).  
> **File SQL đầy đủ (gom 1 chỗ):** [`scripts/WarehouseFullQueries.sql`](scripts/WarehouseFullQueries.sql) — mở file, `USE` đúng DB, chỉnh biến mục 0, chạy từng batch.

### 10.1. KPI trên Dashboard (thẻ số)

> **Đầy đủ map UI ↔ SQL:** mục **§2** trong [`scripts/WarehouseFullQueries.sql`](scripts/WarehouseFullQueries.sql)  
> (Hero, Chỉ số nhanh, Danh mục, pie/bar, tồn thấp TOP 8, 50 giao dịch gần nhất).

| Thẻ UI | SQL |
|--------|-----|
| Hero: Nhập đã XN | `ImportReceipts WHERE Status=1` |
| Hero: Xuất đã XN | `ExportReceipts WHERE Status=1` |
| Hero / KPI: Tồn thấp | `WarehouseStocks` + `EffectiveMin` |
| Hero: KK đã XN | `Stocktakes WHERE Status=1` |
| KPI: Thuốc / vật tư | `COUNT(*) FROM Products` |
| KPI: Kho dược | `COUNT(*) FROM Warehouses` |
| KPI: Phiếu nhập nháp | `ImportReceipts WHERE Status=0` |
| KPI: Phiếu xuất nháp | `ExportReceipts WHERE Status=0` |
| KPI: Giao dịch kho | `COUNT(*) FROM StockTransactions` |
| Chip: Nhóm thuốc / NCC / Khoa phòng | `Categories` / `Suppliers` / `Customers` |

```sql
-- Gom nhanh (đủ Hero + KPI chính). Chi tiết từng khối: WarehouseFullQueries.sql §2
SELECT
  (SELECT COUNT(*) FROM Products) AS ThuocVatTu,
  (SELECT COUNT(*) FROM Warehouses) AS KhoDuoc,
  (SELECT COUNT(*)
   FROM WarehouseStocks ws
   INNER JOIN Products p ON p.Id = ws.ProductId
   WHERE ISNULL(ws.MinQuantity, p.MinStock) > 0
     AND ws.Quantity <= ISNULL(ws.MinQuantity, p.MinStock)) AS TonThap,
  (SELECT COUNT(*) FROM ImportReceipts WHERE Status = 0) AS NhapNhap,
  (SELECT COUNT(*) FROM ImportReceipts WHERE Status = 1) AS NhapDaXN,
  (SELECT COUNT(*) FROM ExportReceipts WHERE Status = 0) AS XuatNhap,
  (SELECT COUNT(*) FROM ExportReceipts WHERE Status = 1) AS XuatDaXN,
  (SELECT COUNT(*) FROM Stocktakes WHERE Status = 0) AS KkNhap,
  (SELECT COUNT(*) FROM Stocktakes WHERE Status = 1) AS KkDaXN,
  (SELECT COUNT(*) FROM StockTransactions) AS GiaoDichKho,
  (SELECT COUNT(*) FROM Categories) AS NhomThuoc,
  (SELECT COUNT(*) FROM Suppliers) AS NccDuoc,
  (SELECT COUNT(*) FROM Customers) AS KhoaPhong;
```

### 10.2. Biểu đồ “Cơ cấu danh mục master data”

```sql
SELECT 'San pham'   AS Loai, COUNT(*) AS SoLuong FROM Products
UNION ALL SELECT 'Kho',         COUNT(*) FROM Warehouses
UNION ALL SELECT 'Khach hang',  COUNT(*) FROM Customers
UNION ALL SELECT 'NCC',         COUNT(*) FROM Suppliers
UNION ALL SELECT 'Danh muc SP', COUNT(*) FROM Categories;
```

### 10.3. Biểu đồ trạng thái phiếu nhập / xuất / kiểm kê

```sql
-- Nhập
SELECT
  CASE Status WHEN 0 THEN 'Nhap Draft' WHEN 1 THEN 'Nhap Confirmed' WHEN 2 THEN 'Nhap Cancelled' END AS Nhan,
  COUNT(*) AS SoLuong
FROM ImportReceipts
GROUP BY Status;

-- Xuất
SELECT
  CASE Status WHEN 0 THEN 'Xuat Draft' WHEN 1 THEN 'Xuat Confirmed' WHEN 2 THEN 'Xuat Cancelled' END AS Nhan,
  COUNT(*) AS SoLuong
FROM ExportReceipts
GROUP BY Status;

-- Kiểm kê
SELECT
  CASE Status WHEN 0 THEN 'Kiem ke Draft' WHEN 1 THEN 'Kiem ke Confirmed' WHEN 2 THEN 'Kiem ke Cancelled' END AS Nhan,
  COUNT(*) AS SoLuong
FROM Stocktakes
GROUP BY Status;

-- Gộp 1 bảng (giống chart Dashboard)
SELECT Nhan, SoLuong FROM (
  SELECT CASE Status WHEN 0 THEN N'Nhập Draft' WHEN 1 THEN N'Nhập Confirmed' WHEN 2 THEN N'Nhập Cancelled' END AS Nhan, COUNT(*) AS SoLuong, 1 AS ThuTu FROM ImportReceipts GROUP BY Status
  UNION ALL
  SELECT CASE Status WHEN 0 THEN N'Xuất Draft' WHEN 1 THEN N'Xuất Confirmed' WHEN 2 THEN N'Xuất Cancelled' END, COUNT(*), 2 FROM ExportReceipts GROUP BY Status
  UNION ALL
  SELECT CASE Status WHEN 0 THEN N'Kiểm kê Draft' WHEN 1 THEN N'Kiểm kê Confirmed' WHEN 2 THEN N'Kiểm kê Cancelled' END, COUNT(*), 3 FROM Stocktakes GROUP BY Status
) x
ORDER BY ThuTu, Nhan;
```

### 10.4. Đếm toàn bộ bảng nghiệp vụ

```sql
SELECT 'Roles'              AS Bang, COUNT(*) AS SoLuong FROM Roles
UNION ALL SELECT 'Users',              COUNT(*) FROM Users
UNION ALL SELECT 'Categories',         COUNT(*) FROM Categories
UNION ALL SELECT 'Units',              COUNT(*) FROM Units
UNION ALL SELECT 'Suppliers',          COUNT(*) FROM Suppliers
UNION ALL SELECT 'Customers',          COUNT(*) FROM Customers
UNION ALL SELECT 'Warehouses',         COUNT(*) FROM Warehouses
UNION ALL SELECT 'Products',           COUNT(*) FROM Products
UNION ALL SELECT 'ProductImages',      COUNT(*) FROM ProductImages
UNION ALL SELECT 'WarehouseStocks',    COUNT(*) FROM WarehouseStocks
UNION ALL SELECT 'ImportReceipts',     COUNT(*) FROM ImportReceipts
UNION ALL SELECT 'ImportReceiptDetails', COUNT(*) FROM ImportReceiptDetails
UNION ALL SELECT 'ExportReceipts',     COUNT(*) FROM ExportReceipts
UNION ALL SELECT 'ExportReceiptDetails', COUNT(*) FROM ExportReceiptDetails
UNION ALL SELECT 'Stocktakes',         COUNT(*) FROM Stocktakes
UNION ALL SELECT 'StocktakeDetails',   COUNT(*) FROM StocktakeDetails
UNION ALL SELECT 'StockTransactions',  COUNT(*) FROM StockTransactions
UNION ALL SELECT 'Invoices',           COUNT(*) FROM Invoices
UNION ALL SELECT 'InvoiceDetails',     COUNT(*) FROM InvoiceDetails
UNION ALL SELECT 'MediaAssets',        COUNT(*) FROM MediaAssets
ORDER BY Bang;
```

### 10.5. Truy vấn chi tiết hay dùng khi đối chiếu UI

```sql
-- Danh sách dòng tồn thấp (giống trang Stocks?lowStockOnly=true)
SELECT
  ws.Id,
  w.Code AS WarehouseCode,
  p.Sku,
  p.Name AS ProductName,
  ws.Quantity,
  ISNULL(ws.MinQuantity, p.MinStock) AS EffectiveMinStock
FROM WarehouseStocks ws
INNER JOIN Warehouses w ON w.Id = ws.WarehouseId
INNER JOIN Products p ON p.Id = ws.ProductId
WHERE ISNULL(ws.MinQuantity, p.MinStock) > 0
  AND ws.Quantity <= ISNULL(ws.MinQuantity, p.MinStock)
ORDER BY ws.Quantity ASC;

-- Phân bố loại giao dịch
SELECT
  CASE [Type]
    WHEN 1 THEN 'Import'
    WHEN 2 THEN 'ImportCancel'
    WHEN 3 THEN 'Export'
    WHEN 4 THEN 'ExportCancel'
    WHEN 5 THEN 'Stocktake'
    WHEN 6 THEN 'StocktakeCancel'
    WHEN 7 THEN 'Manual'
    ELSE CONCAT('Other_', [Type])
  END AS Loai,
  COUNT(*) AS SoLuong
FROM StockTransactions
GROUP BY [Type]
ORDER BY SoLuong DESC;

-- Phiếu / hóa đơn seed demo
SELECT 'PN-SEED' AS Prefix, COUNT(*) AS SoLuong FROM ImportReceipts WHERE Code LIKE 'PN-SEED-%'
UNION ALL SELECT 'PX-SEED', COUNT(*) FROM ExportReceipts WHERE Code LIKE 'PX-SEED-%'
UNION ALL SELECT 'KK-SEED', COUNT(*) FROM Stocktakes WHERE Code LIKE 'KK-SEED-%'
UNION ALL SELECT 'HDN-SEED', COUNT(*) FROM ImportReceipts WHERE PurchaseInvoiceCode LIKE 'HDN-SEED-%';
```

### 10.6. Đối chiếu nhập / xuất / tồn (trang Stocks)

```sql
-- Theo từng SKU tại 1 kho (đổi @WarehouseId)
DECLARE @WarehouseId INT = 1;

;WITH Tx AS (
  SELECT
    ProductId,
    SUM(CASE WHEN [Type] IN (1, 2) THEN QuantityChange ELSE 0 END) AS SlNhap,          -- Import / ImportCancel
    SUM(CASE WHEN [Type] IN (3, 4) THEN -QuantityChange ELSE 0 END) AS SlXuat,         -- Export net (dương)
    SUM(CASE WHEN [Type] IN (5, 6, 7) THEN QuantityChange ELSE 0 END) AS SlDieuChinh   -- Stocktake / Manual
  FROM StockTransactions
  WHERE WarehouseId = @WarehouseId
  GROUP BY ProductId
)
SELECT
  p.Sku,
  p.Name,
  ISNULL(t.SlNhap, 0) AS SlNhap,
  ISNULL(t.SlXuat, 0) AS SlXuat,
  ISNULL(t.SlDieuChinh, 0) AS SlDieuChinh,
  ISNULL(t.SlNhap, 0) - ISNULL(t.SlXuat, 0) + ISNULL(t.SlDieuChinh, 0) AS SoTinh,
  ISNULL(ws.Quantity, 0) AS TonKho,
  ISNULL(ws.Quantity, 0) - (ISNULL(t.SlNhap, 0) - ISNULL(t.SlXuat, 0) + ISNULL(t.SlDieuChinh, 0)) AS ChenhLech,
  CASE
    WHEN ISNULL(t.SlXuat, 0) > ISNULL(t.SlNhap, 0)
      THEN ISNULL(t.SlXuat, 0) - ISNULL(t.SlNhap, 0)
    ELSE 0
  END AS CanNhapThem
FROM Products p
LEFT JOIN WarehouseStocks ws ON ws.ProductId = p.Id AND ws.WarehouseId = @WarehouseId
LEFT JOIN Tx t ON t.ProductId = p.Id
WHERE ws.Id IS NOT NULL OR t.ProductId IS NOT NULL
ORDER BY CanNhapThem DESC, ABS(ISNULL(ws.Quantity, 0) - (ISNULL(t.SlNhap, 0) - ISNULL(t.SlXuat, 0) + ISNULL(t.SlDieuChinh, 0))) DESC;

-- SKU xuất > nhập (cần nhập thêm)
SELECT ProductId,
       SUM(CASE WHEN [Type] IN (1, 2) THEN QuantityChange ELSE 0 END) AS SlNhap,
       SUM(CASE WHEN [Type] IN (3, 4) THEN -QuantityChange ELSE 0 END) AS SlXuat
FROM StockTransactions
WHERE WarehouseId = @WarehouseId
GROUP BY ProductId
HAVING SUM(CASE WHEN [Type] IN (3, 4) THEN -QuantityChange ELSE 0 END)
     > SUM(CASE WHEN [Type] IN (1, 2) THEN QuantityChange ELSE 0 END);
```

### 10.7. Kiểm tra mục tiêu seed (~500 / bảng)

`DataSeeder.Target = 500`. Sau khi API start lần đầu (hoặc top-up), các bảng nghiệp vụ chính nên gần ngưỡng này.

```sql
SELECT Bang, SoLuong, 500 AS MucTieu, CASE WHEN SoLuong >= 500 THEN N'OK' ELSE N'Thiếu' END AS TrangThai
FROM (
  SELECT 'Categories' AS Bang, COUNT(*) AS SoLuong FROM Categories
  UNION ALL SELECT 'Units', COUNT(*) FROM Units
  UNION ALL SELECT 'Warehouses', COUNT(*) FROM Warehouses
  UNION ALL SELECT 'Suppliers', COUNT(*) FROM Suppliers
  UNION ALL SELECT 'Users', COUNT(*) FROM Users
  UNION ALL SELECT 'Customers', COUNT(*) FROM Customers
  UNION ALL SELECT 'Products', COUNT(*) FROM Products
  UNION ALL SELECT 'WarehouseStocks', COUNT(*) FROM WarehouseStocks
  UNION ALL SELECT 'ImportReceipts (PN-SEED)', COUNT(*) FROM ImportReceipts WHERE Code LIKE 'PN-SEED-%'
  UNION ALL SELECT 'ExportReceipts (PX-SEED)', COUNT(*) FROM ExportReceipts WHERE Code LIKE 'PX-SEED-%'
  UNION ALL SELECT 'Stocktakes (KK-SEED)', COUNT(*) FROM Stocktakes WHERE Code LIKE 'KK-SEED-%'
) x
ORDER BY Bang;
```

### 10.8. Hóa đơn nhập (HDN)

```sql
SELECT COUNT(*) AS TongHoaDon FROM Invoices;
SELECT COUNT(*) AS TongDongHD FROM InvoiceDetails;
SELECT COUNT(*) AS PhieuCoMaHDN
FROM ImportReceipts
WHERE PurchaseInvoiceCode IS NOT NULL AND LTRIM(RTRIM(PurchaseInvoiceCode)) <> '';

SELECT TOP 20 Id, Code, PurchaseInvoiceCode, Status, ShippingFee, TradeDiscount, NonRefundableTax
FROM ImportReceipts
WHERE PurchaseInvoiceCode LIKE 'HDN-%'
ORDER BY Id DESC;
```

### 10.9. Chạy nhanh bằng `sqlcmd` (macOS / Linux)

```bash
# Đổi Server / Database / Password cho khớp appsettings.json
sqlcmd -S localhost,1434 -U sa -P '123456Aa@' -d WareDbnew1 -Q "
SELECT
  (SELECT COUNT(*) FROM Products) AS SanPham,
  (SELECT COUNT(*) FROM Warehouses) AS KhoHang,
  (SELECT COUNT(*) FROM ImportReceipts WHERE Status = 0) AS NhapDraft,
  (SELECT COUNT(*) FROM ExportReceipts WHERE Status = 0) AS XuatDraft,
  (SELECT COUNT(*) FROM Stocktakes WHERE Status = 0) AS KkDraft,
  (SELECT COUNT(*) FROM StockTransactions) AS GiaoDich;
"

# Chạy cả file truy vấn đầy đủ
sqlcmd -S localhost,1434 -U sa -P '123456Aa@' -d WareDbnew1 \
  -i scripts/WarehouseFullQueries.sql
```

---

## 11. Test hiệu năng

Thay cho script bash/k6: dùng **xUnit performance tests** trong  
`backend/tests/WarehouseManagement.PerformanceTests`  
(chi tiết: `backend/tests/README.md`).

### 11.1. Chạy

```bash
# Bắt buộc: đứng ở thư mục backend/ (không phải WebAPI)
cd backend
dotnet test tests/WarehouseManagement.PerformanceTests
```

Nếu đang ở `src/WarehouseManagement.WebAPI`:

```bash
dotnet test ../../tests/WarehouseManagement.PerformanceTests/WarehouseManagement.PerformanceTests.csproj
```

### 11.2. Nội dung đo

`LoginPerformanceTests` — đăng nhập theo nấc **50 → 100 → 250 → 500 → 750 → 1000**.

```bash
# API đang chạy cổng 5136
cd backend
dotnet test tests/WarehouseManagement.PerformanceTests
```

Sau khi chạy xong, tự xuất Excel:

`tests/WarehouseManagement.PerformanceTests/TestResults/PerformanceTests_Latest.xlsx`

Chi tiết: `backend/tests/README.md`.

### 11.3. SQL — đo thời gian truy vấn (tuỳ chọn, SSMS)

```sql
SET STATISTICS TIME ON;
SELECT TOP (20) Id, Sku, Name FROM Products ORDER BY Id;
SELECT TOP (20) Id, Sku, Name FROM Products
WHERE Name LIKE N'%Paracetamol%' OR Sku LIKE N'%Paracetamol%';
SET STATISTICS TIME OFF;
```

---

## 12. Cấu trúc & lệnh

```text
WarehouseManagement.Domain          Entity, enum
WarehouseManagement.Application     DTO, Interface, Service
WarehouseManagement.Infrastructure  EF, Repository, JWT, File, Seeder
WarehouseManagement.WebAPI          Controllers, Swagger, Program.cs
WarehouseManagement.Shared          ApiResponse, Exception, AppRoles, Pagination
tests/WarehouseManagement.PerformanceTests   đo hiệu năng (catalog / paging / phiếu)
```

- Admin API: `AdminApiController` → `[Authorize(Roles = Admin,Manager,Staff)]`.
- Tồn kho: `StockMovementService`.

```bash
dotnet build backend/src/WarehouseManagement.WebAPI

cd backend/src/WarehouseManagement.WebAPI
dotnet ef migrations add TenMigration \
  --project ../WarehouseManagement.Infrastructure/WarehouseManagement.Infrastructure.csproj \
  --output-dir Persistence/Migrations
dotnet ef database update \
  --project ../WarehouseManagement.Infrastructure/WarehouseManagement.Infrastructure.csproj

dotnet run --launch-profile http
```

---

**Tóm tắt:**  
`dotnet run --launch-profile http` → mở `/swagger` → **admin/login** → **Authorize (dán token)** → xem Curl có `Authorization` → mới GET/POST/PUT/DELETE.  
**Số liệu ↔ SQL:** tài khoản §2, checklist §6, enum §8, Dashboard/seed/đối chiếu §10, perf 500 SP §11.  
Test hiệu năng: từ `backend/`: `dotnet test tests/WarehouseManagement.PerformanceTests`.
