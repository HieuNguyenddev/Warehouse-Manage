/**
 * perf-k6.js — k6 load test nhiều kịch bản cho Warehouse API
 *
 * Cần cài k6: https://k6.io  (brew install k6 / choco install k6 …)
 *
 * Chạy:
 *   k6 run -e SCENARIO=smoke  -e BASE_URL=http://localhost:5136 backend/scripts/perf-k6.js
 *   k6 run -e SCENARIO=load   -e BASE_URL=http://localhost:5136 backend/scripts/perf-k6.js
 *   k6 run -e SCENARIO=stress ...
 *   k6 run -e SCENARIO=spike  ...
 *   k6 run -e SCENARIO=soak   -e SOAK_DURATION=3m ...
 *   k6 run -e SCENARIO=mixed  ...
 *
 * Biến môi trường (-e):
 *   BASE_URL      — URL API (mặc định http://localhost:5136)
 *   SCENARIO      — smoke | load | stress | spike | soak | mixed (mặc định load)
 *   SOAK_DURATION — chỉ dùng khi SCENARIO=soak (vd "2m", "5m")
 *
 * Khác bash scripts:
 *   k6 mô phỏng Virtual Users (VU) chuyên nghiệp hơn: ramp theo thời gian,
 *   arrival-rate, thresholds tự fail test. Dùng khi cần báo cáo chuẩn / CI.
 */

import http from 'k6/http';
import { check, sleep, group } from 'k6';
import { Rate, Trend } from 'k6/metrics';

// ---------------------------------------------------------------------------
// Cấu hình từ môi trường
// __ENV.X = biến truyền bằng k6 -e X=value
// ---------------------------------------------------------------------------
const BASE = __ENV.BASE_URL || 'http://localhost:5136'; // gốc API
const SCENARIO = (__ENV.SCENARIO || 'load').toLowerCase(); // tên kịch bản đang chạy

// ---------------------------------------------------------------------------
// Metric tùy chỉnh (hiện thêm trên báo cáo k6 ngoài http_req_*)
// Rate  : tỉ lệ true/false → dùng đếm % lỗi nghiệp vụ
// Trend : phân phối thời gian (ms) → có p95/p99 riêng
// ---------------------------------------------------------------------------
const errorRate = new Rate('wm_errors'); // true = lỗi (không 2xx)
const productTrend = new Trend('wm_products_ms', true); // true = giá trị là milliseconds

// ---------------------------------------------------------------------------
// Định nghĩa từng scenario (object scenarios của k6 options)
//
// executor phổ biến:
//   constant-vus        — giữ cố định số VU trong suốt duration
//   ramping-vus         — tăng/giảm số VU theo stages [{duration, target}]
//   ramping-arrival-rate— tăng/giảm số request MỚI mỗi timeUnit (không phụ thuộc
//                         response time như VU thuần)
//
// VU (Virtual User) ≈ 1 “user giả” lặp lại hàm default export
// ---------------------------------------------------------------------------
const scenarios = {
  // smoke: vài VU, thời gian ngắn — xác nhận script + API chạy được trước khi load nặng
  smoke: {
    executor: 'constant-vus',
    vus: 2, // 2 user ảo
    duration: '20s', // chạy 20 giây
  },

  // load: tải trung bình ổn định — “ngày thường”
  load: {
    executor: 'ramping-vus',
    startVUs: 0, // bắt đầu từ 0 VU
    stages: [
      { duration: '20s', target: 10 }, // 20s leo lên 10 VU
      { duration: '40s', target: 10 }, // giữ 10 VU trong 40s
      { duration: '10s', target: 0 }, // hạ về 0 (cool-down)
    ],
  },

  // stress: tăng dần sát giới hạn — tìm điểm bắt đầu lỗi / chậm
  stress: {
    executor: 'ramping-vus',
    startVUs: 0,
    stages: [
      { duration: '20s', target: 20 }, // lên 20
      { duration: '30s', target: 40 }, // lên 40
      { duration: '30s', target: 60 }, // lên 60
      { duration: '20s', target: 0 }, // hạ tải
    ],
  },

  // spike: đột biến — mô phỏng flash crowd / mở ca làm việc đồng loạt
  spike: {
    executor: 'ramping-vus',
    startVUs: 5, // nền sẵn 5 VU
    stages: [
      { duration: '10s', target: 5 }, // giữ nền
      { duration: '5s', target: 80 }, // nhảy vọt lên 80 trong 5s
      { duration: '20s', target: 80 }, // giữ đỉnh
      { duration: '10s', target: 5 }, // hạ về nền
      { duration: '10s', target: 0 }, // tắt
    ],
  },

  // soak: tải vừa phải nhưng LÂU — bắt memory leak / connection leak / chậm dần
  soak: {
    executor: 'constant-vus',
    vus: 15, // 15 VU cố định
    // SOAK_DURATION override được; mặc định 2 phút (demo). Prod/soak thật: 30m–2h
    duration: __ENV.SOAK_DURATION || '2m',
  },

  // mixed: điều khiển theo arrival-rate — số request/giây, endpoint ngẫu nhiên
  mixed: {
    executor: 'ramping-arrival-rate',
    startRate: 5, // bắt đầu ~5 iteration/giây
    timeUnit: '1s', // đơn vị của rate = mỗi giây
    preAllocatedVUs: 30, // cấp sẵn 30 VU (tránh allocate giữa chừng)
    maxVUs: 50, // trần VU nếu rate cần nhiều hơn
    stages: [
      { duration: '20s', target: 10 }, // lên 10 iter/s
      { duration: '40s', target: 25 }, // lên 25 iter/s
      { duration: '20s', target: 5 }, // hạ 5 iter/s
    ],
  },
};

// ---------------------------------------------------------------------------
// options — cấu hình chính k6 export
// scenarios: chỉ bật ĐÚNG 1 scenario theo SCENARIO (tránh chạy chồng)
// thresholds: điều kiện PASS/FAIL tự động (k6 exit ≠ 0 nếu vượt)
// ---------------------------------------------------------------------------
export const options = {
  scenarios: {
    // key động = tên scenario; fallback load nếu SCENARIO sai chính tả
    [SCENARIO]: scenarios[SCENARIO] || scenarios.load,
  },
  thresholds: {
    // < 5% request HTTP bị coi là failed (4xx/5xx/network theo k6)
    http_req_failed: ['rate<0.05'],
    // p95 toàn cục < 1.5s; p99 < 3s
    http_req_duration: ['p(95)<1500', 'p(99)<3000'],
    // metric custom: tỉ lệ lỗi nghiệp vụ < 5%
    wm_errors: ['rate<0.05'],
    // latency riêng của GET products: p95 < 1.2s
    wm_products_ms: ['p(95)<1200'],
  },
};

// ---------------------------------------------------------------------------
// setup() — chạy 1 lần trước khi VU bắt đầu
// Login lấy JWT, trả object { token } → truyền vào default function(data)
// ---------------------------------------------------------------------------
export function setup() {
  const res = http.post(
    `${BASE}/api/v1/auth/admin/login`,
    JSON.stringify({ username: 'admin', password: '123456' }),
    { headers: { 'Content-Type': 'application/json' } },
  );
  check(res, { 'login 200': (r) => r.status === 200 });
  const token = res.json('data.token'); // đường dẫn JSON: { data: { token } }
  if (!token) {
    throw new Error('Không lấy được token — API/login lỗi');
  }
  return { token };
}

// authParams — header chuẩn cho API bảo vệ JWT
function authParams(token) {
  return { headers: { Authorization: `Bearer ${token}`, Accept: 'application/json' } };
}

// hit — GET 1 path, ghi metric lỗi, trả Response
// path: phần sau BASE, vd '/api/v1/products?pageNumber=1&pageSize=20'
function hit(token, path) {
  const res = http.get(`${BASE}${path}`, authParams(token));
  const ok = res.status >= 200 && res.status < 300;
  errorRate.add(!ok); // true = lỗi → làm tăng rate wm_errors
  return res;
}

// ---------------------------------------------------------------------------
// default function — mỗi VU lặp hàm này cho đến hết scenario
// data = object trả về từ setup() → data.token
// ---------------------------------------------------------------------------
export default function (data) {
  const token = data.token;

  // --- Nhánh mixed: mỗi iteration chọn ngẫu nhiên 1 trong danh sách endpoint ---
  if (SCENARIO === 'mixed') {
    group('mixed-read', () => {
      const paths = [
        '/api/v1/products?pageNumber=1&pageSize=20', // list trang đầu
        '/api/v1/products?pageNumber=40&pageSize=10', // trang sâu
        '/api/v1/products?search=Gao&pageSize=20', // search
        '/api/v1/categories?pageNumber=1&pageSize=20',
        '/api/v1/warehouses?pageNumber=1&pageSize=20',
        '/api/v1/warehouse-stocks?lowStockOnly=true&pageSize=20', // query nặng hơn
        '/api/v1/import-receipts?status=Draft&pageSize=20',
        '/api/v1/export-receipts?status=Draft&pageSize=20',
        '/api/v1/stock-transactions?pageSize=40',
        '/api/v1/customers?pageNumber=1&pageSize=20',
      ];
      // Math.random → index 0..length-1
      const path = paths[Math.floor(Math.random() * paths.length)];
      const res = hit(token, path);
      check(res, { '2xx': (r) => r.status >= 200 && r.status < 300 });
    });
    sleep(0.2); // nghĩ 200ms giữa các iteration (giảm đập liên tục)
    return;
  }

  // --- Mặc định (smoke/load/stress/spike/soak): luôn đo products list ---
  group('products-list', () => {
    const res = hit(token, '/api/v1/products?pageNumber=1&pageSize=20');
    productTrend.add(res.timings.duration); // ghi vào wm_products_ms
    check(res, { 'products 200': (r) => r.status === 200 });
  });

  // stress & soak thêm lowStock — query join/filter nặng hơn list thường
  if (SCENARIO === 'stress' || SCENARIO === 'soak') {
    group('stocks-low', () => {
      const res = hit(token, '/api/v1/warehouse-stocks?lowStockOnly=true&pageSize=20');
      check(res, { 'stocks 200': (r) => r.status === 200 });
    });
  }

  // spike: nghĩ ít hơn (0.1s) → dồn request dày hơn; scenario khác nghĩ 0.3s
  sleep(SCENARIO === 'spike' ? 0.1 : 0.3);
}
