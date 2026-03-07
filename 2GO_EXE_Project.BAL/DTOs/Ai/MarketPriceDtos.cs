namespace _2GO_EXE_Project.BAL.DTOs.Ai;

public record MarketPriceInput(
    string ProductKey,
    int? CategoryId,
    string Condition);

public record MarketPriceResult(
    decimal? MarketAvg,
    decimal? MinPrice,
    decimal? MaxPrice,
    int SampleCount,
    string Source,
    string Confidence,
    string? Reason);
