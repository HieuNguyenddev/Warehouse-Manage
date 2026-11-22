// Lớp Application — hợp đồng nghiệp vụ thanh toán cửa hàng (checkout + hóa đơn khách).
using WarehouseManagement.Application.DTOs;
using WarehouseManagement.Shared;

namespace WarehouseManagement.Application.Interfaces;

public interface IStoreCheckoutService
{
    // Khách hàng checkout: trừ tồn, tạo phiếu xuất/hóa đơn theo giỏ hàng.
    Task<StoreCheckoutResponseDto> CheckoutAsync(int userId, StoreCheckoutRequestDto dto);

    // Lấy một hóa đơn của chính khách hàng đang đăng nhập.
    Task<InvoiceResponseDto> GetMyInvoiceAsync(int userId, int invoiceId);

    // Lấy danh sách hóa đơn của khách hàng đang đăng nhập (phân trang).
    Task<PagedResult<InvoiceResponseDto>> GetMyInvoicesAsync(int userId, PaginationQuery query);
}
