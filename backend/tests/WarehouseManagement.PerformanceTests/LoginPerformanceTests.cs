using System.Diagnostics; // Stopwatch — đo latency từng request và wall-clock cả burst
using System.Net;         // HttpStatusCode — mã HTTP trả về từ login
using System.Text;        // Encoding.UTF8 — body JSON login
using System.Text.Json;   // JsonDocument — parse token từ response

namespace WarehouseManagement.PerformanceTests;

// LoginPerformanceTests.cs
// Nguyên lý kiểm thử hiệu năng đăng nhập:
//   1) Gửi N request POST login CÙNG LÚC (giới hạn bằng SemaphoreSlim = maxParallel).
//   2) Đếm Ok/Fail, đo min/p50/avg/p95/max latency + wall time.
//   3) So với "budget" (ngưỡng tỷ lệ / wall / p95) → PASS hoặc FAIL.
//   4) Leo thang tải: 50 → 100 → 250 → 500 → 750 → 1000 → 2000 (quá tải).
//   5) Mỗi nấc ghi 1 dòng Excel với cột SoNguoiDangNhapDongThoi = N.

/// <summary>
/// Kịch bản hiệu năng đăng nhập (admin/123456).
/// API: PERF_API_BASE hoặc http://127.0.0.1:5136
/// Sau dotnet test → TestResults/PerformanceTests_Latest.xlsx
///
/// Kịch bản:
///   1) Baseline n=50          → kỳ vọng PASS
///   2) Chịu tải 100→500       → kỳ vọng PASS
///   3) Cao điểm 750→1000      → kỳ vọng PASS (ngưỡng thấp hơn)
///   4) Quá tải n=2000 full    → nếu rate thấp / lỗi nhiều → FAIL (QUÁ TẢI)
/// </summary>
[Collection("PerfReport")] // Gắn collection → dùng PerfReportFixture, chạy tuần tự, ghi Excel cuối
public class LoginPerformanceTests
{
    // URL mặc định khi không set biến môi trường PERF_API_BASE.
    private const string DefaultBaseUrl = "http://127.0.0.1:5136";

    // Đường dẫn tương đối endpoint login admin (ghép với BaseAddress của HttpClient).
    private const string LoginUrl = "api/v1/auth/admin/login";

    // Body JSON cố định — tài khoản seed admin/123456.
    private const string LoginJson = """{"username":"admin","password":"123456"}""";

    /// <summary>
    /// Kết quả thống kê của 1 burst (1 nấc tải với N người đồng thời).
    /// </summary>
    /// <param name="N">Tổng số request = số người đăng nhập đồng thời.</param>
    /// <param name="Ok">Số request thành công (HTTP 2xx + có token).</param>
    /// <param name="Fail">Số request thất bại (= N - Ok).</param>
    /// <param name="WallMs">Thời gian wall-clock từ lúc bắt đầu đến khi mọi task xong.</param>
    /// <param name="MinMs">Latency nhỏ nhất (ms) trong các mẫu đo được.</param>
    /// <param name="P50Ms">Trung vị (percentile 50) — 50% request nhanh hơn mức này.</param>
    /// <param name="AvgMs">Trung bình cộng latency.</param>
    /// <param name="P95Ms">Percentile 95 — 95% request nhanh hơn mức này (đuôi chậm).</param>
    /// <param name="MaxMs">Latency lớn nhất.</param>
    private sealed record BurstResult(
        int N, int Ok, int Fail, long WallMs,
        long MinMs, long P50Ms, long AvgMs, long P95Ms, long MaxMs)
    {
        // Tỷ lệ thành công 0.0 → 1.0; tránh chia 0.
        public double Rate => N == 0 ? 0 : (double)Ok / N;

        // Alias rõ nghĩa cho cột Excel SoNguoiDangNhapDongThoi.
        public int SoNguoiDangNhapDongThoi => N;

        // Chuỗi metrics đưa vào Note / cột Metrics.
        public override string ToString()
            => $"SoNguoiDangNhapDongThoi={N}; ok={Ok}; fail={Fail}; tyLe={Rate:P1}; wall={WallMs}ms; min={MinMs}; p50={P50Ms}; avg={AvgMs}; p95={P95Ms}; max={MaxMs}";
    }

    // -------------------------------------------------------------------------
    // Kịch bản 1 — Baseline (nhẹ): xác nhận API ổn định ở tải nhỏ.
    // -------------------------------------------------------------------------
    [Fact] // xUnit: 1 test case
    public Task Scenario_01_Baseline_Login_n50() => PerfCase.RunAsync(async () =>
    {
        // HttpClient với pool kết nối đủ cho burst nhỏ.
        using var client = await ReadyClientAsync(256);
        PerfCase.Note("kich_ban=1_baseline; n=50; ky_vong=PASS");
        // n=50 người, parallel đầy đủ 50, cho 1 lần retry nếu lỗi tạm thời.
        var r = await RunLoginBurstAsync(client, n: 50, maxParallel: 50, retries: 1);
        // Budget chặt: ≥95% thành công, wall ≤30s, p95 ≤8s.
        AssertPassWithinBudget("KB1_Baseline_n50", r, minRate: 0.95, maxWallMs: 30_000, maxP95Ms: 8_000);
    });

    // -------------------------------------------------------------------------
    // Kịch bản 2 — Chịu tải: leo thang 100 → 250 → 500.
    // -------------------------------------------------------------------------
    [Fact]
    public Task Scenario_02_ChiuTai_Ramp_100_to_500() => PerfCase.RunAsync(async () =>
    {
        using var client = await ReadyClientAsync(512);
        PerfCase.Note("kich_ban=2_chiu_tai; steps=100,250,500; ky_vong=PASS");

        // Mỗi n là một "số người đăng nhập cùng lúc".
        foreach (var n in new[] { 100, 250, 500 })
        {
            PerfCase.Note($"buoc SoNguoiDangNhapDongThoi={n}");
            // MaxParallelFor throttle nhẹ để máy local không sập ngay ở 500.
            var r = await RunLoginBurstAsync(client, n, MaxParallelFor(n), retries: 1);
            // Budget nới dần theo n (CapacityBudget).
            var (minRate, maxWall, maxP95) = CapacityBudget(n);
            AssertPassWithinBudget($"KB2_ChiuTai_n{n}", r, minRate, maxWall, maxP95);
            await Task.Delay(800); // Nghỉ giữa các nấc để server "thở"
        }
    });

    // -------------------------------------------------------------------------
    // Kịch bản 3 — Cao điểm: 750 → 1000 (ngưỡng PASS thấp hơn).
    // -------------------------------------------------------------------------
    [Fact]
    public Task Scenario_03_CaoDiem_750_to_1000() => PerfCase.RunAsync(async () =>
    {
        using var client = await ReadyClientAsync(1024);
        PerfCase.Note("kich_ban=3_cao_diem; steps=750,1000; ky_vong=PASS");

        foreach (var n in new[] { 750, 1000 })
        {
            PerfCase.Note($"buoc SoNguoiDangNhapDongThoi={n}");
            var r = await RunLoginBurstAsync(client, n, MaxParallelFor(n), retries: 1);
            var (minRate, maxWall, maxP95) = CapacityBudget(n);
            AssertPassWithinBudget($"KB3_CaoDiem_n{n}", r, minRate, maxWall, maxP95);
            await Task.Delay(1000);
        }
    });

    // -------------------------------------------------------------------------
    // Kịch bản 4 — Quá tải: full burst n=2000, không throttle, không retry.
    // Mục tiêu: nếu hệ thống quá tải thì Assert.Fail → Excel Result=FAIL.
    // -------------------------------------------------------------------------
    [Fact]
    public Task Scenario_04_QuaTai_FullBurst_n2000() => PerfCase.RunAsync(async () =>
    {
        const int n = 2000; // Số người đăng nhập cùng lúc ở mức quá tải
        // Ngưỡng phát hiện quá tải:
        const double overloadRateCeil = 0.90; // rate < 90% → quá tải
        const int overloadFailFloor = 100;    // hoặc số fail ≥ 100 → quá tải

        using var client = await ReadyClientAsync(n);
        PerfCase.Note("kich_ban=4_qua_tai; n=2000; parallel=full; retry=0; ky_vong=FAIL_neu_qua_tai");

        // Baseline nhỏ trước để so sánh mức sụt giảm rate.
        var baseline = await RunLoginBurstAsync(client, n: 50, maxParallel: 50, retries: 1);
        PerfCase.RecordStep(
            "KB4_Baseline_n50",
            baseline.SoNguoiDangNhapDongThoi,
            baseline.Ok,
            baseline.Fail,
            baseline.WallMs,
            baseline.ToString(),
            passed: baseline.Rate >= 0.95,
            baseline.Rate >= 0.95
                ? $"PASS baseline: {baseline.SoNguoiDangNhapDongThoi} người đăng nhập cùng lúc"
                : $"FAIL baseline: {baseline}");

        await Task.Delay(500);

        // Full concurrent: maxParallel = n, retries = 0 → dễ làm Kestrel/DB quá tải.
        var overload = await RunLoginBurstAsync(client, n, maxParallel: n, retries: 0);

        // Ba điều kiện OR: rate thấp HOẶC fail nhiều HOẶC sụt >10% so baseline.
        var isOverloaded =
            overload.Rate < overloadRateCeil
            || overload.Fail >= overloadFailFloor
            || (baseline.Rate > 0 && overload.Rate < baseline.Rate - 0.10);

        PerfCase.Note(
            $"detect overload={isOverloaded}; rule=rate<{overloadRateCeil:P0} OR fail>={overloadFailFloor} OR rate drop >10% vs baseline");

        if (isOverloaded)
        {
            var msg =
                $"QUÁ TẢI ĐĂNG NHẬP: SoNguoiDangNhapDongThoi={n}, ok={overload.Ok}, fail={overload.Fail}, " +
                $"rate={overload.Rate:P1}, p95={overload.P95Ms}ms, wall={overload.WallMs}ms. " +
                $"Baseline n=50 rate={baseline.Rate:P1}. Hệ thống không đáp ứng đủ dưới tải cực đại.";
            RecordOverloadFail("KB4_QuaTai_n2000", overload, msg); // Ghi FAIL vào Excel
            Assert.Fail(msg); // Đánh dấu test FAILED trên xUnit
        }

        // Máy mạnh / API chịu tốt → vẫn có thể PASS ở 2000.
        AssertPassWithinBudget("KB4_QuaTai_n2000", overload, minRate: 0.90, maxWallMs: 300_000, maxP95Ms: 60_000);
    });

    // -------------------------------------------------------------------------
    // Kịch bản tổng — chạy tuần tự KB1→KB4 trong 1 test (đủ metrics trên 1 SUMMARY).
    // -------------------------------------------------------------------------
    [Fact]
    public Task Scenario_Full_1_to_4() => PerfCase.RunAsync(async () =>
    {
        using var client = await ReadyClientAsync(2048);
        PerfCase.Note("kich_ban=full_1_to_4");

        // --- KB1 baseline ---
        PerfCase.Note("=== KB1 baseline SoNguoiDangNhapDongThoi=50 ===");
        var b1 = await RunLoginBurstAsync(client, 50, 50, 1);
        AssertPassWithinBudget("Full_KB1_Baseline_n50", b1, 0.95, 30_000, 8_000);
        await Task.Delay(500);

        // --- KB2 chịu tải ---
        PerfCase.Note("=== KB2 chiu tai ===");
        foreach (var n in new[] { 100, 250, 500 })
        {
            var r = await RunLoginBurstAsync(client, n, MaxParallelFor(n), 1);
            var budget = CapacityBudget(n); // Tuple (MinRate, MaxWall, MaxP95)
            AssertPassWithinBudget($"Full_KB2_ChiuTai_n{n}", r, budget.MinRate, budget.MaxWall, budget.MaxP95);
            await Task.Delay(500);
        }

        // --- KB3 cao điểm ---
        PerfCase.Note("=== KB3 cao diem ===");
        foreach (var n in new[] { 750, 1000 })
        {
            var r = await RunLoginBurstAsync(client, n, MaxParallelFor(n), 1);
            var budget = CapacityBudget(n);
            AssertPassWithinBudget($"Full_KB3_CaoDiem_n{n}", r, budget.MinRate, budget.MaxWall, budget.MaxP95);
            await Task.Delay(800);
        }

        // --- KB4 quá tải ---
        PerfCase.Note("=== KB4 qua tai SoNguoiDangNhapDongThoi=2000 full ===");
        var over = await RunLoginBurstAsync(client, 2000, maxParallel: 2000, retries: 0);
        if (over.Rate < 0.90 || over.Fail >= 100)
        {
            var msg =
                $"QUÁ TẢI ĐĂNG NHẬP (KB4): SoNguoiDangNhapDongThoi=2000, ok={over.Ok}/2000 fail={over.Fail} rate={over.Rate:P1} " +
                $"p95={over.P95Ms}ms wall={over.WallMs}ms";
            RecordOverloadFail("Full_KB4_QuaTai_n2000", over, msg);
            Assert.Fail(msg);
        }
        AssertPassWithinBudget("Full_KB4_QuaTai_n2000", over, minRate: 0.90, maxWallMs: 300_000, maxP95Ms: 60_000);
    });

    // =========================================================================
    // Helpers — assert, budget, HTTP client, burst, login
    // =========================================================================

    /// <summary>
    /// Kiểm tra BurstResult có nằm trong budget không; ghi 1 dòng Excel; FAIL nếu vượt.
    /// </summary>
    /// <param name="stepName">Tên bước trên Excel.</param>
    /// <param name="r">Kết quả burst vừa chạy.</param>
    /// <param name="minRate">Tỷ lệ thành công tối thiểu (0..1).</param>
    /// <param name="maxWallMs">Wall-clock tối đa cho phép (ms).</param>
    /// <param name="maxP95Ms">P95 latency tối đa cho phép (ms).</param>
    private static void AssertPassWithinBudget(
        string stepName,
        BurstResult r,
        double minRate,
        long maxWallMs,
        long maxP95Ms)
    {
        PerfCase.Note(r.ToString());
        PerfCase.Note($"budget rate>={minRate:P0} wall<={maxWallMs}ms p95<={maxP95Ms}ms");

        // failMsg != null nghĩa là vi phạm ít nhất 1 ngưỡng.
        string? failMsg = null;
        if (r.Rate < minRate)
            failMsg = $"FAIL chịu tải: {r.SoNguoiDangNhapDongThoi} người đồng thời, success {r.Ok}/{r.N} ({r.Rate:P1}) < {minRate:P0}";
        else if (r.WallMs > maxWallMs)
            failMsg = $"FAIL chịu tải: {r.SoNguoiDangNhapDongThoi} người đồng thời, wall {r.WallMs}ms > {maxWallMs}ms";
        else if (r.P95Ms > maxP95Ms)
            failMsg = $"FAIL chịu tải: {r.SoNguoiDangNhapDongThoi} người đồng thời, p95 {r.P95Ms}ms > {maxP95Ms}ms";

        var passed = failMsg is null; // Không có lỗi → PASS
        PerfCase.RecordStep(
            stepName,
            r.SoNguoiDangNhapDongThoi,
            r.Ok,
            r.Fail,
            r.WallMs,
            r.ToString(),
            passed,
            failMsg ?? $"PASS: {r.SoNguoiDangNhapDongThoi} người đăng nhập cùng lúc");

        if (!passed)
            Assert.Fail(failMsg!); // Ném để xUnit + PerfCase.ExecuteAsync ghi SUMMARY FAIL
    }

    /// <summary>
    /// Ghi dòng Excel Result=FAIL cho kịch bản quá tải (không tự Assert).
    /// Caller tự gọi Assert.Fail(message) sau đó.
    /// </summary>
    private static void RecordOverloadFail(string stepName, BurstResult r, string message)
    {
        PerfCase.RecordStep(
            stepName,
            r.SoNguoiDangNhapDongThoi,
            r.Ok,
            r.Fail,
            r.WallMs,
            r.ToString(),
            passed: false,
            message);
    }

    /// <summary>
    /// Budget theo mức N: N càng lớn → rate tối thiểu càng thấp, wall/p95 càng nới.
    /// Trả về tuple đặt tên: (MinRate, MaxWall, MaxP95).
    /// </summary>
    private static (double MinRate, long MaxWall, long MaxP95) CapacityBudget(int n) => n switch
    {
        <= 100 => (0.92, 45_000, 10_000),   // Nhẹ: yêu cầu cao
        <= 250 => (0.85, 60_000, 12_000),
        <= 500 => (0.80, 90_000, 15_000),
        <= 750 => (0.75, 120_000, 20_000),
        _ => (0.70, 180_000, 25_000)        // Cao điểm 1000+: nới nhất
    };

    /// <summary>
    /// Giới hạn song song thực tế (throttle) theo N.
    /// N lớn nhưng maxParallel nhỏ hơn N → vẫn "gần đồng thời" nhưng không mở hết connection.
    /// KB4 cố ý bỏ qua hàm này (maxParallel = n).
    /// </summary>
    private static int MaxParallelFor(int n) => n switch
    {
        <= 100 => n,   // Nhỏ: mở full
        <= 250 => 100,
        <= 500 => 125,
        <= 750 => 150,
        _ => 200       // 1000+: tối đa 200 connection đồng thời
    };

    /// <summary>
    /// Tạo HttpClient sẵn sàng: đọc base URL → tạo client → ping API → warmup login.
    /// </summary>
    /// <param name="maxConnections">MaxConnectionsPerServer tối thiểu muốn mở.</param>
    private static async Task<HttpClient> ReadyClientAsync(int maxConnections)
    {
        // Ưu tiên biến môi trường PERF_API_BASE; không có thì DefaultBaseUrl.
        var baseUrl = (Environment.GetEnvironmentVariable("PERF_API_BASE") ?? DefaultBaseUrl).TrimEnd('/');
        var client = CreateClient(baseUrl, maxConnections);
        await EnsureApiAliveAsync(client, baseUrl); // Fail sớm nếu API chưa chạy
        await LoginOnceWithRetryAsync(client);      // Warmup JWT / DB connection
        PerfCase.Note($"base={baseUrl}");
        return client;
    }

    /// <summary>
    /// Tạo HttpClient với SocketsHttpHandler tùy chỉnh pool kết nối.
    /// </summary>
    private static HttpClient CreateClient(string baseUrl, int maxConnections)
    {
        var handler = new SocketsHttpHandler
        {
            // Số kết nối TCP tối đa tới 1 host — càng lớn càng "đồng thời" hơn.
            MaxConnectionsPerServer = Math.Max(maxConnections, 256),
            PooledConnectionLifetime = TimeSpan.FromMinutes(5), // Giữ connection tái sử dụng
            ConnectTimeout = TimeSpan.FromSeconds(30)          // Timeout bắt tay TCP
        };
        return new HttpClient(handler)
        {
            BaseAddress = new Uri(baseUrl + "/"), // Mọi path tương đối ghép vào đây
            Timeout = TimeSpan.FromMinutes(3)     // Timeout 1 request (không phải cả burst)
        };
    }

    /// <summary>
    /// Kiểm tra API còn sống bằng GET swagger/index.html (timeout 5s).
    /// </summary>
    private static async Task EnsureApiAliveAsync(HttpClient client, string baseUrl)
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            using var res = await client.GetAsync("swagger/index.html", cts.Token);
            // 5xx = server lỗi nặng → coi như chưa sẵn sàng.
            if ((int)res.StatusCode >= 500)
                throw new InvalidOperationException($"API HTTP {(int)res.StatusCode}");
        }
        catch (Exception ex)
        {
            // Gói lại với hướng dẫn chạy API cho người vận hành test.
            throw new InvalidOperationException(
                $"API chưa chạy tại {baseUrl}. Chạy: cd backend/src/WarehouseManagement.WebAPI && dotnet run --launch-profile http. ({ex.Message})");
        }
    }

    /// <summary>
    /// Warmup: thử login tối đa 3 lần trước khi bắt đầu đo (loại bỏ cold-start).
    /// </summary>
    private static async Task LoginOnceWithRetryAsync(HttpClient client)
    {
        for (var i = 0; i < 3; i++)
        {
            var (ok, _, _) = await TryLoginAsync(client); // Bỏ qua ms và status
            if (ok) return;
            await Task.Delay(300); // Chờ ngắn rồi thử lại
        }
        Assert.Fail("Warmup login thất bại (admin/123456).");
    }

    /// <summary>
    /// Chạy burst: tạo N task login, giới hạn song song bằng SemaphoreSlim.
    /// Nguyên lý đo đồng thời:
    ///   - gate.WaitAsync() trước mỗi request → tối đa maxParallel task đang chạy.
    ///   - Task.WhenAll chờ tất cả xong → wall.ElapsedMilliseconds = thời gian tổng.
    ///   - samples[i] = latency request i; oks[i] = 1 nếu thành công.
    /// </summary>
    /// <param name="client">HttpClient dùng chung (thread-safe cho SendAsync).</param>
    /// <param name="n">Số người / số request = SoNguoiDangNhapDongThoi.</param>
    /// <param name="maxParallel">Số request thực sự chạy chồng lên nhau tối đa.</param>
    /// <param name="retries">Số lần thử lại mỗi request khi lỗi tạm thời.</param>
    private static async Task<BurstResult> RunLoginBurstAsync(
        HttpClient client, int n, int maxParallel, int retries)
    {
        // SemaphoreSlim(initial, max): cổng chỉ cho maxParallel task qua cùng lúc.
        using var gate = new SemaphoreSlim(maxParallel, maxParallel);
        var samples = new long[n]; // Latency từng request (-1 nếu exception)
        var oks = new int[n];      // 1 = OK, 0 = fail (dùng int để Sum() dễ)
        var wall = Stopwatch.StartNew(); // Đồng hồ wall-clock cả burst

        // Tạo n task; Select không chạy ngay — WhenAll mới kích hoạt song song.
        var tasks = Enumerable.Range(0, n).Select(async i =>
        {
            await gate.WaitAsync(); // Xin slot; chờ nếu đã đủ maxParallel
            try
            {
                var (ok, ms, _) = await TryLoginAsync(client, retries);
                samples[i] = ms;      // Lưu latency
                oks[i] = ok ? 1 : 0;  // Lưu kết quả
            }
            catch
            {
                samples[i] = -1; // Đánh dấu mẫu lỗi (loại khỏi percentile)
                oks[i] = 0;
            }
            finally
            {
                gate.Release(); // Trả slot cho task khác
            }
        });

        await Task.WhenAll(tasks); // Chờ mọi người đăng nhập xong
        wall.Stop();

        var okCount = oks.Sum(); // Tổng số thành công
        // Chỉ lấy mẫu latency hợp lệ (≥0), sắp tăng dần để tính percentile.
        var positive = samples.Where(x => x >= 0).OrderBy(x => x).ToArray();
        if (positive.Length == 0)
            // Không đo được latency nào → mọi chỉ số = 0, Fail = n.
            return new BurstResult(n, 0, n, wall.ElapsedMilliseconds, 0, 0, 0, 0, 0);

        var len = positive.Length;
        return new BurstResult(
            n,
            okCount,
            n - okCount,                         // Fail
            wall.ElapsedMilliseconds,            // Wall
            positive[0],                         // Min
            positive[len / 2],                   // P50 (trung vị)
            (long)positive.Average(),            // Avg
            positive[Math.Max(0, (int)(len * 0.95) - 1)], // P95 (gần đúng)
            positive[^1]);                       // Max (phần tử cuối)
    }

    /// <summary>
    /// Gửi 1 lần POST login (có thể retry). Trả về (Ok, Ms, Status).
    /// Ok = HTTP 2xx VÀ body có data.token.
    /// </summary>
    private static async Task<(bool Ok, long Ms, HttpStatusCode Status)> TryLoginAsync(
        HttpClient client, int retries = 0)
    {
        // attempt từ 0 → retries (tổng retries+1 lần thử).
        for (var attempt = 0; attempt <= retries; attempt++)
        {
            var sw = Stopwatch.StartNew(); // Đo latency 1 attempt
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, LoginUrl);
                // Content JSON cố định admin/123456.
                req.Content = new StringContent(LoginJson, Encoding.UTF8, "application/json");
                using var res = await client.SendAsync(req);
                var body = await res.Content.ReadAsStringAsync();
                sw.Stop();

                var token = ExtractToken(body);
                // Thành công nghiệp vụ: status 2xx và có JWT token.
                var ok = (int)res.StatusCode is >= 200 and < 300 && !string.IsNullOrWhiteSpace(token);
                if (ok || attempt == retries)
                    return (ok, sw.ElapsedMilliseconds, res.StatusCode);
                await Task.Delay(50); // Nghỉ ngắn trước retry
            }
            catch
            {
                sw.Stop();
                if (attempt == retries)
                    return (false, sw.ElapsedMilliseconds, 0); // Status 0 = không nhận được HTTP
                await Task.Delay(50);
            }
        }
        return (false, 0, 0); // Không tới được đây trong thực tế
    }

    /// <summary>
    /// Parse JSON response dạng { "data": { "token": "..." } } → lấy token.
    /// Lỗi parse → null (coi như login fail).
    /// </summary>
    private static string? ExtractToken(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            // Đi theo cấu trúc API chuẩn của project.
            if (doc.RootElement.TryGetProperty("data", out var data) &&
                data.TryGetProperty("token", out var token))
                return token.GetString();
        }
        catch
        {
            // Body không phải JSON hợp lệ / thiếu field → bỏ qua.
        }
        return null;
    }
}
