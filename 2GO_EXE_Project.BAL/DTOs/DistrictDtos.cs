namespace _2GO_EXE_Project.BAL.DTOs.Districts;

public record DistrictResponse(
    int DistrictId,
    int? CityId,
    string? Name,
    string? CityName = null
);

public record DistrictListResponse(
    int Total,
    IReadOnlyList<DistrictResponse> Items
);

public record CreateDistrictRequest(
    int CityId,
    string Name
);

public record UpdateDistrictRequest(
    int? CityId,
    string? Name
);
