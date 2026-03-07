using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using _2GO_EXE_Project.BAL.DTOs.Wards;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services
{
    public class WardService : IWardService
    {   
       private readonly IUnitOfWork _uow;

        public WardService(IUnitOfWork uow)
        {
            _uow = uow;
        }
    public async Task<WardListResponse> GetWardsAsync(int? districtId, string? search, CancellationToken cancellationToken = default)
    {   
        var query = _uow.Wards.Query();

        // Filter by district
        if (districtId.HasValue)
        {
            query = query.Where(w => w.DistrictId == districtId.Value);
        }

        // Filter by search
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(w => w.Name != null && w.Name.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(w => w.District)
            .OrderBy(w => w.Name)
            .Select(w => new WardResponse(
                w.WardId,
                w.DistrictId,
                w.Name,
                w.District != null ? w.District.Name : null))
            .ToListAsync(cancellationToken);

        return new WardListResponse(total, items);
        
    }
    public async Task<WardResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var ward = await _uow.Wards.Query()
                .Include(w => w.District)
                .Where(w => w.WardId == id)
                .FirstOrDefaultAsync(cancellationToken);

            if (ward == null) return null;

            return new WardResponse(
                ward.WardId,
                ward.DistrictId,
                ward.Name,
                ward.District != null ? ward.District.Name : null);
        }
    public async Task<WardResponse> CreateAsync(CreateWardRequest request, CancellationToken cancellationToken = default)
        {
            ValidationGuard.ThrowIfInvalid(CatalogValidator.ValidateCreateWard(request));
            var entity = new DAL.Entities.Ward
            {
                DistrictId = request.DistrictId,
                Name = request.Name
            };

            await  _uow.Wards.AddAsync(entity);
            await _uow.SaveChangesAsync(cancellationToken);

            return new WardResponse(
                entity.WardId,
                entity.DistrictId,
                entity.Name,
                null);
        }
    public async Task<WardResponse?> UpdateAsync(int id, UpdateWardRequest request, CancellationToken cancellationToken = default)
        {
            ValidationGuard.ThrowIfInvalid(CatalogValidator.ValidateUpdateWard(request));
            var entity = await _uow.Wards.GetByIdAsync(id);
            if (entity == null) return null;

            if (request.DistrictId.HasValue)
            {
                entity.DistrictId = request.DistrictId.Value;
            }
            if (request.Name != null)
            {
                entity.Name = request.Name;
            }

            _uow.Wards.Update(entity);
            await _uow.SaveChangesAsync(cancellationToken);

            return new WardResponse(
                entity.WardId,
                entity.DistrictId,
                entity.Name,
                null);
        }
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var entity = await _uow.Wards.GetByIdAsync(id);
            if (entity == null) return false;

            _uow.Wards.Remove(entity);
            await _uow.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
