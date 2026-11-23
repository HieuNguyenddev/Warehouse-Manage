#!/usr/bin/env bash
# =============================================================================
# perf-test.sh — Test HIỆU NĂNG (latency) API Warehouse Management
#
# Khác với perf-load.sh (đo chịu tải / nhiều user):
#   Script này đo chủ yếu LATENCY từng endpoint khi gọi tuần tự + 1 burst nhỏ
#   + mô phỏng Dashboard gọi song song (Promise.all).
#
# Dữ liệu giả định: DataSeeder ~500 bản ghi/bảng.
#
# Cách chạy:
#   chmod +x backend/scripts/perf-test.sh
#   ./backend/scripts/perf-test.sh
#
# Tuỳ chọn (env):
#   BASE_URL=http://localhost:5136 ROUNDS=20 CONCURRENCY=10 ./backend/scripts/perf-test.sh
#   FAIL_ON_THRESHOLD=1 ./backend/scripts/perf-test.sh   # có endpoint SLOW → exit 1
# =============================================================================

# -e: dừng khi lệnh lỗi | -u: biến chưa gán → lỗi | pipefail: pipe lỗi nếu 1 lệnh lỗi
set -euo pipefail

# -----------------------------------------------------------------------------
# Kết nối & tài khoản
# ${VAR:-default} = dùng $VAR nếu đã set, không thì default
# -----------------------------------------------------------------------------
BASE_URL="${BASE_URL:-http://localhost:5136}"  # URL API (launch profile http = cổng 5136)
USER_NAME="${USER_NAME:-admin}"                # User seed admin
PASSWORD="${PASSWORD:-123456}"                 # Password seed

# -----------------------------------------------------------------------------
# Tham số đo latency
# ROUNDS          : mỗi endpoint gọi bao nhiêu lần (sau warmup) để tính min/p50/avg/p95/max
#                   càng lớn càng ổn định thống kê, nhưng chạy lâu hơn
# CONCURRENCY     : số request song song tối đa trong phần B (burst)
# CONCURRENT_TOTAL: tổng số request trong phần B (chia thành các lô kích thước CONCURRENCY)
# WARMUP          : số lần gọi “làm nóng” trước khi đo (JIT / connection pool / cache)
#                   kết quả warmup BỎ QUA, không tính vào thống kê
# -----------------------------------------------------------------------------
ROUNDS="${ROUNDS:-15}"
CONCURRENCY="${CONCURRENCY:-8}"
CONCURRENT_TOTAL="${CONCURRENT_TOTAL:-40}"
WARMUP="${WARMUP:-2}"

# -----------------------------------------------------------------------------
# Ngưỡng cảnh báo (milliseconds)
# P95_WARN_MS      : nếu p95 > giá trị này → đánh dấu SLOW
# AVG_WARN_MS      : nếu avg > giá trị này → đánh dấu SLOW
# FAIL_ON_THRESHOLD: "1" = nếu có SLOW thì script exit 1 (phù hợp CI);
#                    "0" = chỉ cảnh báo, vẫn Done (mặc định cho demo/local)
# -----------------------------------------------------------------------------
P95_WARN_MS="${P95_WARN_MS:-800}"
AVG_WARN_MS="${AVG_WARN_MS:-400}"
FAIL_ON_THRESHOLD="${FAIL_ON_THRESHOLD:-0}"

# Màu ANSI terminal
RED=$'\033[0;31m'   # đỏ — API down / fail cứng
GRN=$'\033[0;32m'   # xanh — OK
YLW=$'\033[0;33m'   # vàng — SLOW
NC=$'\033[0m'       # reset màu

# -----------------------------------------------------------------------------
# Health check — API phải sống trước khi đo
# -----------------------------------------------------------------------------
echo "==> Health check ${BASE_URL}"
if ! curl -sf "${BASE_URL}/api/v1/health" >/dev/null; then
  echo "${RED}API chưa chạy. Chạy: cd backend/src/WarehouseManagement.WebAPI && dotnet run --launch-profile http${NC}"
  exit 1
fi

# -----------------------------------------------------------------------------
# Login 1 lần → TOKEN dùng cho mọi GET có bảo vệ
# AUTH = header đầy đủ "Authorization: Bearer …"
# -----------------------------------------------------------------------------
echo "==> Login ${USER_NAME}"
LOGIN_JSON=$(curl -sf -X POST "${BASE_URL}/api/v1/auth/admin/login" \
  -H 'Content-Type: application/json' \
  -d "{\"username\":\"${USER_NAME}\",\"password\":\"${PASSWORD}\"}")
TOKEN=$(python3 -c "import sys,json; print(json.loads(sys.argv[1])['data']['token'])" "$LOGIN_JSON")
AUTH="Authorization: Bearer ${TOKEN}"

# =============================================================================
# HELPERS
# =============================================================================

# time_one — gọi 1 GET, trả về time_total (giây, float) qua stdout
# Tham số: $1 = URL đầy đủ
# -o /dev/null: bỏ body | -w "%{time_total}": chỉ in thời gian
time_one() {
  local url="$1"
  curl -sf -o /dev/null -w "%{time_total}" -H "$AUTH" -H 'Accept: application/json' "$url"
}

# measure_endpoint — warmup + ROUNDS lần đo → in 1 dòng:
#   name|n|min|p50|avg|p95|max|OK|SLOW
# Tham số: $1 = tên hiển thị, $2 = URL
measure_endpoint() {
  local name="$1"
  local url="$2"
  local times=()   # mảng milliseconds từng lần đo
  local i t ms

  # Warmup: gọi trước, bỏ kết quả (|| true = không dừng script nếu 1 lần fail)
  for ((i = 0; i < WARMUP; i++)); do
    time_one "$url" >/dev/null || true
  done

  # Đo thật: mỗi lần lấy giây → đổi sang ms (int) → đưa vào mảng times
  for ((i = 0; i < ROUNDS; i++)); do
    t=$(time_one "$url")
    ms=$(python3 -c "print(int(float('$t')*1000))")
    times+=("$ms")
  done

  # Python tính thống kê; nhận: name, AVG_WARN, P95_WARN, rồi từng giá trị ms
  python3 - "$name" "$AVG_WARN_MS" "$P95_WARN_MS" "${times[@]}" <<'PY'
import sys
name, avg_warn, p95_warn = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
vals = sorted(int(x) for x in sys.argv[4:])
n = len(vals)
avg = sum(vals) / n
p50 = vals[n // 2]                          # trung vị (median)
p95 = vals[max(0, int(n * 0.95) - 1)]       # 95-percentile
mn, mx = vals[0], vals[-1]                  # min / max sau khi sort
ok = avg <= avg_warn and p95 <= p95_warn
status = "OK" if ok else "SLOW"
# Format pipe-separated để print_row parse
print(f"{name}|{n}|{mn}|{p50}|{avg:.0f}|{p95}|{mx}|{status}")
PY
}

# print_row — tách dòng "a|b|c|…" và in đẹp có màu
print_row() {
  local line="$1"
  IFS='|' read -r name n mn p50 avg p95 mx status <<<"$line"
  local color="$GRN"
  [[ "$status" == "SLOW" ]] && color="$YLW"
  printf "${color}%-28s rounds=%-3s min=%4sms  p50=%4sms  avg=%4sms  p95=%4sms  max=%4sms  [%s]${NC}\n" \
    "$name" "$n" "$mn" "$p50" "$avg" "$p95" "$mx" "$status"
}

# =============================================================================
# A) Latency tuần tự — từng endpoint một, phản ánh trải nghiệm “user bình thường”
# =============================================================================
echo
echo "==> A) Latency tuần tự (ROUNDS=${ROUNDS}, WARMUP=${WARMUP})"
echo "    Ngưỡng: avg<=${AVG_WARN_MS}ms, p95<=${P95_WARN_MS}ms"
echo

RESULTS=()   # lưu các dòng kết quả (dự phòng, hiện ít dùng lại)
FAILS=0      # đếm số endpoint SLOW + phần B/C fail

# run — bọc measure_endpoint + print + tăng FAILS nếu SLOW
# Tham số: $1 tên, $2 URL
run() {
  local name="$1" url="$2"
  local row
  row=$(measure_endpoint "$name" "$url")
  RESULTS+=("$row")
  print_row "$row"
  if [[ "$row" == *"|SLOW" ]]; then
    FAILS=$((FAILS + 1))
  fi
}

# measure_login — đo POST login riêng (không gắn Bearer sẵn)
# Warmup + ROUNDS giống measure_endpoint nhưng dùng curl POST
measure_login() {
  local times=() i t ms
  for ((i = 0; i < WARMUP; i++)); do
    curl -sf -o /dev/null -X POST "${BASE_URL}/api/v1/auth/admin/login" \
      -H 'Content-Type: application/json' \
      -d "{\"username\":\"${USER_NAME}\",\"password\":\"${PASSWORD}\"}" || true
  done
  for ((i = 0; i < ROUNDS; i++)); do
    t=$(curl -sf -o /dev/null -w "%{time_total}" -X POST "${BASE_URL}/api/v1/auth/admin/login" \
      -H 'Content-Type: application/json' \
      -d "{\"username\":\"${USER_NAME}\",\"password\":\"${PASSWORD}\"}")
    ms=$(python3 -c "print(int(float('$t')*1000))")
    times+=("$ms")
  done
  python3 - "POST /auth/admin/login" "$AVG_WARN_MS" "$P95_WARN_MS" "${times[@]}" <<'PY'
import sys
name, avg_warn, p95_warn = sys.argv[1], int(sys.argv[2]), int(sys.argv[3])
vals = sorted(int(x) for x in sys.argv[4:])
n = len(vals)
avg = sum(vals) / n
p50 = vals[n // 2]
p95 = vals[max(0, int(n * 0.95) - 1)]
mn, mx = vals[0], vals[-1]
ok = avg <= avg_warn and p95 <= p95_warn
status = "OK" if ok else "SLOW"
print(f"{name}|{n}|{mn}|{p50}|{avg:.0f}|{p95}|{mx}|{status}")
PY
}
row=$(measure_login)
RESULTS+=("$row")
print_row "$row"
[[ "$row" == *"|SLOW" ]] && FAILS=$((FAILS + 1))

# --- Các endpoint list / search / filter điển hình (~500 bản ghi) ---
# p1 s10     : trang đầu, pageSize nhỏ — case UI phổ biến
# p50 s10    : trang sâu (OFFSET lớn) — thường chậm hơn p1
# search     : lọc theo chuỗi (LIKE / fulltext tùy implement)
# pageSize100: payload lớn hơn — kiểm tra serialize + Include
run "GET products p1 s10"      "${BASE_URL}/api/v1/products?pageNumber=1&pageSize=10"
run "GET products p50 s10"     "${BASE_URL}/api/v1/products?pageNumber=50&pageSize=10"
run "GET products search"      "${BASE_URL}/api/v1/products?search=Gao&pageNumber=1&pageSize=20"
run "GET products pageSize100" "${BASE_URL}/api/v1/products?pageNumber=1&pageSize=100"
run "GET categories"           "${BASE_URL}/api/v1/categories?pageNumber=1&pageSize=20"
run "GET units"                "${BASE_URL}/api/v1/units?pageNumber=1&pageSize=20"
run "GET warehouses"           "${BASE_URL}/api/v1/warehouses?pageNumber=1&pageSize=20"
run "GET suppliers"            "${BASE_URL}/api/v1/suppliers?pageNumber=1&pageSize=20"
run "GET customers"            "${BASE_URL}/api/v1/customers?pageNumber=1&pageSize=20"
run "GET users"                "${BASE_URL}/api/v1/users?pageNumber=1&pageSize=20"
run "GET stocks"               "${BASE_URL}/api/v1/warehouse-stocks?pageNumber=1&pageSize=20"
run "GET stocks lowStock"      "${BASE_URL}/api/v1/warehouse-stocks?pageNumber=1&pageSize=20&lowStockOnly=true"
run "GET import Draft"         "${BASE_URL}/api/v1/import-receipts?pageNumber=1&pageSize=20&status=Draft"
run "GET export Draft"         "${BASE_URL}/api/v1/export-receipts?pageNumber=1&pageSize=20&status=Draft"
run "GET stocktakes"           "${BASE_URL}/api/v1/stocktakes?pageNumber=1&pageSize=20"
run "GET stock-transactions"   "${BASE_URL}/api/v1/stock-transactions?pageNumber=1&pageSize=50"
run "GET health"               "${BASE_URL}/api/v1/health"

# =============================================================================
# B) Concurrent burst — nhiều GET products cùng lúc (đo tranh chấp ngắn)
# =============================================================================
echo
echo "==> B) Concurrent burst (CONCURRENCY=${CONCURRENCY}, TOTAL=${CONCURRENT_TOTAL})"
echo "    Endpoint: GET /api/v1/products?pageNumber=1&pageSize=20"

TMPDIR_PERF=$(mktemp -d)   # mỗi request ghi 1 file r_i.txt = "code time"
# shellcheck disable=SC2064
trap 'rm -rf "$TMPDIR_PERF"' EXIT

# Export biến để hàm run_one (chạy nền) đọc được
# Tránh xargs + JWT dài gây "argument list too long"
export PERF_TOKEN="$TOKEN"
export PERF_BASE="$BASE_URL"
export PERF_OUT="$TMPDIR_PERF"

# run_one — 1 request burst; $1 = chỉ số file
run_one() {
  local i="$1"
  curl -sf -o /dev/null -w '%{http_code} %{time_total}\n' \
    -H "Authorization: Bearer ${PERF_TOKEN}" -H 'Accept: application/json' \
    "${PERF_BASE}/api/v1/products?pageNumber=1&pageSize=20" \
    > "${PERF_OUT}/r_${i}.txt" || echo "000 0" > "${PERF_OUT}/r_${i}.txt"
}
export -f run_one

# Phóng job nền theo lô: mỗi CONCURRENCY request thì wait một lần
for i in $(seq 1 "$CONCURRENT_TOTAL"); do
  run_one "$i" &
  if (( i % CONCURRENCY == 0 )); then
    wait
  fi
done
wait   # chờ lô cuối

# Tổng hợp: đếm HTTP 200, avg, p95; ngưỡng nới hơn tuần tự (* 1.5) vì có contention
python3 - "$TMPDIR_PERF" "$AVG_WARN_MS" "$P95_WARN_MS" <<'PY'
import pathlib, sys
folder = pathlib.Path(sys.argv[1])
avg_warn, p95_warn = int(sys.argv[2]), int(sys.argv[3])
codes, ms = [], []
for f in sorted(folder.glob("r_*.txt")):
    text = f.read_text().strip().split()
    if len(text) != 2:
        continue
    codes.append(int(text[0]))
    ms.append(int(float(text[1]) * 1000))
ms.sort()
n = len(ms)
ok_http = sum(1 for c in codes if c == 200)
avg = sum(ms) / n if n else 0
p95 = ms[max(0, int(n * 0.95) - 1)] if n else 0
print(f"  requests={n}  http200={ok_http}/{n}  avg={avg:.0f}ms  p95={p95}ms  max={ms[-1] if ms else 0}ms")
# Nới 1.5× so với ngưỡng tuần tự vì concurrent luôn chậm hơn một chút
status = "OK" if ok_http == n and avg <= avg_warn * 1.5 and p95 <= p95_warn * 1.5 else "SLOW"
print(f"  concurrent_status={status}")
sys.exit(0 if status == "OK" else 2)
PY
CONC_RC=$?
[[ $CONC_RC -ne 0 ]] && FAILS=$((FAILS + 1))

# =============================================================================
# C) Dashboard parallel — mô phỏng UI gọi ~9 API cùng lúc (Promise.all)
# Đo wall_time = thời điểm bắt đầu → sau khi wait hết tất cả
# =============================================================================
echo
echo "==> C) Dashboard parallel (mô phỏng Promise.all của UI)"
START=$(python3 -c "import time; print(time.time())")
curl -sf -o /dev/null -H "$AUTH" "${BASE_URL}/api/v1/products?pageNumber=1&pageSize=1" &
curl -sf -o /dev/null -H "$AUTH" "${BASE_URL}/api/v1/warehouses?pageNumber=1&pageSize=1" &
curl -sf -o /dev/null -H "$AUTH" "${BASE_URL}/api/v1/customers?pageNumber=1&pageSize=1" &
curl -sf -o /dev/null -H "$AUTH" "${BASE_URL}/api/v1/suppliers?pageNumber=1&pageSize=1" &
curl -sf -o /dev/null -H "$AUTH" "${BASE_URL}/api/v1/categories?pageNumber=1&pageSize=1" &
curl -sf -o /dev/null -H "$AUTH" "${BASE_URL}/api/v1/warehouse-stocks?pageNumber=1&pageSize=12&lowStockOnly=true" &
curl -sf -o /dev/null -H "$AUTH" "${BASE_URL}/api/v1/import-receipts?pageNumber=1&pageSize=1&status=Draft" &
curl -sf -o /dev/null -H "$AUTH" "${BASE_URL}/api/v1/export-receipts?pageNumber=1&pageSize=1&status=Draft" &
curl -sf -o /dev/null -H "$AUTH" "${BASE_URL}/api/v1/stock-transactions?pageNumber=1&pageSize=50" &
wait
END=$(python3 -c "import time; print(time.time())")
DASH_MS=$(python3 -c "print(int((float('$END')-float('$START'))*1000))")
echo "  wall_time=${DASH_MS}ms (song song ~9 API)"
# Ngưỡng cứng 2000ms: dashboard chậm → UX cảm giác “đơ”
if (( DASH_MS > 2000 )); then
  echo "  ${YLW}[SLOW] Dashboard parallel > 2000ms${NC}"
  FAILS=$((FAILS + 1))
else
  echo "  ${GRN}[OK] Dashboard parallel${NC}"
fi

# =============================================================================
# Tóm tắt
# =============================================================================
echo
echo "==> Tóm tắt"
echo "  Endpoints chậm (SLOW): ${FAILS}"
echo "  Gợi ý: xem SQL §10 + index; tăng ROUNDS để ổn định hơn."
if [[ "$FAIL_ON_THRESHOLD" == "1" && "$FAILS" -gt 0 ]]; then
  echo "${RED}FAIL_ON_THRESHOLD=1 → exit 1${NC}"
  exit 1
fi
echo "${GRN}Done.${NC}"
