namespace _2GO_EXE_Project.BAL.DTOs.SubCategories;

public record SubCategoryResponse(int SubCategoryId, int CategoryId, string? Name, bool IsActive);
public record SubCategoryListResponse(int Total, IReadOnlyList<SubCategoryResponse> Items);
public record CreateSubCategoryRequest(string Name, bool IsActive = true);
public record UpdateSubCategoryRequest(string? Name, bool? IsActive);
public record UpdateSubCategoryStatusRequest(bool IsActive);
