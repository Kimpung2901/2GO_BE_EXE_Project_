using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Categories;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.BAL.DTOs.SubCategories;

namespace _2GO_EXE_Project.BAL.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;

    public CategoryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<CategoryListResponse> GetCategoriesAsync(string? search, int skip, int take, bool includeSubCategories = false, bool? subIsActive = null, CancellationToken cancellationToken = default)
    {
        var query = _uow.Categories.Query();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name != null && c.Name.Contains(search));
        }

        if (includeSubCategories)
        {
            query = query.Include(c => c.SubCategories);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.Name)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : take)
            .Select(c => new CategoryResponse(
                c.CategoryId,
                c.Name,
                c.IconUrl,
                c.IsActive,
                includeSubCategories
                    ? c.SubCategories
                        .Where(sc => !subIsActive.HasValue || sc.IsActive == subIsActive.Value)
                        .OrderBy(sc => sc.Name)
                        .Select(sc => new SubCategoryResponse(sc.SubCategoryId, sc.CategoryId ?? 0, sc.Name, sc.IsActive))
                        .ToList()
                    : null))
            .ToListAsync(cancellationToken);

        return new CategoryListResponse(total, items);
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id, bool includeSubCategories = false, bool? subIsActive = null, CancellationToken cancellationToken = default)
    {
        var query = _uow.Categories.Query().Where(c => c.CategoryId == id);
        if (includeSubCategories)
        {
            query = query.Include(c => c.SubCategories);
        }

        var category = await query.FirstOrDefaultAsync(cancellationToken);
        if (category == null) return null;

        IReadOnlyList<SubCategoryResponse>? subs = null;
        if (includeSubCategories)
        {
            subs = category.SubCategories
                .Where(sc => !subIsActive.HasValue || sc.IsActive == subIsActive.Value)
                .OrderBy(sc => sc.Name)
                .Select(sc => new SubCategoryResponse(sc.SubCategoryId, sc.CategoryId ?? 0, sc.Name, sc.IsActive))
                .ToList();
        }

        return new CategoryResponse(category.CategoryId, category.Name, category.IconUrl, category.IsActive, subs);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Category
        {
            Name = request.Name,
            IconUrl = request.IconUrl,
            IsActive = request.IsActive
        };
        await _uow.Categories.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(entity.CategoryId, entity.Name, entity.IconUrl, entity.IsActive);
    }

    public async Task<CategoryResponse?> UpdateAsync(int id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Categories.GetByIdAsync(id);
        if (entity == null) return null;

        entity.Name = request.Name ?? entity.Name;
        entity.IconUrl = request.IconUrl ?? entity.IconUrl;
        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        _uow.Categories.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(entity.CategoryId, entity.Name, entity.IconUrl, entity.IsActive);
    }

    public async Task<CategoryResponse?> UpdateStatusAsync(int id, UpdateCategoryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Categories.GetByIdAsync(id);
        if (entity == null) return null;
        entity.IsActive = request.IsActive;
        _uow.Categories.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(entity.CategoryId, entity.Name, entity.IconUrl, entity.IsActive);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Categories.GetByIdAsync(id);
        if (entity == null) return false;
        // soft delete: set inactive
        entity.IsActive = false;
        _uow.Categories.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
