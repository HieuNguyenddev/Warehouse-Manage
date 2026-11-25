namespace WarehouseManagement.Domain;

/// <summary>
/// Mục đích xuất thuốc từ kho dược bệnh viện.
/// </summary>
public enum ExportIssuePurpose
{
    /// <summary>Lĩnh thuốc cho khoa/phòng (mặc định).</summary>
    DepartmentIssue = 0,

    /// <summary>Cấp phát cho người bệnh (nhà thuốc BV / đơn thuốc).</summary>
    PatientDispense = 1,

    /// <summary>Điều chuyển nội bộ giữa các kho.</summary>
    InternalTransfer = 2,

    /// <summary>Hủy / tiêu hủy thuốc hết hạn, hỏng, thu hồi.</summary>
    Destruction = 3,

    /// <summary>Trả hàng nhà cung cấp.</summary>
    ReturnToSupplier = 4
}
