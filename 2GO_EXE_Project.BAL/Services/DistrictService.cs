using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Districts;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services;

public class DistrictService : IDistrictService
{
    private readonly IUnitOfWork _uow;

    public DistrictService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<DistrictListResponse> GetDistrictsAsync(int? cityId, string? search, CancellationToken cancellationToken = default)
    {
        var query = _uow.Districts.Query();

        // Filter by city
        if (cityId.HasValue)
        {
            query = query.Where(d => d.CityId == cityId.Value);
        }

        // Filter by search
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d => d.Name != null && d.Name.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(d => d.City)
            .OrderBy(d => d.Name)
            .Select(d => new DistrictResponse(
                d.DistrictId,
                d.CityId,
                d.Name,
                d.City != null ? d.City.Name : null))
            .ToListAsync(cancellationToken);

        return new DistrictListResponse(total, items);
    }

    public async Task<DistrictResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var district = await _uow.Districts.Query()
            .Include(d => d.City)
            .Where(d => d.DistrictId == id)
            .FirstOrDefaultAsync(cancellationToken);

        if (district == null) return null;

        return new DistrictResponse(
            district.DistrictId,
            district.CityId,
            district.Name,
            district.City?.Name);
    }

    public async Task<DistrictResponse> CreateAsync(CreateDistrictRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(CatalogValidator.ValidateCreateDistrict(request));
        var entity = new District
        {
            CityId = request.CityId,
            Name = request.Name
        };

        await _uow.Districts.AddAsync(entity, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);

        // Load city name
        var city = await _uow.Cities.GetByIdAsync(request.CityId);

        return new DistrictResponse(
            entity.DistrictId,
            entity.CityId,
            entity.Name,
            city?.Name);
    }

    public async Task<DistrictResponse?> UpdateAsync(int id, UpdateDistrictRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(CatalogValidator.ValidateUpdateDistrict(request));
        var entity = await _uow.Districts.GetByIdAsync(id);
        if (entity == null) return null;

        if (request.CityId.HasValue)
        {
            entity.CityId = request.CityId.Value;
        }

        if (request.Name != null)
        {
            entity.Name = request.Name;
        }

        _uow.Districts.Update(entity);
        await _uow.SaveChangesAsync(cancellationToken);

        // Load city name
        var city = entity.CityId.HasValue 
            ? await _uow.Cities.GetByIdAsync(entity.CityId.Value) 
            : null;

        return new DistrictResponse(
            entity.DistrictId,
            entity.CityId,
            entity.Name,
            city?.Name);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _uow.Districts.GetByIdAsync(id);
        if (entity == null) return false;

        _uow.Districts.Remove(entity);
        await _uow.SaveChangesAsync(cancellationToken);
        return true;
    }
}
