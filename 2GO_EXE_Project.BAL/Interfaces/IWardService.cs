using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _2GO_EXE_Project.BAL.DTOs.Wards;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IWardService
{
    Task<WardListResponse> GetWardsAsync(int? districtId, string? search, CancellationToken cancellationToken = default);
    Task<WardResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WardResponse> CreateAsync(CreateWardRequest request, CancellationToken cancellationToken = default);
    Task<WardResponse?> UpdateAsync(int id, UpdateWardRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default); 
}
