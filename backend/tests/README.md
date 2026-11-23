# Tests — hiệu năng đăng nhập (kịch bản)

Project: `WarehouseManagement.PerformanceTests`  
File chính: `LoginPerformanceTests.cs`

## Kịch bản

| # | Test | Tải | Kỳ vọng |
|---|------|-----|---------|
| 1 | `Scenario_01_Baseline_Login_n50` | 50 login | **PASS** |
| 2 | `Scenario_02_ChiuTai_Ramp_100_to_500` | 100 → 250 → 500 | **PASS** |
| 3 | `Scenario_03_CaoDiem_750_to_1000` | 750 → 1000 | **PASS** |
| 4 | `Scenario_04_QuaTai_FullBurst_n2000` | 2000 full, không retry | **FAIL** nếu quá tải |
| * | `Scenario_Full_1_to_4` | Chạy tuần tự 1→4 | Bước 4 có thể **FAIL** |

### Quá tải (KB4) — điều kiện FAIL

Ghi `Result=FAIL` trên Excel khi:

- `rate < 90%`, hoặc
- `fail >= 100`, hoặc
- rate giảm > 10% so với baseline n=50

Message dạng: `QUÁ TẢI ĐĂNG NHẬP: ...`

## Chạy

```bash
# Terminal 1 — API
cd backend/src/WarehouseManagement.WebAPI && dotnet run --launch-profile http

# Terminal 2 — test
cd backend
dotnet test tests/WarehouseManagement.PerformanceTests
```

Chỉ kịch bản quá tải:

```bash
dotnet test tests/WarehouseManagement.PerformanceTests --filter "FullyQualifiedName~Scenario_04"
```

## Excel

```text
TestResults/PerformanceTests_Latest.xlsx
```

Cột: STT, Class, TestCase, Result (PASS/FAIL), DurationMs, Metrics, Message, FinishedAtUtc.
