using _2GO_EXE_Project.BAL.DTOs.Ai;
namespace _2GO_EXE_Project.BAL.Interfaces;
public interface IUserRiskInfoService
{
    Task<AiUserRiskInfo> BuildUserRiskInfoAsync(string userId, CancellationToken cancellationToken = default);
}