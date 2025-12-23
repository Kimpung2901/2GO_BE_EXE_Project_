using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Shipping;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IShippingService
{
    Task<ShippingResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateShippingRequest request, CancellationToken cancellationToken = default);
    Task<ShippingResponse?> GetByOrderAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateStatusAsync(ClaimsPrincipal userPrincipal, long shipId, UpdateShippingStatusRequest request, CancellationToken cancellationToken = default);
}
