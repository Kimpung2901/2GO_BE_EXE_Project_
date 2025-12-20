namespace _2GO_EXE_Project.BAL.DTOs.Listings;

public record ListingStatsResponse(
    long ListingId,
    int Views,
    int Saves,
    int Inquiries);
