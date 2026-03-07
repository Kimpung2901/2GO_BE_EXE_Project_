namespace _2GO_EXE_Project.BAL.DTOs.Wards;

public record WardResponse(
    int WardId,
    int? DistrictId,
    string? Name,
    string? DistrictName = null
);

public record WardListResponse(
    int Total,
    IReadOnlyList<WardResponse> Items
);

public record CreateWardRequest(
    int DistrictId,
    string Name
);

public record UpdateWardRequest(
    int? DistrictId,
    string? Name
);
