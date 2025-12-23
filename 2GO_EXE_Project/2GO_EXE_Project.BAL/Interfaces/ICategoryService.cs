using _2GO_EXE_Project.BAL.DTOs.Categories;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ICategoryService
{
    Task<CategoryListResponse> GetCategoriesAsync(string? search, int skip, int take, bool includeSubCategories = false, bool? subIsActive = null, CancellationToken cancellationToken = default);
    Task<CategoryResponse?> GetByIdAsync(int id, bool includeSubCategories = false, bool? subIsActive = null, CancellationToken cancellationToken = default);
    Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryResponse?> UpdateStatusAsync(int id, UpdateCategoryStatusRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
