using _2GO_EXE_Project.BAL.DTOs.Shipping;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IGhnShippingService
{
    Task<string> CreateOrderAsync(CreateGhnShippingRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GhnProvinceResponse>> GetProvincesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GhnDistrictResponse>> GetDistrictsAsync(int provinceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GhnWardResponse>> GetWardsAsync(int districtId, CancellationToken cancellationToken = default);
    Task<GhnFeeResponse> GetFeeAsync(GhnFeeRequest request, CancellationToken cancellationToken = default);
    Task<GhnCancelResponse> CancelAsync(GhnCancelRequest request, CancellationToken cancellationToken = default);
    Task<GhnPrintTokenResponse> GetPrintTokenAsync(GhnPrintTokenRequest request, CancellationToken cancellationToken = default);
    Task<GhnOrderInfoResponse> GetOrderInfoAsync(string orderCode, CancellationToken cancellationToken = default);
}
