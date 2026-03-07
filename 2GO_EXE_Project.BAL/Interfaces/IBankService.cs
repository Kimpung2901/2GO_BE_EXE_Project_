using _2GO_EXE_Project.BAL.DTOs.Banks;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface IBankService
{
    Task<BankListResponse> GetAllAsync(bool? isActive, int skip, int take, CancellationToken cancellationToken = default);
}
