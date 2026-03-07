using _2GO_EXE_Project.BAL.DTOs.SubCategories;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ISubCategoryService
{
    Task<SubCategoryListResponse> GetAllAsync(bool? isActive, int? categoryId, int skip, int take, CancellationToken cancellationToken = default);
    Task<SubCategoryListResponse> GetByCategoryAsync(int categoryId, bool? isActive, int skip, int take, CancellationToken cancellationToken = default);
    Task<SubCategoryResponse?> GetByIdAsync(int id, bool onlyActive, CancellationToken cancellationToken = default);
    Task<SubCategoryResponse> CreateAsync(int categoryId, CreateSubCategoryRequest request, CancellationToken cancellationToken = default);
    Task<SubCategoryResponse?> UpdateAsync(int id, UpdateSubCategoryRequest request, CancellationToken cancellationToken = default);
    Task<SubCategoryResponse?> UpdateStatusAsync(int id, UpdateSubCategoryStatusRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default); // soft delete -> IsActive=false
}
