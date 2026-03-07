using _2GO_EXE_Project.BAL.DTOs.Ai;
using _2GO_EXE_Project.BAL.Services;
using Xunit;

namespace _2GO_EXE_Project.Tests;

public class ModerationTests
{
    [Fact]
    public void NewUserCleanContent_DoesNotAutoPending()
    {
        var service = new ModerationService();
        var userInfo = new AiUserRiskInfo(
            AccountAgeDays: 1,
            RecentListingsCount: 0,
            TotalListingsCount: 5,
            CompletedSalesCount: 0,
            ReportsCount: 0,
            DeviceCount: 1,
            PhoneVerified: false,
            EmailVerified: false);

        var result = service.AnalyzeRisk(
            "Bàn học gỗ",
            "Còn mới, không spam.",
            150000,
            null,
            null,
            userInfo);

        Assert.Equal("PUBLISHED", result.Action);
        Assert.DoesNotContain(result.Flags, f => f == "PHONE_NUMBER" || f == "EXTERNAL_LINK" || f == "KEYWORD_SPAM");
    }
}
