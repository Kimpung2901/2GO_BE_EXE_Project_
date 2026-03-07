using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.BAL.Services;

public class EscrowPayoutRetryService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(30);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EscrowPayoutRetryService> _logger;

    public EscrowPayoutRetryService(IServiceScopeFactory scopeFactory, ILogger<EscrowPayoutRetryService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var escrowService = scope.ServiceProvider.GetRequiredService<IEscrowService>();
                await escrowService.RetryFailedForfeitPayoutsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Escrow payout retry job failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
