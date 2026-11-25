namespace WarehouseManagement.Domain;

/// <summary>
/// Loại kho dược bệnh viện — quyết định điều kiện bảo quản / kiểm soát.
/// </summary>
public enum WarehouseType
{
    /// <summary>Kho thuốc thường (nhiệt độ phòng).</summary>
    General = 0,

    /// <summary>Kho lạnh / chuỗi lạnh (vắc xin, insulin, sinh phẩm) 2–8°C.</summary>
    ColdChain = 1,

    /// <summary>Kho gây nghiện / hướng thần — kiểm soát đặc biệt.</summary>
    Controlled = 2,

    /// <summary>Kho cấp cứu 24/7.</summary>
    Emergency = 3,

    /// <summary>Kho vật tư y tế tiêu hao / vô trùng.</summary>
    MedicalSupply = 4
}
