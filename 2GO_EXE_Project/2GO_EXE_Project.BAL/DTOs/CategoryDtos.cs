using _2GO_EXE_Project.BAL.DTOs.SubCategories;

namespace _2GO_EXE_Project.BAL.DTOs.Categories;

public record CategoryResponse(int CategoryId, string? Name, string? IconUrl, bool IsActive, IReadOnlyList<SubCategoryResponse>? SubCategories = null);
public record CategoryListResponse(int Total, IReadOnlyList<CategoryResponse> Items);
public record CreateCategoryRequest(string Name, string? IconUrl, bool IsActive = true);
public record UpdateCategoryRequest(string? Name, string? IconUrl, bool? IsActive);
public record UpdateCategoryStatusRequest(bool IsActive);
