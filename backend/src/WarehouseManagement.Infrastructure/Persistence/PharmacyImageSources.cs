using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WarehouseManagement.Infrastructure.Persistence;

/// <summary>
/// Tải ảnh thuốc / vật tư y tế thật (vỉ thuốc, lọ, ống tiêm, gạc…).
/// Ưu tiên danh sách Wikimedia đã chọn lọc; search chỉ nhận title khớp từ khóa y tế,
/// loại ảnh không liên quan (đĩa ăn, người, nghĩa trang, icon…).
/// </summary>
public static class PharmacyImageSources
{
    /// <summary>Từ khóa tìm ảnh theo CategoryIndex.</summary>
    public static readonly string[] CategoryQueries =
    [
        "Antibiotic pills blister",             // 0 Kháng sinh
        "Paracetamol tablets blister",           // 1 Giảm đau hạ sốt
        "Prednisolone tablets medicine",         // 2 Kháng viêm
        "Aspirin tablets blister",               // 3 Tim mạch
        "Omeprazole capsules",                   // 4 Tiêu hóa
        "Albuterol inhaler",                     // 5 Hô hấp
        "Diazepam tablets blister",              // 6 Thần kinh
        "Human insulin vial",                    // 7 Nội tiết
        "Vitamin tablets bottle medicine",       // 8 Vitamin
        "Intravenous saline bag",                // 9 Dịch truyền
        "Propofol ampoule",                      // 10 Gây mê
        "Cetirizine tablets blister",            // 11 Chống dị ứng
        "Hydrocortisone cream tube",             // 12 Da liễu
        "Eye drops bottle medicine",             // 13 Mắt TMH
        "Medicine glass ampoule",                // 14 Sản phụ khoa
        "Chemotherapy vial medicine",            // 15 Ung thư
        "Injection ampoule medicine",            // 16 Thuốc tiêm
        "Blister pack tablets medicine",         // 17 Viên uống
        "Cough syrup bottle",                    // 18 Siro
        "Ointment tube medicine",                // 19 Bôi ngoài da
        "Vaccine vial medicine",                 // 20 Vắc xin
        "Sterile gauze pad medical",             // 21 Vật tư tiêu hao
        "Disposable syringe medical",            // 22 Dụng cụ y tế
        "Blood glucose test strips",             // 23 Hóa chất XN
        "Alprazolam tablets blister",            // 24 Hướng thần
        "Morphine ampoule",                      // 25 Gây nghiện
        "Adrenaline ampoule",                    // 26 Cấp cứu
        "Medical nutrition powder can",          // 27 Dinh dưỡng
        "Acyclovir tablets blister",             // 28 Kháng nấm virus
        "Warfarin tablets blister"               // 29 Kháng đông
    ];

    /// <summary>
    /// URL Wikimedia đã kiểm tra (ảnh thuốc/vật tư thật) — nguồn chính, ổn định.
    /// Index = CategoryIndex.
    /// </summary>
    private static readonly string[] CuratedUrls =
    [
        // 0 kháng sinh
        "https://upload.wikimedia.org/wikipedia/commons/thumb/5/57/Antibiotic_pills.jpg/500px-Antibiotic_pills.jpg",
        // 1 giảm đau
        "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9d/Thiocolchicoside-Aceclofenac-Paracetamol_Tablet_-_Howrah_20170920111151.jpg/500px-Thiocolchicoside-Aceclofenac-Paracetamol_Tablet_-_Howrah_20170920111151.jpg",
        // 2 kháng viêm (dùng vỉ thuốc)
        "https://upload.wikimedia.org/wikipedia/commons/thumb/5/57/Antibiotic_pills.jpg/500px-Antibiotic_pills.jpg",
        // 3 tim mạch — aspirin-like blister from amoxicillin pack photo as tablet blister lookalike fallback replaced by aspirin's verified search later; use amoxicillin pack
        "https://upload.wikimedia.org/wikipedia/commons/thumb/f/ff/%D8%A3%D9%85%D9%88%D9%83%D8%B3%D9%8A%D8%B3%D9%8A%D9%84%D9%8A%D9%86_%D8%A7%D9%86%D8%AA%D8%A7%D8%AC_%D9%85%D8%B5%D9%86%D8%B9_%D8%B3%D8%A7%D9%85%D8%B1%D8%A7%D8%A1_Amoxicillin_SDI_production.jpg/500px-%D8%A3%D9%85%D9%88%D9%83%D8%B3%D9%8A%D8%B3%D9%8A%D9%84%D9%8A%D9%86_%D8%A7%D9%86%D8%AA%D8%A7%D8%AC_%D9%85%D8%B5%D9%86%D8%B9_%D8%B3%D8%A7%D9%85%D8%B1%D8%A7%D8%A1_Amoxicillin_SDI_production.jpg",
        // 4 tiêu hóa — omeprazole-like: use blister
        "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9d/Thiocolchicoside-Aceclofenac-Paracetamol_Tablet_-_Howrah_20170920111151.jpg/500px-Thiocolchicoside-Aceclofenac-Paracetamol_Tablet_-_Howrah_20170920111151.jpg",
        // 5 hô hấp
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/1e/Albuterol_Inhaler.JPG/500px-Albuterol_Inhaler.JPG",
        // 6 thần kinh — tablet blister
        "https://upload.wikimedia.org/wikipedia/commons/thumb/5/57/Antibiotic_pills.jpg/500px-Antibiotic_pills.jpg",
        // 7 nội tiết
        "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4e/Human_insulin_100IU-ml_vial_white_background.jpg/500px-Human_insulin_100IU-ml_vial_white_background.jpg",
        // 8 vitamin — tablet pack
        "https://upload.wikimedia.org/wikipedia/commons/thumb/f/ff/%D8%A3%D9%85%D9%88%D9%83%D8%B3%D9%8A%D8%B3%D9%8A%D9%84%D9%8A%D9%86_%D8%A7%D9%86%D8%AA%D8%A7%D8%AC_%D9%85%D8%B5%D9%86%D8%B9_%D8%B3%D8%A7%D9%85%D8%B1%D8%A7%D8%A1_Amoxicillin_SDI_production.jpg/500px-%D8%A3%D9%85%D9%88%D9%83%D8%B3%D9%8A%D8%B3%D9%8A%D9%84%D9%8A%D9%86_%D8%A7%D9%86%D8%AA%D8%A7%D8%AC_%D9%85%D8%B5%D9%86%D8%B9_%D8%B3%D8%A7%D9%85%D8%B1%D8%A7%D8%A1_Amoxicillin_SDI_production.jpg",
        // 9 dịch truyền — reuse insulin-style vial / bag: use vaccine vial as sterile liquid container look; search may improve
        "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e7/Vaccine_Vial_%28NIH_BioArt_535_-_636843%29.png/500px-Vaccine_Vial_%28NIH_BioArt_535_-_636843%29.png",
        // 10 gây mê
        "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4e/Human_insulin_100IU-ml_vial_white_background.jpg/500px-Human_insulin_100IU-ml_vial_white_background.jpg",
        // 11 chống dị ứng
        "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9d/Thiocolchicoside-Aceclofenac-Paracetamol_Tablet_-_Howrah_20170920111151.jpg/500px-Thiocolchicoside-Aceclofenac-Paracetamol_Tablet_-_Howrah_20170920111151.jpg",
        // 12 da liễu
        "https://upload.wikimedia.org/wikipedia/commons/thumb/5/57/Tube_of_hydrocortisone_cream.jpg/500px-Tube_of_hydrocortisone_cream.jpg",
        // 13 mắt
        "https://upload.wikimedia.org/wikipedia/commons/thumb/d/d8/CVS_Health_Olopatadine_HCI_Eye_Drops.jpg/500px-CVS_Health_Olopatadine_HCI_Eye_Drops.jpg",
        // 14 sản — ampoule-like vial
        "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4e/Human_insulin_100IU-ml_vial_white_background.jpg/500px-Human_insulin_100IU-ml_vial_white_background.jpg",
        // 15 ung thư — vial
        "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e7/Vaccine_Vial_%28NIH_BioArt_535_-_636843%29.png/500px-Vaccine_Vial_%28NIH_BioArt_535_-_636843%29.png",
        // 16 tiêm
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/10/Baxa_syringe.jpg/500px-Baxa_syringe.jpg",
        // 17 viên uống
        "https://upload.wikimedia.org/wikipedia/commons/thumb/5/57/Antibiotic_pills.jpg/500px-Antibiotic_pills.jpg",
        // 18 siro — eye drop bottle shape as liquid medicine bottle; better from search
        "https://upload.wikimedia.org/wikipedia/commons/thumb/c/c6/Ciclosporin_eye_drops_%28in_water%29.jpg/500px-Ciclosporin_eye_drops_%28in_water%29.jpg",
        // 19 bôi
        "https://upload.wikimedia.org/wikipedia/commons/thumb/6/6a/Medpride_Hydrocortisone_Cream1.jpg/500px-Medpride_Hydrocortisone_Cream1.jpg",
        // 20 vắc xin
        "https://upload.wikimedia.org/wikipedia/commons/thumb/e/e7/Vaccine_Vial_%28NIH_BioArt_535_-_636843%29.png/500px-Vaccine_Vial_%28NIH_BioArt_535_-_636843%29.png",
        // 21 vật tư — cream box packaging as medical consumable packaging; syringe as fallback for tools elsewhere
        "https://upload.wikimedia.org/wikipedia/commons/thumb/6/6a/Medpride_Hydrocortisone_Cream1.jpg/500px-Medpride_Hydrocortisone_Cream1.jpg",
        // 22 dụng cụ
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/14/Syringe_disposable_%22Luer%22_circuit_device.jpg/500px-Syringe_disposable_%22Luer%22_circuit_device.jpg",
        // 23 XN
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/10/Baxa_syringe.jpg/500px-Baxa_syringe.jpg",
        // 24 hướng thần
        "https://upload.wikimedia.org/wikipedia/commons/thumb/9/9d/Thiocolchicoside-Aceclofenac-Paracetamol_Tablet_-_Howrah_20170920111151.jpg/500px-Thiocolchicoside-Aceclofenac-Paracetamol_Tablet_-_Howrah_20170920111151.jpg",
        // 25 gây nghiện — vial/ampoule
        "https://upload.wikimedia.org/wikipedia/commons/thumb/4/4e/Human_insulin_100IU-ml_vial_white_background.jpg/500px-Human_insulin_100IU-ml_vial_white_background.jpg",
        // 26 cấp cứu
        "https://upload.wikimedia.org/wikipedia/commons/thumb/1/10/Baxa_syringe.jpg/500px-Baxa_syringe.jpg",
        // 27 dinh dưỡng
        "https://upload.wikimedia.org/wikipedia/commons/thumb/6/6a/Medpride_Hydrocortisone_Cream1.jpg/500px-Medpride_Hydrocortisone_Cream1.jpg",
        // 28 kháng virus
        "https://upload.wikimedia.org/wikipedia/commons/thumb/5/57/Antibiotic_pills.jpg/500px-Antibiotic_pills.jpg",
        // 29 kháng đông
        "https://upload.wikimedia.org/wikipedia/commons/thumb/f/ff/%D8%A3%D9%85%D9%88%D9%83%D8%B3%D9%8A%D8%B3%D9%8A%D9%84%D9%8A%D9%86_%D8%A7%D9%86%D8%AA%D8%A7%D8%AC_%D9%85%D8%B5%D9%86%D8%B9_%D8%B3%D8%A7%D9%85%D8%B1%D8%A7%D8%A1_Amoxicillin_SDI_production.jpg/500px-%D8%A3%D9%85%D9%88%D9%83%D8%B3%D9%8A%D8%B3%D9%8A%D9%84%D9%8A%D9%86_%D8%A7%D9%86%D8%AA%D8%A7%D8%AC_%D9%85%D8%B5%D9%86%D8%B9_%D8%B3%D8%A7%D9%85%D8%B1%D8%A7%D8%A1_Amoxicillin_SDI_production.jpg"
    ];

    private static readonly Regex TitleDeny = new(
        @"plate|fork|knife|food|meal|dinner|person|people|woman|man|child|face|dog|cat|car|logo|\.svg|icon|map|chart|diagram|screenshot|cemetery|crosses|film|movie|actor|portrait|selfie|banquet|restaurant|euro|coin|money|vector|flat icon|hariadhi|collage|meme",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex TitleAllow = new(
        @"pill|tablet|capsule|blister|syringe|vial|ampoule|ampule|gauze|bandage|inhaler|vaccine|saline|infusion|drop|cream|ointment|medicine|drug|pharma|medical|needle|catheter|mask|glove|paracetamol|amoxicillin|aspirin|insulin|antibiotic|metformin|omeprazole|iodine|glucose|strip|syrup|vitamin|epinephrine|morphine|dressing|ibuprofen|cetirizine|warfarin|acyclovir|prednisolon|diazepam|tramadol|heparin|albuterol|salbutamol|propofol|gauze|compress|ointment|cream|ampoule",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HttpClient Http = CreateClient();

    private static HttpClient CreateClient()
    {
        var c = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        c.DefaultRequestHeaders.UserAgent.ParseAdd(
            "WarehouseManagePharmacySeed/1.1 (hospital pharmacy thesis; medicine product photos only)");
        c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return c;
    }

    /// <summary>
    /// Tải / ghi đè template pharma-cat-XX.jpg (stamp v2 = chỉ ảnh thuốc/vật tư).
    /// </summary>
    public static async Task EnsureCategoryTemplatesAsync(string templateDir, CancellationToken ct = default)
    {
        Directory.CreateDirectory(templateDir);
        var stamp = Path.Combine(templateDir, ".pharma-images-v2");
        var allPresent = Enumerable.Range(0, CategoryQueries.Length).All(i =>
        {
            var p = Path.Combine(templateDir, $"pharma-cat-{i:D2}.jpg");
            return File.Exists(p) && new FileInfo(p).Length >= 3000;
        });

        if (allPresent && File.Exists(stamp))
            return;

        // Xóa stamp cũ v1 để buộc làm sạch
        var oldStamp = Path.Combine(templateDir, ".pharma-images-v1");
        if (File.Exists(oldStamp))
            File.Delete(oldStamp);

        for (var i = 0; i < CategoryQueries.Length; i++)
        {
            ct.ThrowIfCancellationRequested();
            var path = Path.Combine(templateDir, $"pharma-cat-{i:D2}.jpg");

            // Luôn ưu tiên curated (ảnh thuốc/vật tư đã kiểm)
            byte[]? bytes = null;
            try
            {
                bytes = await DownloadBytesAsync(CuratedUrls[i % CuratedUrls.Length], ct);
            }
            catch { /* try search */ }

            if (bytes is null || bytes.Length < 3000)
                bytes = await TrySearchMedicineImageAsync(CategoryQueries[i], ct);

            if (bytes is { Length: >= 3000 })
                await File.WriteAllBytesAsync(path, bytes, ct);
        }

        // Bù thiếu: chỉ lấy từ các file curated đã tải thành công (không copy ảnh bẩn)
        var goodDonors = Enumerable.Range(0, CategoryQueries.Length)
            .Select(i => Path.Combine(templateDir, $"pharma-cat-{i:D2}.jpg"))
            .Where(p => File.Exists(p) && new FileInfo(p).Length >= 3000)
            .ToList();

        for (var i = 0; i < CategoryQueries.Length; i++)
        {
            var path = Path.Combine(templateDir, $"pharma-cat-{i:D2}.jpg");
            if (File.Exists(path) && new FileInfo(path).Length >= 3000)
                continue;
            if (goodDonors.Count == 0) break;
            File.Copy(goodDonors[i % goodDonors.Count], path, overwrite: true);
        }

        var complete = Enumerable.Range(0, CategoryQueries.Length).All(i =>
        {
            var p = Path.Combine(templateDir, $"pharma-cat-{i:D2}.jpg");
            return File.Exists(p) && new FileInfo(p).Length >= 3000;
        });
        if (complete)
            await File.WriteAllTextAsync(stamp, DateTime.UtcNow.ToString("O"), ct);
    }

    public static string TemplatePathForCategory(string templateDir, int categoryIndex)
    {
        var n = CategoryQueries.Length;
        var idx = ((categoryIndex % n) + n) % n;
        return Path.Combine(templateDir, $"pharma-cat-{idx:D2}.jpg");
    }

    private static async Task<byte[]?> TrySearchMedicineImageAsync(string query, CancellationToken ct)
    {
        try
        {
            var api =
                "https://commons.wikimedia.org/w/api.php" +
                "?action=query&format=json&origin=*" +
                "&generator=search&gsrnamespace=6" +
                $"&gsrsearch={Uri.EscapeDataString(query)}" +
                "&gsrlimit=12&prop=imageinfo&iiprop=url|mime|size&iiurlwidth=480";

            using var resp = await Http.GetAsync(api, ct);
            if (!resp.IsSuccessStatusCode) return null;

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            if (!doc.RootElement.TryGetProperty("query", out var q) ||
                !q.TryGetProperty("pages", out var pages))
                return null;

            foreach (var page in pages.EnumerateObject())
            {
                var title = page.Value.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                if (TitleDeny.IsMatch(title)) continue;
                if (!TitleAllow.IsMatch(title)) continue;

                if (!page.Value.TryGetProperty("imageinfo", out var infos)) continue;
                foreach (var info in infos.EnumerateArray())
                {
                    var mime = info.TryGetProperty("mime", out var m) ? m.GetString() : null;
                    if (mime is not null &&
                        (mime.Contains("svg", StringComparison.OrdinalIgnoreCase) ||
                         mime.Contains("pdf", StringComparison.OrdinalIgnoreCase) ||
                         mime.Contains("djvu", StringComparison.OrdinalIgnoreCase)))
                        continue;
                    if (mime is not null &&
                        !mime.Contains("jpeg", StringComparison.OrdinalIgnoreCase) &&
                        !mime.Contains("jpg", StringComparison.OrdinalIgnoreCase) &&
                        !mime.Contains("png", StringComparison.OrdinalIgnoreCase) &&
                        !mime.Contains("webp", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var url = info.TryGetProperty("thumburl", out var tu) ? tu.GetString()
                        : info.TryGetProperty("url", out var u) ? u.GetString()
                        : null;
                    if (string.IsNullOrWhiteSpace(url)) continue;

                    var data = await DownloadBytesAsync(url.Split('?', 2)[0], ct);
                    if (data is { Length: >= 3000 })
                        return data;
                }
            }
        }
        catch
        {
            // ignore
        }

        return null;
    }

    private static async Task<byte[]?> DownloadBytesAsync(string url, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.UserAgent.ParseAdd(
            "WarehouseManagePharmacySeed/1.1 (hospital pharmacy thesis)");
        using var resp = await Http.SendAsync(req, ct);
        if (!resp.IsSuccessStatusCode)
            return null;
        return await resp.Content.ReadAsByteArrayAsync(ct);
    }
}
