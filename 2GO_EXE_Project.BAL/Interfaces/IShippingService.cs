using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Shipping;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IShippingService
{
    Task<ShippingResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateShippingRequest request, CancellationToken cancellationToken = default);
    Task<ShippingResponse> CreateGhnAsync(ClaimsPrincipal userPrincipal, CreateGhnShippingRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GhnProvinceResponse>> GetGhnProvincesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GhnDistrictResponse>> GetGhnDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GhnWardResponse>> GetGhnWardsAsync(int districtId, CancellationToken cancellationToken = default);
    Task<GhnFeeResponse> GetGhnFeeAsync(GhnFeeRequest request, CancellationToken cancellationToken = default);
    Task<GhnCancelResponse> CancelGhnAsync(ClaimsPrincipal userPrincipal, GhnCancelRequest request, CancellationToken cancellationToken = default);
    Task<GhnPrintTokenResponse> GetGhnPrintTokenAsync(ClaimsPrincipal userPrincipal, GhnPrintTokenRequest request, CancellationToken cancellationToken = default);
    Task<GhnOrderInfoResponse> GetGhnOrderInfoAsync(ClaimsPrincipal userPrincipal, string orderCode, CancellationToken cancellationToken = default);
    Task<BasicResponse> HandleGhnWebhookAsync(GhnWebhookPayload payload, string? webhookToken, CancellationToken cancellationToken = default);
    Task<ShippingResponse?> GetByOrderAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateStatusAsync(ClaimsPrincipal userPrincipal, long shipId, UpdateShippingStatusRequest request, CancellationToken cancellationToken = default);
}
