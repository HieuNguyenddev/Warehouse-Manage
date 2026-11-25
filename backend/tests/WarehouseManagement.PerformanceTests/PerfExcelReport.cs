using System.Collections.Concurrent; // ConcurrentQueue — hàng đợi thread-safe khi nhiều test Add()
using System.Globalization;          // CultureInfo — format số/ngày ổn định, không phụ thuộc locale máy
using System.IO.Compression;         // ZipArchive — .xlsx thực chất là ZIP chứa XML
using System.Text;                   // UTF8Encoding — ghi XML không BOM
using System.Xml.Linq;               // XDocument/XElement — dựng XML Open XML Spreadsheet

namespace WarehouseManagement.PerformanceTests;

// PerfExcelReport.cs
// Nguyên lý xuất Excel không dùng thư viện bên thứ 3:
//   - Office Open XML: file .xlsx = ZIP gồm [Content_Types].xml, _rels, xl/workbook.xml,
//     xl/worksheets/sheet1.xml, ...
//   - Mỗi ô dùng inlineStr (chuỗi nhúng trực tiếp) để đơn giản, không cần sharedStrings.
//   - Rows thu thập trong ConcurrentQueue; WriteIfNeeded chỉ ghi 1 lần (Interlocked + lock).

/// <summary>
/// Thu thập kết quả và ghi Excel (.xlsx) sau khi chạy xong.
/// </summary>
internal static class PerfExcelReport
{
    // Hàng đợi chứa mọi dòng kết quả (step + SUMMARY). Thread-safe.
    private static readonly ConcurrentQueue<Row> Rows = new();

    // Khóa đồng bộ khi ghi file — tránh 2 luồng ghi cùng lúc.
    private static readonly object WriteLock = new();

    // Cờ 0/1: đã ghi Excel chưa? Interlocked.Exchange đổi atomic.
    private static int _written;

    /// <summary>
    /// </summary>
    /// <param name="ClassName">Tên class test (cột Class).</param>
    /// <param name="TestCase">Tên bước / scenario (cột TestCase).</param>
    /// <param name="ConcurrentUsers">Số người đăng nhập đồng thời (cột SoNguoiDangNhapDongThoi).</param>
    /// <param name="Result">"PASS" hoặc "FAIL".</param>
    /// <param name="DurationMs">Thời gian đo (ms).</param>
    /// <param name="Ok">Số login thành công.</param>
    /// <param name="Fail">Số login thất bại.</param>
    /// <param name="SuccessRate">Tỷ lệ thành công dạng chuỗi (ví dụ "95.0%").</param>
    /// <param name="Metrics">Chuỗi chi tiết latency / ghi chú.</param>
    /// <param name="Message">Thông báo PASS/FAIL hoặc exception.</param>
    /// <param name="FinishedAtUtc">Thời điểm ghi dòng (UTC).</param>
    public sealed record Row(
        string ClassName,
        string TestCase,
        int ConcurrentUsers,
        string Result,
        long DurationMs,
        int Ok,
        int Fail,
        string SuccessRate,
        string Metrics,
        string Message,
        DateTime FinishedAtUtc);

    /// <summary>Đưa một dòng vào hàng đợi (chưa ghi file).</summary>
    public static void Add(Row row) => Rows.Enqueue(row);

    /// <summary>Đường dẫn file Latest đã ghi gần nhất (null nếu chưa ghi).</summary>
    public static string? LastOutputPath { get; private set; }

    /// <summary>
    /// Ghi Excel một lần duy nhất: PerformanceTests_{timestamp}.xlsx + copy Latest.
    /// Gọi từ PerfReportFixture.Dispose / ProcessExit.
    /// </summary>
    /// <returns>Đường dẫn file Latest.</returns>
    public static string WriteIfNeeded()
    {
        lock (WriteLock) // Chỉ một luồng vào khối ghi
        {
            // Interlocked.Exchange: đặt _written = 1, trả về giá trị cũ.
            // Nếu cũ đã = 1 và đã có path → bỏ qua (đã ghi rồi).
            if (Interlocked.Exchange(ref _written, 1) == 1 && LastOutputPath is not null)
                return LastOutputPath;

            // Lấy toàn bộ hàng đợi, sắp xếp để Excel dễ đọc.
            var list = Rows
                .OrderBy(r => r.ClassName)         // Theo class
                .ThenBy(r => r.TestCase)           // Theo tên bước
                .ThenBy(r => r.ConcurrentUsers)    // Theo số người đồng thời
                .ToList();

            // Thư mục TestResults cạnh project test.
            var outDir = ResolveOutputDir();
            Directory.CreateDirectory(outDir); // Tạo nếu chưa có

            // Tên file có timestamp + bản "Latest" luôn ghi đè.
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
            var stamped = Path.Combine(outDir, $"PerformanceTests_{stamp}.xlsx");
            var latest = Path.Combine(outDir, "PerformanceTests_Latest.xlsx");

            WriteXlsx(stamped, list);                 // Ghi bản có timestamp
            File.Copy(stamped, latest, overwrite: true); // Copy thành Latest

            LastOutputPath = latest;
            // In ra console để biết đường dẫn sau dotnet test.
            Console.WriteLine();
            Console.WriteLine($"==> Excel test report: {latest}");
            Console.WriteLine($"    (bản có timestamp: {stamped})");
            Console.WriteLine(
                $"    rows={list.Count}, passed={list.Count(r => r.Result == "PASS")}, failed={list.Count(r => r.Result == "FAIL")}");
            return latest;
        }
    }

    /// <summary>
    /// Từ thư mục bin/.../netX.Y/ đi lên 3 cấp → thư mục project → TestResults.
    /// </summary>
    private static string ResolveOutputDir()
    {
        // AppContext.BaseDirectory = thư mục chứa DLL đang chạy (bin/Debug/netX.Y/).
        var baseDir = AppContext.BaseDirectory;
        // ../ = project root của PerformanceTests; gắn thêm "TestResults".
        return Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "TestResults"));
    }

    /// <summary>
    /// Dựng file .xlsx thủ công theo chuẩn Open XML (ZIP + XML).
    /// </summary>
    /// <param name="path">Đường dẫn file đích.</param>
    /// <param name="rows">Danh sách dòng dữ liệu.</param>
    private static void WriteXlsx(string path, IReadOnlyList<Row> rows)
    {
        // Namespace chuẩn SpreadsheetML / Relationships / Content Types.
        var ns = XNamespace.Get("http://schemas.openxmlformats.org/spreadsheetml/2006/main");
        var relNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
        var contentTypesNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/content-types");
        var docRelNs = XNamespace.Get("http://schemas.openxmlformats.org/officeDocument/2006/relationships");

        // Tiêu đề cột trên hàng 1 của sheet.
        var headers = new[]
        {
            "STT",                      // Số thứ tự
            "Class",                    // Class test
            "TestCase",                 // Tên bước / scenario
            "SoNguoiDangNhapDongThoi",  // Số người đăng nhập cùng lúc
            "Result",                   // PASS / FAIL
            "DurationMs",               // Thời gian (ms)
            "Ok",                       // Số thành công
            "Fail",                     // Số thất bại
            "TyLeThanhCong",            // Tỷ lệ %
            "Metrics",                  // Chi tiết latency
            "Message",                  // Thông báo
            "FinishedAtUtc"             // Thời điểm UTC
        };

        /// <summary>
        /// Đổi index cột 0-based → tên cột Excel: 0→A, 1→B, ..., 25→Z, 26→AA, ...
        /// </summary>
        string ColName(int index)
        {
            var n = index + 1; // Excel dùng 1-based
            var s = "";
            while (n > 0)
            {
                var m = (n - 1) % 26;           // Chữ cái hiện tại
                s = (char)('A' + m) + s;        // Ghép phía trước (vì chia dần)
                n = (n - 1) / 26;               // Chuyển sang "hàng" chữ cái tiếp
            }
            return s;
        }

        /// <summary>
        /// Tạo 1 ô Excel kiểu inline string tại vị trí col+row.
        /// </summary>
        /// <param name="col">Tên cột (A, B, ...).</param>
        /// <param name="row">Số hàng (1 = header).</param>
        /// <param name="value">Nội dung text (null → rỗng).</param>
        XElement Cell(string col, int row, string? value)
        {
            var text = value ?? string.Empty;
            // Phần tử <t> chứa text; xml:space=preserve giữ khoảng trắng.
            var t = new XElement(ns + "t", text);
            t.SetAttributeValue(XNamespace.Xml + "space", "preserve");
            // <c r="A1" t="inlineStr"><is><t>...</t></is></c>
            return new XElement(ns + "c",
                new XAttribute("r", $"{col}{row}"),
                new XAttribute("t", "inlineStr"),
                new XElement(ns + "is", t));
        }

        // Danh sách phần tử <row> trong sheetData.
        var sheetRows = new List<XElement>();

        // Hàng 1: header.
        sheetRows.Add(new XElement(ns + "row", new XAttribute("r", 1),
            headers.Select((h, i) => Cell(ColName(i), 1, h)).ToArray()));

        // Các hàng dữ liệu bắt đầu từ hàng 2.
        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];           // Row nguồn
            var excelRow = i + 2;      // Hàng Excel (header chiếm hàng 1)
            // Map từng field → chuỗi hiển thị trên cột tương ứng.
            var values = new[]
            {
                (i + 1).ToString(CultureInfo.InvariantCulture),                          // STT
                r.ClassName,                                                             // Class
                r.TestCase,                                                              // TestCase
                r.ConcurrentUsers.ToString(CultureInfo.InvariantCulture),                // SoNguoiDangNhapDongThoi
                r.Result,                                                                // Result
                r.DurationMs.ToString(CultureInfo.InvariantCulture),                     // DurationMs
                r.Ok.ToString(CultureInfo.InvariantCulture),                             // Ok
                r.Fail.ToString(CultureInfo.InvariantCulture),                           // Fail
                r.SuccessRate,                                                           // TyLeThanhCong
                r.Metrics,                                                               // Metrics
                r.Message,                                                               // Message
                r.FinishedAtUtc.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) // FinishedAtUtc
            };
            sheetRows.Add(new XElement(ns + "row", new XAttribute("r", excelRow),
                values.Select((v, idx) => Cell(ColName(idx), excelRow, v)).ToArray()));
        }

        // xl/worksheets/sheet1.xml — dữ liệu bảng.
        var worksheet = new XDocument(
            new XElement(ns + "worksheet",
                new XElement(ns + "sheetData", sheetRows)));

        // xl/workbook.xml — khai báo sheet "LoginPerf" trỏ rId1.
        var workbook = new XDocument(
            new XElement(ns + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", docRelNs),
                new XElement(ns + "sheets",
                    new XElement(ns + "sheet",
                        new XAttribute("name", "LoginPerf"),
                        new XAttribute("sheetId", "1"),
                        new XAttribute(docRelNs + "id", "rId1")))));

        // _rels/.rels — package relationship: root → workbook.
        var rootRels = new XDocument(
            new XElement(relNs + "Relationships",
                new XElement(relNs + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "xl/workbook.xml"))));

        // xl/_rels/workbook.xml.rels — workbook → worksheet.
        var wbRels = new XDocument(
            new XElement(relNs + "Relationships",
                new XElement(relNs + "Relationship",
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                    new XAttribute("Target", "worksheets/sheet1.xml"))));

        // [Content_Types].xml — MIME type từng phần trong package.
        var contentTypes = new XDocument(
            new XElement(contentTypesNs + "Types",
                new XElement(contentTypesNs + "Default",
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(contentTypesNs + "Default",
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                new XElement(contentTypesNs + "Override",
                    new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(contentTypesNs + "Override",
                    new XAttribute("PartName", "/xl/worksheets/sheet1.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"))));

        // Xóa file cũ nếu tồn tại rồi tạo mới.
        if (File.Exists(path))
            File.Delete(path);

        // Tạo ZIP (= .xlsx) và ghi lần lượt các phần XML bắt buộc.
        using var fs = File.Create(path);
        using var zip = new ZipArchive(fs, ZipArchiveMode.Create);
        WriteEntry(zip, "[Content_Types].xml", contentTypes);
        WriteEntry(zip, "_rels/.rels", rootRels);
        WriteEntry(zip, "xl/workbook.xml", workbook);
        WriteEntry(zip, "xl/_rels/workbook.xml.rels", wbRels);
        WriteEntry(zip, "xl/worksheets/sheet1.xml", worksheet);
    }

    /// <summary>
    /// Ghi một entry XML vào ZIP với UTF-8 không BOM, không indent (nhỏ gọn).
    /// </summary>
    /// <param name="zip">ZipArchive đang mở ở chế độ Create.</param>
    /// <param name="entryName">Đường dẫn trong package (ví dụ xl/workbook.xml).</param>
    /// <param name="doc">Nội dung XML.</param>
    private static void WriteEntry(ZipArchive zip, string entryName, XDocument doc)
    {
        var entry = zip.CreateEntry(entryName, CompressionLevel.Optimal); // Nén tối ưu
        using var stream = entry.Open();
        // encoderShouldEmitUTF8Identifier: false → không ghi BOM (Excel thích vậy).
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        doc.Save(writer, SaveOptions.DisableFormatting); // Không xuống dòng/indent
    }
}
