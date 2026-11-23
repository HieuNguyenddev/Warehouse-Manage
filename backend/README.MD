# Warehouse Management API — Chạy & test Swagger

Backend .NET (Clean Architecture) + JWT.  
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

## 2. Tài khoản seed

| Username | Password | Dùng login | Gọi được API quản trị? |
|----------|----------|------------|-------------------------|
| `admin` | `123456` | `POST /api/v1/auth/admin/login` | Có |
| `manager` | `123456` | admin/login | Có |
| `staff` | `123456` | admin/login | Có |
| `customer1` | `123456` | `POST /api/v1/auth/customer/login` | **Không** (403/401 với Products admin) |

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

### B. Sản phẩm + ảnh

| # | API | Ghi chú | Kỳ vọng |
|---|-----|---------|---------|
| 9 | `POST /api/v1/products` | `sku`, `name`, `price`, `minStock`, `categoryId`, `unitId` | 201 |
| 10 | `POST /api/v1/products/{id}/images` | form `files` | Ảnh; primary |
| 11 | `PUT .../images/{imageId}/primary` | | Đổi ảnh chính |
| 12 | `DELETE .../images/{imageId}` | | Xóa ảnh |

### C. Tồn thủ công

| # | API | Kỳ vọng |
|---|-----|---------|
| 13 | `POST /api/v1/warehouse-stocks` | Manual TX |
| 14 | `PUT /api/v1/warehouse-stocks/{id}` | Cập nhật tồn |
| 15 | `GET /api/v1/warehouse-stocks?lowStockOnly=true` | `isLowStock` |

### D. Nhập kho

Draft → Confirm (cộng tồn) → Cancel (hoàn).

| # | API | Kỳ vọng |
|---|-----|---------|
| 16 | `POST /api/v1/import-receipts` | Draft, mã `PN-...` |
| 17 | `PUT /api/v1/import-receipts/{id}` | Chỉ Draft |
| 18 | `POST .../{id}/confirm` | Confirmed + tồn tăng |
| 19 | `POST .../{id}/cancel` | Hoàn tồn |
| 20 | `DELETE .../{id}` | Chỉ Draft → 204 |

### E. Xuất kho

| # | API | Kỳ vọng |
|---|-----|---------|
| 21 | `POST /api/v1/export-receipts` | Draft `PX-...` |
| 22 | `POST .../{id}/confirm` | Đủ tồn → trừ |
| 23 | Confirm quá tồn | **409** |
| 24 | `POST .../{id}/cancel` | Hoàn tồn |

### F. Kiểm kê

| # | API | Kỳ vọng |
|---|-----|---------|
| 25 | `POST /api/v1/stocktakes` | Snapshot system/counted/diff |
| 26–28 | CRUD details / `onlyDiff=true` | Lệch tồn |
| 29 | `POST .../{id}/confirm` | Gán tồn = counted |
| 30 | `POST .../{id}/cancel` | Hoàn system |

### G. Lịch sử tồn

| # | API | Kỳ vọng |
|---|-----|---------|
| 31–34 | `GET /api/v1/stock-transactions` | Import/Export/Stocktake/Manual |

### H. Cổng Customer (smoke)

| # | API | Kỳ vọng |
|---|-----|---------|
| 35 | `customer/login` với `customer1` | Token customer |
| 36 | Token Customer → `GET /api/v1/products` | 401/403 |
| 37 | `admin` vào `customer/login` | 401 sai cổng |

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

| Status | Sửa/Xóa | Tồn |
|--------|---------|-----|
| `Draft` | Có | Chưa đổi |
| `Confirmed` | Không | Đã cộng/trừ/gán |
| `Cancelled` | Không | Đã hoàn (nếu từng Confirm) |

| StockTransactionType | Khi nào |
|----------------------|---------|
| `Import` / `ImportCancel` | Confirm / hủy phiếu nhập |
| `Export` / `ExportCancel` | Confirm / hủy phiếu xuất |
| `Stocktake` / `StocktakeCancel` | Confirm / hủy kiểm kê |
| `Manual` | CRUD `warehouse-stocks` |

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

## 10. SQL — đếm số lượng (Dashboard & toàn bộ bảng)

> Chạy trên SQL Server (SSMS / Azure Data Studio / `sqlcmd`).  
> Database mặc định theo `appsettings.json` (ví dụ `WareDbnew1`).  
> Enum trạng thái phiếu: `0 = Draft`, `1 = Confirmed`, `2 = Cancelled`.

### 10.1. KPI trên Dashboard (thẻ số)

```sql
-- Sản phẩm
SELECT COUNT(*) AS SanPham FROM Products;

-- Kho hàng
SELECT COUNT(*) AS KhoHang FROM Warehouses;

-- Tồn thấp (cùng logic API: EffectiveMin = ISNULL(MinQuantity, Product.MinStock);
-- cảnh báo khi EffectiveMin > 0 AND Quantity <= EffectiveMin)
SELECT COUNT(*) AS TonThap
FROM WarehouseStocks ws
INNER JOIN Products p ON p.Id = ws.ProductId
WHERE ISNULL(ws.MinQuantity, p.MinStock) > 0
  AND ws.Quantity <= ISNULL(ws.MinQuantity, p.MinStock);

-- Nhập Draft / Xuất Draft
SELECT COUNT(*) AS NhapDraft FROM ImportReceipts WHERE Status = 0;
SELECT COUNT(*) AS XuatDraft FROM ExportReceipts WHERE Status = 0;
```

Một câu gom KPI:

```sql
SELECT
  (SELECT COUNT(*) FROM Products) AS SanPham,
  (SELECT COUNT(*) FROM Warehouses) AS KhoHang,
  (SELECT COUNT(*)
   FROM WarehouseStocks ws
   INNER JOIN Products p ON p.Id = ws.ProductId
   WHERE ISNULL(ws.MinQuantity, p.MinStock) > 0
     AND ws.Quantity <= ISNULL(ws.MinQuantity, p.MinStock)) AS TonThap,
  (SELECT COUNT(*) FROM ImportReceipts WHERE Status = 0) AS NhapDraft,
  (SELECT COUNT(*) FROM ExportReceipts WHERE Status = 0) AS XuatDraft;
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

-- Tổng giao dịch tồn
SELECT COUNT(*) AS TongGiaoDich FROM StockTransactions;

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

-- Phiếu seed demo
SELECT 'PN-SEED' AS Prefix, COUNT(*) AS SoLuong FROM ImportReceipts WHERE Code LIKE 'PN-SEED-%'
UNION ALL SELECT 'PX-SEED', COUNT(*) FROM ExportReceipts WHERE Code LIKE 'PX-SEED-%'
UNION ALL SELECT 'KK-SEED', COUNT(*) FROM Stocktakes WHERE Code LIKE 'KK-SEED-%';
```

### 10.6. Chạy nhanh bằng `sqlcmd` (macOS / Linux)

```bash
# Đổi Server / Database / Password cho khớp appsettings.json
sqlcmd -S localhost,1434 -U sa -P '123456Aa@' -d WareDbnew1 -Q "
SELECT
  (SELECT COUNT(*) FROM Products) AS SanPham,
  (SELECT COUNT(*) FROM Warehouses) AS KhoHang,
  (SELECT COUNT(*) FROM ImportReceipts WHERE Status = 0) AS NhapDraft,
  (SELECT COUNT(*) FROM ExportReceipts WHERE Status = 0) AS XuatDraft;
"
```

---

## 11. Test hiệu năng (API + SQL)

> Điều kiện: API đang chạy (`dotnet run --launch-profile http`), DB đã seed ~500 bản ghi/bảng.  
> Script: `backend/scripts/perf-test.sh` (curl + Python, không cần cài thêm tool).

### 11.1. Chạy nhanh

```bash
chmod +x backend/scripts/perf-test.sh
./backend/scripts/perf-test.sh
```

Tuỳ chọn:

```bash
BASE_URL=http://localhost:5136 \
ROUNDS=20 \
CONCURRENCY=10 \
CONCURRENT_TOTAL=50 \
AVG_WARN_MS=400 \
P95_WARN_MS=800 \
FAIL_ON_THRESHOLD=0 \
./backend/scripts/perf-test.sh
```

| Biến | Mặc định | Ý nghĩa |
|------|----------|---------|
| `ROUNDS` | 15 | Số lần đo tuần tự mỗi endpoint |
| `CONCURRENCY` | 8 | Số request song song |
| `CONCURRENT_TOTAL` | 40 | Tổng request trong burst |
| `AVG_WARN_MS` / `P95_WARN_MS` | 400 / 800 | Ngưỡng cảnh báo (ms) |
| `FAIL_ON_THRESHOLD` | 0 | `1` = exit 1 nếu có endpoint SLOW |

Script đo 3 nhóm:
1. **Latency tuần tự** — login, products (trang 1/50, search, pageSize=100), categories/units/warehouses/suppliers/customers/users, stocks (+ lowStock), phiếu Draft, transactions, health → in `min / p50 / avg / p95 / max`
2. **Concurrent burst** — 40× GET products song song
3. **Dashboard parallel** — ~9 API như UI `Promise.all`, đo wall-time

### 11.2. Test độ chịu tải (load / stress / spike / soak)

Script: `backend/scripts/perf-load.sh` — nhiều kịch bản đo khả năng chịu tải với ~500 bản ghi/bảng.

```bash
chmod +x backend/scripts/perf-load.sh

# Chạy TẤT CẢ scenario
./backend/scripts/perf-load.sh

# Chỉ một hoặc vài scenario
./backend/scripts/perf-load.sh ramp
./backend/scripts/perf-load.sh spike soak
./backend/scripts/perf-load.sh mixed authstorm write deep
```

| Scenario | Mục đích | Mặc định |
|----------|----------|----------|
| **ramp** | Tăng dần concurrency `2→5→10→20→40` | 40 req/bậc |
| **spike** | Đột ngột ~50 VU | 100 req |
| **soak** | Giữ tải ổn định lâu | 10 VU × 30s (mixed GET) |
| **mixed** | Xen kẽ nhiều endpoint (giống user thật) | 12 VU × 60 req |
| **authstorm** | Nhiều `POST login` song song | 15 × 45 |
| **write** | POST category + DELETE (ghi DB) | 5 × 20 |
| **deep** | Phân trang sâu page 1..50 dưới tải | 10 × 40 |

Mỗi scenario in: `ok/err/%`, `p50/avg/p95/p99/max`.  
**FAIL** nếu `error > MAX_ERROR_PCT` (mặc định 5%) hoặc `p95 > P95_WARN_MS` (mặc định 1500ms).

Tuỳ chỉnh tải nặng hơn:

```bash
# Ramp cao hơn
RAMP_LEVELS=10,20,40,60,80 RAMP_REQUESTS=60 ./backend/scripts/perf-load.sh ramp

# Spike mạnh
SPIKE_CONCURRENCY=100 SPIKE_REQUESTS=200 ./backend/scripts/perf-load.sh spike

# Soak 2 phút
SOAK_CONCURRENCY=20 SOAK_SECONDS=120 ./backend/scripts/perf-load.sh soak

# Mixed + write
MIXED_CONCURRENCY=20 WRITE_CONCURRENCY=8 ./backend/scripts/perf-load.sh mixed write

# Siết ngưỡng
MAX_ERROR_PCT=1 P95_WARN_MS=800 ./backend/scripts/perf-load.sh
```

### 11.3. Checklist endpoint nên đo (thủ công / Swagger)

| # | API | Mục đích |
|---|-----|----------|
| 1 | `POST /api/v1/auth/admin/login` | Auth + hash password |
| 2 | `GET /api/v1/products?pageNumber=1&pageSize=10` | List + ảnh |
| 3 | `GET /api/v1/products?pageNumber=50&pageSize=10` | Trang sâu (~500 SP) |
| 4 | `GET /api/v1/products?search=Gao&pageSize=20` | Search Name/SKU |
| 5 | `GET /api/v1/products?pageSize=100` | Payload lớn |
| 6 | `GET /api/v1/warehouse-stocks?lowStockOnly=true` | Filter join Product |
| 7 | `GET /api/v1/import-receipts?status=Draft` | Filter enum |
| 8 | `GET /api/v1/stock-transactions?pageSize=50` | Bảng giao dịch lớn |
| 9 | Dashboard: gọi song song các API §10 KPI | Wall-time UI |

### 11.4. Đo một endpoint bằng `curl`

```bash
TOKEN=$(curl -s -X POST http://localhost:5136/api/v1/auth/admin/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"123456"}' \
  | python3 -c "import sys,json; print(json.load(sys.stdin)['data']['token'])")

# time_total (giây) — lặp 10 lần
for i in $(seq 1 10); do
  curl -sf -o /dev/null -w "products_p1 %{time_total}s\n" \
    -H "Authorization: Bearer $TOKEN" \
    'http://localhost:5136/api/v1/products?pageNumber=1&pageSize=20'
done
```

### 11.5. SQL — đo thời gian truy vấn (SSMS / sqlcmd)

```sql
SET STATISTICS TIME ON;
SET STATISTICS IO ON;

-- List products trang 1 (giống API phân trang)
SELECT TOP (20) Id, Sku, Name, Price, CategoryId, UnitId
FROM Products
ORDER BY Id;

-- Search
SELECT TOP (20) Id, Sku, Name
FROM Products
WHERE Name LIKE N'%Gao%' OR Sku LIKE N'%Gao%'
ORDER BY Id;

-- Tồn thấp (cùng logic Dashboard)
SELECT COUNT(*) AS TonThap
FROM WarehouseStocks ws
INNER JOIN Products p ON p.Id = ws.ProductId
WHERE ISNULL(ws.MinQuantity, p.MinStock) > 0
  AND ws.Quantity <= ISNULL(ws.MinQuantity, p.MinStock);

-- Phiếu nhập Draft
SELECT COUNT(*) FROM ImportReceipts WHERE Status = 0;

-- Stock transactions gần đây
SELECT TOP (50) *
FROM StockTransactions
ORDER BY CreatedAt DESC;

SET STATISTICS TIME OFF;
SET STATISTICS IO OFF;
```

Xem trong Messages: **CPU time / elapsed time**.  
Nếu `logical reads` cao bất thường → kiểm tra index (Sku, Code, Status, WarehouseId+ProductId…).

### 11.6. Gợi ý đọc kết quả chịu tải

| Hiện tượng | Ý nghĩa / hướng xử lý |
|------------|------------------------|
| `ramp` FAIL ở bậc cao (40+) | Sát giới hạn: pool SQL / thread Kestrel |
| `spike` err% cao, p95 tăng vọt | Chưa sẵn sàng đột biến — cần queue / rate limit / cache |
| `soak` ổn đầu rồi FAIL dần | Memory leak / connection leak / tempdb |
| `authstorm` chậm | Hash password tốn CPU — bình thường; đừng spam login UI |
| `write` FAIL | Lock DB / unique constraint / validation |
| `deep` chậm hơn `get` trang 1 | OFFSET lớn — cân nhắc keyset pagination |
| `GET products pageSize=100` chậm | Giảm pageSize; tránh Include ảnh thừa |
| Concurrent nhiều 5xx / timeout | Connection string `Max Pool Size`; tài nguyên máy |

### 11.7. (Tuỳ chọn) k6 — nhiều scenario

Cài [k6](https://k6.io) rồi:

```bash
# Smoke / Load / Stress / Spike / Soak / Mixed
k6 run -e SCENARIO=smoke  -e BASE_URL=http://localhost:5136 backend/scripts/perf-k6.js
k6 run -e SCENARIO=load   -e BASE_URL=http://localhost:5136 backend/scripts/perf-k6.js
k6 run -e SCENARIO=stress -e BASE_URL=http://localhost:5136 backend/scripts/perf-k6.js
k6 run -e SCENARIO=spike  -e BASE_URL=http://localhost:5136 backend/scripts/perf-k6.js
k6 run -e SCENARIO=soak   -e BASE_URL=http://localhost:5136 -e SOAK_DURATION=3m backend/scripts/perf-k6.js
k6 run -e SCENARIO=mixed  -e BASE_URL=http://localhost:5136 backend/scripts/perf-k6.js
```

Thresholds mặc định: `http_req_failed < 5%`, `p95 < 1.5s`, `p99 < 3s`.

---

## 12. Cấu trúc & lệnh

```text
WarehouseManagement.Domain          Entity, enum
WarehouseManagement.Application     DTO, Interface, Service
WarehouseManagement.Infrastructure  EF, Repository, JWT, File, Seeder
WarehouseManagement.WebAPI          Controllers, Swagger, Program.cs
WarehouseManagement.Shared          ApiResponse, Exception, AppRoles, Pagination
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
Đối chiếu số Dashboard: **§10 SQL**. Test hiệu năng: **§11** / `./backend/scripts/perf-test.sh`.
