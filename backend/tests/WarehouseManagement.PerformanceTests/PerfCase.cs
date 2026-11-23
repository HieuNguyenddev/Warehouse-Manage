using System.Diagnostics;                 // Stopwatch — đo thời gian wall-clock của cả test
using System.Globalization;               // CultureInfo.InvariantCulture — format % ổn định (0.0%)
using System.Runtime.CompilerServices;    // CallerMemberName / CallerFilePath — lấy tên test tự động

namespace WarehouseManagement.PerformanceTests;

// PerfCase.cs
// Nguyên lý:
//   - Bọc thân test (body) để: đo thời gian, gom metrics, ghi SUMMARY vào Excel.
//   - AsyncLocal giữ ngữ cảnh theo luồng async hiện tại (không lẫn giữa test khác).
//   - RecordStep ghi 1 dòng Excel cho MỖI nấc tải (SoNguoiDangNhapDongThoi).
//   - Run / RunAsync chỉ là facade lấy tên class/test từ compiler attributes.

/// <summary>
/// Bọc testcase + ghi từng bước (số người đăng nhập đồng thời) vào Excel.
/// </summary>
internal static class PerfCase
{
    // AsyncLocal&lt;T&gt;: giá trị gắn với execution context hiện tại (an toàn với async/await).
    // Metrics: danh sách chuỗi ghi chú trong suốt 1 lần Run/RunAsync.
    private static readonly AsyncLocal<List<string>?> Metrics = new();

    // ClassName: tên file/class test (ví dụ LoginPerformanceTests) — ghi vào cột Class.
    private static readonly AsyncLocal<string?> ClassName = new();

    // TestName: tên method test (ví dụ Scenario_01_Baseline_Login_n50).
    private static readonly AsyncLocal<string?> TestName = new();

    /// <summary>
    /// Thêm một dòng ghi chú vào danh sách metrics của test đang chạy.
    /// Nếu gọi ngoài Run/RunAsync (Metrics.Value == null) thì bỏ qua an toàn.
    /// </summary>
    /// <param name="metric">Chuỗi mô tả (ví dụ "kich_ban=1_baseline; n=50").</param>
    public static void Note(string metric)
    {
        // Lấy list metrics của ngữ cảnh async hiện tại.
        var list = Metrics.Value;
        // Chưa vào Run/RunAsync → không có ngữ cảnh → không ghi.
        if (list is null) return;
        // Thêm ghi chú vào list (sau này join bằng " | " vào cột Metrics của SUMMARY).
        list.Add(metric);
    }

    /// <summary>
    /// Ghi 1 dòng Excel cho một nấc tải (hiển thị số người đăng nhập cùng lúc).
    /// </summary>
    /// <param name="stepName">Tên bước (ví dụ KB1_Baseline_n50) → cột TestCase.</param>
    /// <param name="concurrentUsers">Số người đăng nhập đồng thời → cột SoNguoiDangNhapDongThoi.</param>
    /// <param name="ok">Số request login thành công.</param>
    /// <param name="fail">Số request login thất bại.</param>
    /// <param name="durationMs">Thời gian wall-clock của burst (ms).</param>
    /// <param name="metrics">Chuỗi chi tiết p50/p95/... đưa vào cột Metrics.</param>
    /// <param name="passed">true → Result=PASS; false → Result=FAIL.</param>
    /// <param name="message">Thông báo PASS/FAIL hiển thị trên Excel.</param>
    public static void RecordStep(
        string stepName,
        int concurrentUsers,
        int ok,
        int fail,
        long durationMs,
        string metrics,
        bool passed,
        string message = "")
    {
        // Tỷ lệ thành công dạng "95.0%" (P1 = percent 1 chữ số thập phân).
        // Tránh chia cho 0 khi concurrentUsers = 0 (dòng SUMMARY).
        var rate = concurrentUsers <= 0
            ? "0%"
            : ((double)ok / concurrentUsers).ToString("P1", CultureInfo.InvariantCulture);

        // Đồng thời Note vào list metrics của SUMMARY (để nhìn lại chuỗi bước).
        Note(
            $"SoNguoiDangNhapDongThoi={concurrentUsers}; ok={ok}; fail={fail}; tyLe={rate}; {metrics}");

        // Đưa 1 Row vào hàng đợi Excel (ghi file thật ở cuối collection).
        PerfExcelReport.Add(new PerfExcelReport.Row(
            ClassName.Value ?? "LoginPerformanceTests",                                    // Class
            string.IsNullOrWhiteSpace(stepName) ? (TestName.Value ?? "Step") : stepName, // TestCase
            concurrentUsers,                                                               // SoNguoiDangNhapDongThoi
            passed ? "PASS" : "FAIL",                                                      // Result
            durationMs,                                                                    // DurationMs
            ok,                                                                            // Ok
            fail,                                                                          // Fail
            rate,                                                                          // TyLeThanhCong
            metrics,                                                                       // Metrics
            message,                                                                       // Message
            DateTime.UtcNow));                                                             // FinishedAtUtc
    }

    /// <summary>
    /// Chạy test đồng bộ: đo thời gian + ghi SUMMARY PASS/FAIL.
    /// </summary>
    /// <param name="body">Đoạn code test thực sự.</param>
    /// <param name="testName">Compiler tự điền = tên method gọi (CallerMemberName).</param>
    /// <param name="sourceFile">Compiler tự điền = đường dẫn file .cs (CallerFilePath).</param>
    public static void Run(
        Action body,
        [CallerMemberName] string testName = "",
        [CallerFilePath] string sourceFile = "")
    {
        // Path.GetFileNameWithoutExtension: ".../LoginPerformanceTests.cs" → "LoginPerformanceTests".
        Execute(Path.GetFileNameWithoutExtension(sourceFile), testName, body);
    }

    /// <summary>
    /// Chạy test bất đồng bộ (async) — dùng cho các scenario HTTP.
    /// </summary>
    public static Task RunAsync(
        Func<Task> body,
        [CallerMemberName] string testName = "",
        [CallerFilePath] string sourceFile = "")
        => ExecuteAsync(Path.GetFileNameWithoutExtension(sourceFile), testName, body);

    /// <summary>
    /// Thực thi body đồng bộ: khởi tạo AsyncLocal → chạy → ghi SUMMARY → dọn ngữ cảnh.
    /// </summary>
    private static void Execute(string className, string testName, Action body)
    {
        // List metrics riêng cho lần chạy này.
        var metrics = new List<string>();
        Metrics.Value = metrics;       // Gắn vào AsyncLocal
        ClassName.Value = className;   // Cho RecordStep / SUMMARY dùng
        TestName.Value = testName;
        var sw = Stopwatch.StartNew(); // Bắt đầu đo wall-clock toàn test
        try
        {
            body();                    // Chạy thân test
            sw.Stop();
            // Dòng tổng: ConcurrentUsers=0 vì đã ghi chi tiết từng step bằng RecordStep.
            PerfExcelReport.Add(new PerfExcelReport.Row(
                className,
                testName + " [SUMMARY]", // Hậu tố để phân biệt với dòng step
                0,                       // Không phải một nấc tải cụ thể
                "PASS",
                sw.ElapsedMilliseconds,  // Tổng thời gian cả scenario
                0,
                0,
                "",
                string.Join(" | ", metrics), // Ghép mọi Note/RecordStep
                string.Empty,
                DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            sw.Stop();
            // Test fail: vẫn ghi Excel (Result=FAIL + Message = exception gốc).
            PerfExcelReport.Add(new PerfExcelReport.Row(
                className,
                testName + " [SUMMARY]",
                0,
                "FAIL",
                sw.ElapsedMilliseconds,
                0,
                0,
                "",
                string.Join(" | ", metrics),
                ex.GetBaseException().Message, // Lấy message gốc (bỏ wrapper)
                DateTime.UtcNow));
            throw; // Ném lại để xUnit đánh dấu test FAILED
        }
        finally
        {
            // Luôn xóa ngữ cảnh AsyncLocal để lần chạy sau không lẫn dữ liệu.
            Metrics.Value = null;
            ClassName.Value = null;
            TestName.Value = null;
        }
    }

    /// <summary>
    /// Bản async của Execute — logic giống hệt, chỉ khác await body().
    /// </summary>
    private static async Task ExecuteAsync(string className, string testName, Func<Task> body)
    {
        var metrics = new List<string>();
        Metrics.Value = metrics;
        ClassName.Value = className;
        TestName.Value = testName;
        var sw = Stopwatch.StartNew();
        try
        {
            await body(); // Chờ toàn bộ scenario HTTP hoàn tất
            sw.Stop();
            PerfExcelReport.Add(new PerfExcelReport.Row(
                className,
                testName + " [SUMMARY]",
                0,
                "PASS",
                sw.ElapsedMilliseconds,
                0,
                0,
                "",
                string.Join(" | ", metrics),
                string.Empty,
                DateTime.UtcNow));
        }
        catch (Exception ex)
        {
            sw.Stop();
            PerfExcelReport.Add(new PerfExcelReport.Row(
                className,
                testName + " [SUMMARY]",
                0,
                "FAIL",
                sw.ElapsedMilliseconds,
                0,
                0,
                "",
                string.Join(" | ", metrics),
                ex.GetBaseException().Message,
                DateTime.UtcNow));
            throw;
        }
        finally
        {
            Metrics.Value = null;
            ClassName.Value = null;
            TestName.Value = null;
        }
    }
}
