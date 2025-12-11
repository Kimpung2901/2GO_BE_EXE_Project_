using _2GO_EXE_Project.DAL.Entities;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ITokenService
{
    (string AccessToken, DateTime ExpiresAt) GenerateAccessToken(User user);
    string GenerateRefreshToken();
}
