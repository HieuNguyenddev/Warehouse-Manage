-- Dashboard SQL (AdminDashboard.tsx)
-- Status: 0=Draft, 1=Confirmed, 2=Cancelled
-- StockTransaction Type: 1=Import 2=ImportCancel 3=Export 4=ExportCancel 5=Stocktake 6=StocktakeCancel 7=Manual
-- USE WareDbnew1;

-- Hero: Nhap da XN
SELECT COUNT(*) AS Nhap_da_XN
FROM ImportReceipts
WHERE Status = 1;

-- Hero: Xuat da XN
SELECT COUNT(*) AS Xuat_da_XN
FROM ExportReceipts
WHERE Status = 1;

-- Hero + KPI: Ton thap (EffectiveMin = ISNULL(MinQuantity, Product.MinStock))
SELECT COUNT(*) AS Ton_thap
FROM WarehouseStocks ws
INNER JOIN Products p ON p.Id = ws.ProductId
WHERE ISNULL(ws.MinQuantity, p.MinStock) > 0
  AND ws.Quantity <= ISNULL(ws.MinQuantity, p.MinStock);

-- Hero: KK da XN
SELECT COUNT(*) AS KK_da_XN
FROM Stocktakes
WHERE Status = 1;

-- KPI: Thuoc / vat tu
SELECT COUNT(*) AS Thuoc_vat_tu
FROM Products;

-- KPI: Kho duoc
SELECT COUNT(*) AS Kho_duoc
FROM Warehouses;

-- KPI: Phieu nhap nhap
SELECT COUNT(*) AS Phieu_nhap_nhap
FROM ImportReceipts
WHERE Status = 0;

-- KPI: Phieu xuat nhap
SELECT COUNT(*) AS Phieu_xuat_nhap
FROM ExportReceipts
WHERE Status = 0;

-- KPI: Giao dich kho
SELECT COUNT(*) AS Giao_dich_kho
FROM StockTransactions;

-- Chip: Nhom thuoc
SELECT COUNT(*) AS Nhom_thuoc
FROM Categories;

-- Chip: NCC duoc
SELECT COUNT(*) AS NCC_duoc
FROM Suppliers;

-- Chip: Khoa / phong
SELECT COUNT(*) AS Khoa_phong
FROM Customers;

-- Pie: Thuoc/VT
SELECT COUNT(*) AS Pie_Thuoc_VT
FROM Products;

-- Pie: Kho
SELECT COUNT(*) AS Pie_Kho
FROM Warehouses;

-- Pie: Khoa/phong
SELECT COUNT(*) AS Pie_Khoa_phong
FROM Customers;

-- Pie: NCC
SELECT COUNT(*) AS Pie_NCC
FROM Suppliers;

-- Pie: Nhom thuoc
SELECT COUNT(*) AS Pie_Nhom_thuoc
FROM Categories;

-- Bar: Nhap nhap
SELECT COUNT(*) AS Bar_Nhap_nhap
FROM ImportReceipts
WHERE Status = 0;

-- Bar: Nhap XN
SELECT COUNT(*) AS Bar_Nhap_XN
FROM ImportReceipts
WHERE Status = 1;

-- Bar: Nhap huy
SELECT COUNT(*) AS Bar_Nhap_huy
FROM ImportReceipts
WHERE Status = 2;

-- Bar: Xuat nhap
SELECT COUNT(*) AS Bar_Xuat_nhap
FROM ExportReceipts
WHERE Status = 0;

-- Bar: Xuat XN
SELECT COUNT(*) AS Bar_Xuat_XN
FROM ExportReceipts
WHERE Status = 1;

-- Bar: Xuat huy
SELECT COUNT(*) AS Bar_Xuat_huy
FROM ExportReceipts
WHERE Status = 2;

-- Bar: KK nhap
SELECT COUNT(*) AS Bar_KK_nhap
FROM Stocktakes
WHERE Status = 0;

-- Bar: KK XN
SELECT COUNT(*) AS Bar_KK_XN
FROM Stocktakes
WHERE Status = 1;

-- Canh bao ton thap TOP 8 (bieu do + list uu tien)
SELECT TOP (8)
  ws.Id,
  p.Sku,
  p.Name AS ProductName,
  w.Code AS WarehouseCode,
  ws.Quantity AS TonHienTai,
  ISNULL(ws.MinQuantity, p.MinStock) AS MinStock
FROM WarehouseStocks ws
INNER JOIN Products p ON p.Id = ws.ProductId
INNER JOIN Warehouses w ON w.Id = ws.WarehouseId
WHERE ISNULL(ws.MinQuantity, p.MinStock) > 0
  AND ws.Quantity <= ISNULL(ws.MinQuantity, p.MinStock)
ORDER BY ws.Quantity ASC, ws.Id ASC;

-- Tong so luong ton toan he thong
SELECT SUM(Quantity) AS Tong_so_luong_ton
FROM WarehouseStocks;

-- So dong ton
SELECT COUNT(*) AS So_dong_ton
FROM WarehouseStocks;

-- So SKU het hang
SELECT COUNT(*) AS Het_hang
FROM WarehouseStocks
WHERE Quantity <= 0;

-- Loai GD trong 50 gan nhat: Import
SELECT COUNT(*) AS Loai_Import_trong_50
FROM (
  SELECT TOP (50) [Type]
  FROM StockTransactions
  ORDER BY Id DESC
) x
WHERE [Type] = 1;

-- Loai GD trong 50 gan nhat: ImportCancel
SELECT COUNT(*) AS Loai_ImportCancel_trong_50
FROM (
  SELECT TOP (50) [Type]
  FROM StockTransactions
  ORDER BY Id DESC
) x
WHERE [Type] = 2;

-- Loai GD trong 50 gan nhat: Export
SELECT COUNT(*) AS Loai_Export_trong_50
FROM (
  SELECT TOP (50) [Type]
  FROM StockTransactions
  ORDER BY Id DESC
) x
WHERE [Type] = 3;

-- Loai GD trong 50 gan nhat: ExportCancel
SELECT COUNT(*) AS Loai_ExportCancel_trong_50
FROM (
  SELECT TOP (50) [Type]
  FROM StockTransactions
  ORDER BY Id DESC
) x
WHERE [Type] = 4;

-- Loai GD trong 50 gan nhat: Stocktake
SELECT COUNT(*) AS Loai_Stocktake_trong_50
FROM (
  SELECT TOP (50) [Type]
  FROM StockTransactions
  ORDER BY Id DESC
) x
WHERE [Type] = 5;

-- Loai GD trong 50 gan nhat: StocktakeCancel
SELECT COUNT(*) AS Loai_StocktakeCancel_trong_50
FROM (
  SELECT TOP (50) [Type]
  FROM StockTransactions
  ORDER BY Id DESC
) x
WHERE [Type] = 6;

-- Loai GD trong 50 gan nhat: Manual
SELECT COUNT(*) AS Loai_Manual_trong_50
FROM (
  SELECT TOP (50) [Type]
  FROM StockTransactions
  ORDER BY Id DESC
) x
WHERE [Type] = 7;

-- Xu huong bien dong ton (50 GD gan nhat, gom theo ngay)
SELECT
  CONVERT(date, CreatedAt) AS Ngay,
  SUM(CASE WHEN QuantityChange >= 0 THEN QuantityChange ELSE 0 END) AS Tang,
  SUM(CASE WHEN QuantityChange < 0 THEN ABS(QuantityChange) ELSE 0 END) AS Giam,
  SUM(QuantityChange) AS Net,
  COUNT(*) AS SoGiaoDich
FROM (
  SELECT TOP (50) QuantityChange, CreatedAt
  FROM StockTransactions
  ORDER BY Id DESC
) x
GROUP BY CONVERT(date, CreatedAt)
ORDER BY Ngay;

-- Chi tiet 50 giao dich gan nhat
SELECT TOP (50)
  t.Id,
  CASE t.[Type]
    WHEN 1 THEN N'Import'
    WHEN 2 THEN N'ImportCancel'
    WHEN 3 THEN N'Export'
    WHEN 4 THEN N'ExportCancel'
    WHEN 5 THEN N'Stocktake'
    WHEN 6 THEN N'StocktakeCancel'
    WHEN 7 THEN N'Manual'
    ELSE CONCAT(N'Other_', t.[Type])
  END AS Loai,
  t.QuantityChange,
  t.QuantityBefore,
  t.QuantityAfter,
  t.ReferenceCode,
  t.CreatedAt,
  p.Sku,
  w.Code AS WarehouseCode
FROM StockTransactions t
INNER JOIN Products p ON p.Id = t.ProductId
INNER JOIN Warehouses w ON w.Id = t.WarehouseId
ORDER BY t.Id DESC;
