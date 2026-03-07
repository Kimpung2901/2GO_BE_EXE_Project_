namespace _2GO_EXE_Project.BAL.DTOs.Banks;

public record BankResponse(
    int BankId,
    string Name,
    string Bin,
    string? Code,
    bool IsActive);

public record BankListResponse(int Total, IReadOnlyList<BankResponse> Items);
