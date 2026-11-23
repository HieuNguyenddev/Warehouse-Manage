#!/usr/bin/env bash
# =============================================================================
# perf-load.sh — Test ĐỘ CHỊU TẢI (load / stress / spike / soak / mixed / …)
#
# Mục đích: mô phỏng nhiều user cùng lúc gọi API để xem hệ thống chịu được
#           bao nhiêu tải (latency, % lỗi) với dữ liệu seed ~500 bản ghi/bảng.
#
# Cách chạy:
#   chmod +x backend/scripts/perf-load.sh
#   ./backend/scripts/perf-load.sh                 # chạy TẤT CẢ scenario
#   ./backend/scripts/perf-load.sh ramp spike      # chỉ chạy các scenario chỉ định
#
# Scenario hỗ trợ:
#   ramp      — tăng dần số request song song (stress theo bậc)
#   spike     — đột ngột dồn nhiều request (đột biến)
#   soak      — giữ tải ổn định trong N giây (phát hiện leak / chậm dần)
#   mixed     — xen kẽ nhiều endpoint khác nhau (giống user thật)
#   authstorm — nhiều POST login song song (đo auth + hash password)
#   write     — POST tạo category rồi DELETE (đo ghi DB)
#   deep      — gọi products với pageNumber xoay 1..50 (phân trang sâu)
#
# Override cấu hình bằng biến môi trường, ví dụ:
#   BASE_URL=http://127.0.0.1:5136 RAMP_LEVELS=5,10,20 ./backend/scripts/perf-load.sh ramp
# =============================================================================

# set -e  : thoát ngay nếu lệnh trả mã lỗi (trừ khi được bắt bằng if/||)
# set -u  : báo lỗi nếu dùng biến chưa gán
# set -o pipefail : pipeline fail nếu bất kỳ lệnh nào trong pipe fail
set -euo pipefail

# -----------------------------------------------------------------------------
# Kết nối API & tài khoản đăng nhập
# ${VAR:-default} = lấy $VAR nếu đã set, không thì dùng default
# -----------------------------------------------------------------------------
BASE_URL="${BASE_URL:-http://localhost:5136}"  # Địa chỉ API (Kestrel http profile = 5136)
USER_NAME="${USER_NAME:-admin}"                # Username seed (DataSeeder)
PASSWORD="${PASSWORD:-123456}"                 # Password seed

# -----------------------------------------------------------------------------
# Cấu hình từng scenario — có thể override bằng env trước khi chạy script
# -----------------------------------------------------------------------------

# --- ramp: tăng dần concurrency theo từng bậc ---
# RAMP_LEVELS  : danh sách số request chạy ĐỒNG THỜI mỗi bậc (cách nhau bằng dấu phẩy)
#                Ví dụ 2,5,10,20,40 → bậc 1 chạy 2 song song, bậc 2 chạy 5, …
# RAMP_REQUESTS: tổng số request GỬI ở mỗi bậc (chia theo concurrency bằng wait theo lô)
RAMP_LEVELS="${RAMP_LEVELS:-2,5,10,20,40}"
RAMP_REQUESTS="${RAMP_REQUESTS:-40}"

# --- spike: đột biến tải ---
# SPIKE_CONCURRENCY: số request song song trong “đợt đột biến”
# SPIKE_REQUESTS   : tổng số request trong scenario spike
SPIKE_CONCURRENCY="${SPIKE_CONCURRENCY:-50}"
SPIKE_REQUESTS="${SPIKE_REQUESTS:-100}"

# --- soak: giữ tải lâu ---
# SOAK_CONCURRENCY: luôn duy trì khoảng N request đang chạy (worker pool)
# SOAK_SECONDS    : thời gian chạy liên tục (giây). Demo ngắn = 30; thực tế có thể 300–1800
SOAK_CONCURRENCY="${SOAK_CONCURRENCY:-10}"
SOAK_SECONDS="${SOAK_SECONDS:-30}"

# --- mixed: nhiều endpoint xen kẽ ---
# MIXED_CONCURRENCY: số request song song
# MIXED_REQUESTS   : tổng số request (mỗi request chọn 1 URL trong danh sách xoay vòng)
MIXED_CONCURRENCY="${MIXED_CONCURRENCY:-12}"
MIXED_REQUESTS="${MIXED_REQUESTS:-60}"

# --- authstorm: storm đăng nhập ---
# AUTH_CONCURRENCY: số POST /login chạy song song
# AUTH_REQUESTS   : tổng số lần login thử
AUTH_CONCURRENCY="${AUTH_CONCURRENCY:-15}"
AUTH_REQUESTS="${AUTH_REQUESTS:-45}"

# --- write: tạo + xoá category (ghi DB) ---
# WRITE_CONCURRENCY: số thao tác ghi song song (nên thấp hơn read để tránh lock)
# WRITE_REQUESTS   : tổng số vòng POST+DELETE
WRITE_CONCURRENCY="${WRITE_CONCURRENCY:-5}"
WRITE_REQUESTS="${WRITE_REQUESTS:-20}"

# --- deep: phân trang sâu ---
# DEEP_CONCURRENCY: số request song song
# DEEP_REQUESTS   : tổng request; pageNumber = 1 + (i % 50) → trang 1..50
DEEP_CONCURRENCY="${DEEP_CONCURRENCY:-10}"
DEEP_REQUESTS="${DEEP_REQUESTS:-40}"

# -----------------------------------------------------------------------------
# Ngưỡng đánh giá PASS / FAIL cho mỗi burst
# MAX_ERROR_PCT : % request không thuộc HTTP 2xx cho phép (mặc định 5%)
# P95_WARN_MS   : p95 latency tối đa (ms). p95 = 95% request nhanh hơn hoặc bằng giá trị này
# -----------------------------------------------------------------------------
MAX_ERROR_PCT="${MAX_ERROR_PCT:-5}"
P95_WARN_MS="${P95_WARN_MS:-1500}"

# Mã màu ANSI in terminal (không ảnh hưởng logic test)
RED=$'\033[0;31m'   # đỏ — lỗi / API down
GRN=$'\033[0;32m'   # xanh — pass
YLW=$'\033[0;33m'   # vàng — cảnh báo / đang chạy
CYN=$'\033[0;36m'   # cyan — tiêu đề section
NC=$'\033[0m'       # reset màu

# -----------------------------------------------------------------------------
# Chọn scenario từ tham số dòng lệnh ($@).
# Không truyền gì → chạy đủ 7 scenario theo thứ tự dưới.
# -----------------------------------------------------------------------------
SCENARIOS=("$@")
if [[ ${#SCENARIOS[@]} -eq 0 ]]; then
  SCENARIOS=(ramp spike soak mixed authstorm write deep)
fi

# -----------------------------------------------------------------------------
# Kiểm tra API sống trước khi đo (tránh báo FAIL giả do server chưa chạy)
# curl -s = silent, -f = fail (exit ≠0) nếu HTTP lỗi
# -----------------------------------------------------------------------------
echo "${CYN}==> Health ${BASE_URL}${NC}"
curl -sf "${BASE_URL}/api/v1/health" >/dev/null || {
  echo "${RED}API chưa chạy.${NC}"; exit 1
}

# -----------------------------------------------------------------------------
# Đăng nhập 1 lần → lấy JWT dùng cho mọi request đọc/ghi (trừ authstorm)
# PERF_TOKEN / PERF_BASE export để các hàm worker (chạy nền &) đọc được
# -----------------------------------------------------------------------------
echo "==> Login"
LOGIN_JSON=$(curl -sf -X POST "${BASE_URL}/api/v1/auth/admin/login" \
  -H 'Content-Type: application/json' \
  -d "{\"username\":\"${USER_NAME}\",\"password\":\"${PASSWORD}\"}")
# Parse JSON: data.token (cần python3 trên máy)
TOKEN=$(python3 -c "import sys,json; print(json.loads(sys.argv[1])['data']['token'])" "$LOGIN_JSON")
export PERF_TOKEN="$TOKEN"   # JWT Bearer
export PERF_BASE="$BASE_URL" # URL gốc cho worker

# Thư mục tạm chứa file kết quả từng request: mỗi dòng "http_code time_seconds"
TMP=$(mktemp -d)
# Xoá TMP khi script thoát (kể cả khi Ctrl+C / lỗi)
trap 'rm -rf "$TMP"' EXIT

# =============================================================================
# WORKER — mỗi worker = 1 request HTTP, ghi 1 dòng kết quả vào file $out
# =============================================================================

# --- get_worker: GET có Bearer token ---
# Tham số: $1 = đường dẫn file kết quả, $2 = URL đầy đủ
get_worker() {
  local out="$1" url="$2"   # out: file append; url: endpoint cần gọi
  local code time           # HTTP status + thời gian tổng (giây, float)
  # curl -w in format: "200 0.012" → đọc vào code và time
  # --max-time 30: timeout 30s (tránh treo vô hạn khi server quá tải)
  # || echo "000 0": nếu curl crash → coi như lỗi mạng, code 000
  # shellcheck disable=SC2034
  read -r code time < <(curl -s -o /dev/null -w "%{http_code} %{time_total}" \
    -H "Authorization: Bearer ${PERF_TOKEN}" -H 'Accept: application/json' \
    --max-time 30 \
    "$url" || echo "000 0")
  echo "$code $time" >>"$out"
}
# export -f: cho phép gọi hàm từ subshell / background job (&)
export -f get_worker

# --- login_worker: POST login (không dùng PERF_TOKEN) ---
# Dùng trong scenario authstorm
login_worker() {
  local out="$1"
  local code time
  read -r code time < <(curl -s -o /dev/null -w "%{http_code} %{time_total}" \
    -X POST "${PERF_BASE}/api/v1/auth/admin/login" \
    -H 'Content-Type: application/json' \
    --max-time 30 \
    -d "{\"username\":\"${USER_NAME}\",\"password\":\"${PASSWORD}\"}" || echo "000 0")
  echo "$code $time" >>"$out"
}
export -f login_worker
export USER_NAME PASSWORD   # login_worker cần đọc 2 biến này trong subshell

# --- write_worker: POST tạo category rồi DELETE (đo tải ghi) ---
# Tham số: $1 = file kết quả, $2 = chỉ số request (làm tên unique)
write_worker() {
  local out="$1" idx="$2"
  # Tên category unique theo idx + nanosecond để tránh trùng Unique constraint
  local name="PerfLoad ${idx}-$(date +%s%N)"
  local start end code create_code del_code
  # Đo wall-time toàn bộ create+delete
  start=$(python3 -c "import time; print(time.time())")
  local body
  # -w "\n%{http_code}": body JSON + dòng cuối là status code
  body=$(curl -s -w "\n%{http_code}" -X POST "${PERF_BASE}/api/v1/categories" \
    -H "Authorization: Bearer ${PERF_TOKEN}" -H 'Content-Type: application/json' \
    --max-time 30 \
    -d "{\"name\":\"${name}\",\"description\":\"perf load\",\"isActive\":true}" || echo $'\n000')
  create_code=$(echo "$body" | tail -n1)   # dòng cuối = HTTP code
  local json
  json=$(echo "$body" | sed '$d')          # bỏ dòng cuối → phần JSON
  local id
  # Lấy data.id để xoá; lỗi parse → id rỗng
  id=$(python3 -c "import sys,json
try:
 d=json.loads(sys.argv[1]); print((d.get('data') or {}).get('id') or '')
except Exception:
 print('')" "$json" 2>/dev/null || true)
  del_code=204
  if [[ -n "$id" ]]; then
    # Xoá bản ghi vừa tạo để không làm bẩn DB sau mỗi lần chạy test
    del_code=$(curl -s -o /dev/null -w "%{http_code}" -X DELETE \
      -H "Authorization: Bearer ${PERF_TOKEN}" --max-time 30 \
      "${PERF_BASE}/api/v1/categories/${id}" || echo 000)
  fi
  end=$(python3 -c "import time; print(time.time())")
  # Thành công khi create = 201 và delete = 204 hoặc 200; ngược lại ghi code 500
  code=200
  if [[ "$create_code" != "201" ]] || [[ "$del_code" != "204" && "$del_code" != "200" ]]; then
    code=500
  fi
  # Ghi: "code duration_seconds"
  python3 -c "print('$code', float('$end')-float('$start'))" >>"$out"
}
export -f write_worker

# burst — chạy N request với concurrency C, rồi in thống kê min/p50/avg/p95/p99/max
#
# Tham số:
#   $1 label       : tên hiển thị (vd ramp_c10, spike)
#   $2 concurrency : số job nền tối đa cùng lúc trước khi wait
#   $3 total       : tổng số request cần gửi
#   $4 mode        : get | login | write | mixed | deep
#
# Cơ chế concurrency:
#   for mỗi request → chạy worker & → tăng running
#   khi running >= concurrency → wait (chờ lô hiện tại xong) → reset running=0
burst() {
  local label="$1" concurrency="$2" total="$3" mode="$4"
  local out="${TMP}/${label}.txt"   # file kết quả riêng cho label này
  : >"$out"                         # tạo/xoá trắng file
  local i=0 running=0               # i = chỉ số request; running = số job đang chờ

  # Danh sách URL dùng cho mode=mixed (xoay vòng theo i % length)
  local urls=(
    "${PERF_BASE}/api/v1/products?pageNumber=1&pageSize=20"
    "${PERF_BASE}/api/v1/products?pageNumber=25&pageSize=20"
    "${PERF_BASE}/api/v1/products?search=Gao&pageSize=20"
    "${PERF_BASE}/api/v1/categories?pageNumber=1&pageSize=20"
    "${PERF_BASE}/api/v1/warehouses?pageNumber=1&pageSize=20"
    "${PERF_BASE}/api/v1/warehouse-stocks?pageNumber=1&pageSize=20"
    "${PERF_BASE}/api/v1/warehouse-stocks?lowStockOnly=true&pageSize=20"
    "${PERF_BASE}/api/v1/import-receipts?status=Draft&pageSize=20"
    "${PERF_BASE}/api/v1/export-receipts?status=Draft&pageSize=20"
    "${PERF_BASE}/api/v1/stock-transactions?pageSize=50"
    "${PERF_BASE}/api/v1/customers?pageNumber=1&pageSize=20"
    "${PERF_BASE}/api/v1/suppliers?pageNumber=1&pageSize=20"
  )

  echo "  ${YLW}→ ${label}: concurrency=${concurrency} total=${total} mode=${mode}${NC}"

  for ((i = 1; i <= total; i++)); do
    case "$mode" in
      get)
        # Luôn gọi trang 1 products — baseline đọc ổn định
        get_worker "$out" "${PERF_BASE}/api/v1/products?pageNumber=1&pageSize=20" &
        ;;
      deep)
        # pageNumber xoay 1..50: OFFSET lớn hơn → thường chậm hơn trang đầu
        get_worker "$out" "${PERF_BASE}/api/v1/products?pageNumber=$((1 + i % 50))&pageSize=20" &
        ;;
      mixed)
        # Chọn URL theo vòng: request 1→url[0], 2→url[1], … rồi quay lại
        get_worker "$out" "${urls[$((i % ${#urls[@]}))]}" &
        ;;
      login)
        login_worker "$out" &
        ;;
      write)
        write_worker "$out" "$i" &
        ;;
      *)
        # mode lạ → fallback giống get
        get_worker "$out" "${PERF_BASE}/api/v1/products?pageNumber=1&pageSize=20" &
        ;;
    esac
    running=$((running + 1))
    # Đủ 1 “lô” concurrency → chờ hết rồi mới phóng lô tiếp
    if (( running >= concurrency )); then
      wait
      running=0
    fi
  done
  wait   # chờ lô cuối (có thể < concurrency)

  # Python: đọc file kết quả → tính thống kê + so ngưỡng → exit 0=OK, 2=FAIL
  # Tham số truyền vào: path, label, MAX_ERROR_PCT, P95_WARN_MS
  python3 - "$out" "$label" "$MAX_ERROR_PCT" "$P95_WARN_MS" <<'PY'
import sys
path, label, max_err, p95_warn = sys.argv[1], sys.argv[2], float(sys.argv[3]), int(sys.argv[4])
codes, ms = [], []
with open(path) as f:
    for line in f:
        parts = line.strip().split()
        if len(parts) != 2:
            continue
        codes.append(int(parts[0]))
        # time_total của curl là giây → nhân 1000 ra milliseconds
        ms.append(int(float(parts[1]) * 1000))
n = len(ms)
if n == 0:
    print(f"  [{label}] NO_DATA")
    sys.exit(2)
ms.sort()
# HTTP 2xx = thành công
ok = sum(1 for c in codes if 200 <= c < 300)
err = n - ok
err_pct = 100.0 * err / n
avg = sum(ms) / n
p50 = ms[n // 2]                          # trung vị
p95 = ms[max(0, int(n * 0.95) - 1)]       # 95-percentile
p99 = ms[max(0, int(n * 0.99) - 1)]       # 99-percentile
status = "OK"
# FAIL nếu lỗi quá ngưỡng HOẶC p95 quá chậm
if err_pct > max_err or p95 > p95_warn:
    status = "FAIL"
print(
    f"  [{status}] {label}: n={n} ok={ok} err={err} ({err_pct:.1f}%) "
    f"min={ms[0]}ms p50={p50}ms avg={avg:.0f}ms p95={p95}ms p99={p99}ms max={ms[-1]}ms"
)
sys.exit(0 if status == "OK" else 2)
PY
}

# soak_run — giữ tải liên tục trong $seconds giây (không cố định số request)
#
# Tham số:
#   $1 concurrency : số worker tối đa cùng lúc
#   $2 seconds     : thời gian chạy
# Vòng while: còn trước thời điểm kết thúc → phóng thêm GET mixed → wait theo lô
# 
soak_run() {
  local concurrency="$1" seconds="$2"
  local out="${TMP}/soak.txt"
  : >"$out"
  echo "  ${YLW}→ soak: concurrency=${concurrency} duration=${seconds}s (mixed GET)${NC}"
  # Tập URL nhỏ hơn mixed — đủ đại diện đọc thường xuyên
  local urls=(
    "${PERF_BASE}/api/v1/products?pageNumber=1&pageSize=20"
    "${PERF_BASE}/api/v1/categories?pageNumber=1&pageSize=20"
    "${PERF_BASE}/api/v1/warehouse-stocks?pageNumber=1&pageSize=20"
    "${PERF_BASE}/api/v1/import-receipts?status=Draft&pageSize=10"
    "${PERF_BASE}/api/v1/stock-transactions?pageSize=30"
  )
  # end = timestamp Unix (giây) lúc phải dừng
  local end
  end=$(python3 -c "import time; print(time.time()+float('$seconds'))")
  local i=0
  local running=0
  # Tiếp tục khi now < end
  while python3 -c "import time,sys; sys.exit(0 if time.time()<float('$end') else 1)"; do
    i=$((i + 1))
    get_worker "$out" "${urls[$((i % ${#urls[@]}))]}" &
    running=$((running + 1))
    if (( running >= concurrency )); then
      wait
      running=0
    fi
  done
  wait
  # Cùng công thức thống kê như burst
  python3 - "$out" "soak_${concurrency}x${seconds}s" "$MAX_ERROR_PCT" "$P95_WARN_MS" <<'PY'
import sys
path, label, max_err, p95_warn = sys.argv[1], sys.argv[2], float(sys.argv[3]), int(sys.argv[4])
codes, ms = [], []
with open(path) as f:
    for line in f:
        parts = line.strip().split()
        if len(parts) != 2: continue
        codes.append(int(parts[0])); ms.append(int(float(parts[1])*1000))
n=len(ms)
if not n:
    print("  [FAIL] soak NO_DATA"); sys.exit(2)
ms.sort()
ok=sum(1 for c in codes if 200<=c<300)
err=n-ok
err_pct=100.0*err/n
p50=ms[n//2]; p95=ms[max(0,int(n*0.95)-1)]; p99=ms[max(0,int(n*0.99)-1)]
avg=sum(ms)/n
status="OK" if err_pct<=max_err and p95<=p95_warn else "FAIL"
print(f"  [{status}] {label}: n={n} ok={ok} err={err} ({err_pct:.1f}%) p50={p50}ms avg={avg:.0f}ms p95={p95}ms p99={p99}ms max={ms[-1]}ms")
sys.exit(0 if status=="OK" else 2)
PY
}

# =============================================================================
# Điều phối: chạy lần lượt từng scenario trong SCENARIOS[]
# FAILS đếm số lần burst/soak trả FAIL (exit code ≠ 0)
# =============================================================================
FAILS=0

for sc in "${SCENARIOS[@]}"; do
  echo
  echo "${CYN}========== Scenario: ${sc} ==========${NC}"
  case "$sc" in
    ramp)
      # Tách RAMP_LEVELS "2,5,10,…" thành mảng LEVELS; mỗi bậc gọi burst 1 lần
      IFS=',' read -ra LEVELS <<<"$RAMP_LEVELS"
      for c in "${LEVELS[@]}"; do
        if ! burst "ramp_c${c}" "$c" "$RAMP_REQUESTS" get; then FAILS=$((FAILS + 1)); fi
      done
      ;;
    spike)
      echo "  (đột ngột tăng tải)"
      if ! burst "spike" "$SPIKE_CONCURRENCY" "$SPIKE_REQUESTS" get; then FAILS=$((FAILS + 1)); fi
      ;;
    soak)
      if ! soak_run "$SOAK_CONCURRENCY" "$SOAK_SECONDS"; then FAILS=$((FAILS + 1)); fi
      ;;
    mixed)
      if ! burst "mixed_read" "$MIXED_CONCURRENCY" "$MIXED_REQUESTS" mixed; then FAILS=$((FAILS + 1)); fi
      ;;
    authstorm)
      echo "  (nhiều login song song — đo auth + DB)"
      if ! burst "authstorm" "$AUTH_CONCURRENCY" "$AUTH_REQUESTS" login; then FAILS=$((FAILS + 1)); fi
      ;;
    write)
      echo "  (POST category + DELETE — ghi DB ngắn)"
      if ! burst "write_crud" "$WRITE_CONCURRENCY" "$WRITE_REQUESTS" write; then FAILS=$((FAILS + 1)); fi
      ;;
    deep)
      echo "  (phân trang sâu page 1..50 dưới tải)"
      if ! burst "deep_page" "$DEEP_CONCURRENCY" "$DEEP_REQUESTS" deep; then FAILS=$((FAILS + 1)); fi
      ;;
    *)
      echo "${RED}Unknown scenario: $sc${NC}"
      echo "  Known: ramp spike soak mixed authstorm write deep"
      FAILS=$((FAILS + 1))
      ;;
  esac
done

# -----------------------------------------------------------------------------
# Tổng kết: FAILS > 0 → exit 1 (tiện CI / script bọc ngoài)
# -----------------------------------------------------------------------------
echo
echo "${CYN}========== Tổng kết chịu tải ==========${NC}"
echo "  Scenarios FAIL: ${FAILS}"
echo "  Ngưỡng: error<=${MAX_ERROR_PCT}% ; p95<=${P95_WARN_MS}ms"
if (( FAILS > 0 )); then
  echo "${YLW}Có scenario FAIL — xem log trên; tăng tài nguyên / giảm concurrency / tối ưu query.${NC}"
  exit 1
fi
echo "${GRN}Tất cả scenario đạt ngưỡng.${NC}"
