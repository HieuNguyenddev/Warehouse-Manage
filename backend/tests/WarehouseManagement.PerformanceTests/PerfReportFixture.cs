namespace WarehouseManagement.PerformanceTests;

// PerfReportFixture.cs
// Nguyên lý vòng đời báo cáo Excel:
//   1) Các test gắn [Collection("PerfReport")] dùng chung 1 fixture.
//   2) DisableParallelization = true → test chạy tuần tự (tránh đua ghi Excel / đua HTTP).
//   3) Khi collection kết thúc, xUnit gọi Dispose() → ghi file .xlsx một lần.
//   4) ProcessExit là lưới an toàn nếu runner không Dispose fixture.

/// <summary>
/// Fixture collection: khi toàn bộ test trong collection xong → ghi Excel.
/// </summary>
public sealed class PerfReportFixture : IDisposable
{
    /// <summary>
    /// Static constructor chạy 1 lần khi type được load lần đầu.
    /// Đăng ký sự kiện ProcessExit để vẫn ghi Excel nếu host thoát bất thường.
    /// </summary>
    static PerfReportFixture()
    {
        // Fallback nếu runner không dispose fixture (một số host / crash sớm).
        // (_, _) = bỏ qua sender và EventArgs vì không cần dùng.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            try
            {
                // Ghi Excel nếu chưa ghi (WriteIfNeeded có cờ chống ghi 2 lần).
                PerfExcelReport.WriteIfNeeded();
            }
            catch
            {
                // Nuốt lỗi khi process đang thoát — không làm crash thêm.
            }
        };
    }

    /// <summary>
    /// xUnit gọi Dispose khi collection "PerfReport" kết thúc (sau test cuối).
    /// Đây là đường ghi Excel chính thức.
    /// </summary>
    public void Dispose() => PerfExcelReport.WriteIfNeeded();
}

/// <summary>
/// Định nghĩa collection "PerfReport":
/// - ICollectionFixture&lt;PerfReportFixture&gt;: mọi class gắn [Collection("PerfReport")]
///   dùng chung một instance PerfReportFixture.
/// - DisableParallelization = true: các test trong collection không chạy song song
///   (quan trọng với burst HTTP + ghi hàng Excel tuần tự).
/// </summary>
[CollectionDefinition("PerfReport", DisableParallelization = true)]
public sealed class PerfReportCollection : ICollectionFixture<PerfReportFixture>;
