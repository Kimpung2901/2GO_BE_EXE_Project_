using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Orders;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IOrderService
{
    Task<OrderResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<OrderListResponse> GetMyOrdersAsync(ClaimsPrincipal userPrincipal, int skip, int take, CancellationToken cancellationToken = default);
    Task<OrderDetailResponse?> GetByIdAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default);
    Task<BasicResponse> CancelAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default);
    Task<BasicResponse> ConfirmAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default);
    Task<BasicResponse> CompleteAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default);
}
