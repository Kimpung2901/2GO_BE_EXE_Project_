using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.DTOs.Banks;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class BankService : IBankService
{
    private readonly IUnitOfWork _uow;

    public BankService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<BankListResponse> GetAllAsync(bool? isActive, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = _uow.Banks.Query().AsQueryable();
        if (isActive.HasValue)
        {
            query = query.Where(b => b.IsActive == isActive.Value);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(b => b.Name)
            .Skip(skip < 0 ? 0 : skip)
            .Take(take <= 0 ? 50 : Math.Min(take, 200))
            .Select(b => new BankResponse(b.BankId, b.Name, b.Bin, b.Code, b.IsActive))
            .ToListAsync(cancellationToken);

        return new BankListResponse(total, items);
    }
}
