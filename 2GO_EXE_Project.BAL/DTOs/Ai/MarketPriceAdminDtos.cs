namespace _2GO_EXE_Project.BAL.DTOs.Ai;

public record MarketPriceItem(
    int MarketPriceId,
    string ProductKey,
    int? CategoryId,
    string Condition,
    decimal AvgPrice,
    decimal MinPrice,
    decimal MaxPrice,
    int SampleCount,
    string? Source,
    string? Confidence,
    DateTime? UpdatedAt);

public record MarketPriceListResponse(int Total, IReadOnlyList<MarketPriceItem> Items);