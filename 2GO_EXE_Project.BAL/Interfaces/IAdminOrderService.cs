using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Orders;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IAdminOrderService
{
    Task<OrderListResponse> GetOrdersAsync(
        string? status,
        long? buyerId,
        long? sellerId,
        long? orderCode,
        DateTime? from,
        DateTime? to,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<OrderDetailResponse?> GetByIdAsync(long orderId, CancellationToken cancellationToken = default);

    Task<BasicResponse> UpdateStatusAsync(ClaimsPrincipal adminPrincipal, long orderId, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default);
}
