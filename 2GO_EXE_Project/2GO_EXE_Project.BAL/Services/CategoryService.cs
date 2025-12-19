using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Categories;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class CategoryService : ICategoryService
{
    private readonly IUnitOfWork _uow;

    public CategoryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<CategoryListResponse> GetCategoriesAsync(string? search, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Categories.Query();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c => c.Name != null && c.Name.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : take)
            .Select(c => new CategoryResponse(c.CategoryId, c.Name, c.IconUrl, c.IsActive, c.SortOrder))
            .ToListAsync(cancellationToken);

        return new CategoryListResponse(total, items);
    }

    public async Task<CategoryResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await _uow.Categories.GetByIdAsync(id);
        if (category == null) return null;
        return new CategoryResponse(category.CategoryId, category.Name, category.IconUrl, category.IsActive, category.SortOrder);
    }

    public async Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = new Category
        {
            Name = request.Name,
            IconUrl = request.IconUrl,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder
        };
        await _uow.Categories.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(entity.CategoryId, entity.Name, entity.IconUrl, entity.IsActive, entity.SortOrder);
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
        if (request.SortOrder.HasValue)
        {
            entity.SortOrder = request.SortOrder.Value;
        }

        _uow.Categories.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return new CategoryResponse(entity.CategoryId, entity.Name, entity.IconUrl, entity.IsActive, entity.SortOrder);
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
