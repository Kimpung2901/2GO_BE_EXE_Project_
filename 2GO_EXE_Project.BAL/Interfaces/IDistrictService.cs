using _2GO_EXE_Project.BAL.DTOs.Districts;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IDistrictService
{
    Task<DistrictListResponse> GetDistrictsAsync(int? cityId, string? search, CancellationToken cancellationToken = default);
    Task<DistrictResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<DistrictResponse> CreateAsync(CreateDistrictRequest request, CancellationToken cancellationToken = default);
    Task<DistrictResponse?> UpdateAsync(int id, UpdateDistrictRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
