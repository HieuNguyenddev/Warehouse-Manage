# Warehouse Frontend (React + TypeScript)

UI: **Material UI** + **Tailwind CSS** (layout/spacing).

Hai cổng:

- **Admin** `/admin/login` — quản trị kho (CRUD + nhập/xuất/kiểm kê), dashboard bám API
- **Customer** `/store/login` — xem sản phẩm + hồ sơ

## Chạy

```bash
# Terminal 1 — API (CORS đã bật cho :5173)
cd backend/src/WarehouseManagement.WebAPI
dotnet run --urls "http://localhost:5080"

# Terminal 2 — Frontend
cd frontend
npm install
npm run dev
```

Mở http://localhost:5173

## Tài khoản mẫu

| Cổng | Username | Password |
|------|----------|----------|
| Admin | `admin` / `manager` / `staff` | `123456` |
| Store | `customer1` | `123456` |

Vite proxy `/api` và `/uploads` → `http://localhost:5080`.
