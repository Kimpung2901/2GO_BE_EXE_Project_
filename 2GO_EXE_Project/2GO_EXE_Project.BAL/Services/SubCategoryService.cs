using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.SubCategories;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class SubCategoryService : ISubCategoryService
{
    private readonly IUnitOfWork _uow;

    public SubCategoryService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<SubCategoryListResponse> GetAllAsync(bool? isActive, int? categoryId, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.SubCategories.Query().AsQueryable();
        if (categoryId.HasValue)
        {
            query = query.Where(sc => sc.CategoryId == categoryId.Value);
        }
        if (isActive.HasValue)
        {
            query = query.Where(sc => sc.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(sc => sc.Name)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : take)
            .Select(sc => new SubCategoryResponse(sc.SubCategoryId, sc.CategoryId ?? 0, sc.Name, sc.IsActive))
            .ToListAsync(cancellationToken);

        return new SubCategoryListResponse(total, items);
    }

    public async Task<SubCategoryListResponse> GetByCategoryAsync(int categoryId, bool? isActive, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.SubCategories.Query().Where(sc => sc.CategoryId == categoryId);
        if (isActive.HasValue)
        {
            query = query.Where(sc => sc.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(sc => sc.Name)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 20 : take)
            .Select(sc => new SubCategoryResponse(sc.SubCategoryId, sc.CategoryId ?? 0, sc.Name, sc.IsActive))
            .ToListAsync(cancellationToken);

        return new SubCategoryListResponse(total, items);
    }

    public async Task<SubCategoryResponse?> GetByIdAsync(int id, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var sub = await _uow.SubCategories.Query()
            .FirstOrDefaultAsync(sc => sc.SubCategoryId == id && (!onlyActive || sc.IsActive), cancellationToken);
        if (sub == null) return null;
        return new SubCategoryResponse(sub.SubCategoryId, sub.CategoryId ?? 0, sub.Name, sub.IsActive);
    }

    public async Task<SubCategoryResponse> CreateAsync(int categoryId, CreateSubCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var categoryExists = await _uow.Categories.Query().AnyAsync(c => c.CategoryId == categoryId, cancellationToken);
        if (!categoryExists)
        {
            throw new InvalidOperationException("Category not found.");
        }

        var entity = new SubCategory
        {
            CategoryId = categoryId,
            Name = request.Name,
            IsActive = request.IsActive
        };
        await _uow.SubCategories.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return new SubCategoryResponse(entity.SubCategoryId, entity.CategoryId ?? 0, entity.Name, entity.IsActive);
    }

    public async Task<SubCategoryResponse?> UpdateAsync(int id, UpdateSubCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.SubCategories.GetByIdAsync(id);
        if (entity == null) return null;

        entity.Name = request.Name ?? entity.Name;
        if (request.IsActive.HasValue)
        {
            entity.IsActive = request.IsActive.Value;
        }

        _uow.SubCategories.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return new SubCategoryResponse(entity.SubCategoryId, entity.CategoryId ?? 0, entity.Name, entity.IsActive);
    }

    public async Task<SubCategoryResponse?> UpdateStatusAsync(int id, UpdateSubCategoryStatusRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.SubCategories.GetByIdAsync(id);
        if (entity == null) return null;
        entity.IsActive = request.IsActive;
        _uow.SubCategories.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return new SubCategoryResponse(entity.SubCategoryId, entity.CategoryId ?? 0, entity.Name, entity.IsActive);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.SubCategories.GetByIdAsync(id);
        if (entity == null) return false;
        entity.IsActive = false; // soft delete
        _uow.SubCategories.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
