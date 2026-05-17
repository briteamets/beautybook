using App.BLL.Services;

namespace WebApp.Services;

public class PendingBookingExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<PendingBookingExpiryService> _logger;
    private readonly TimeSpan _interval;

    public PendingBookingExpiryService(
        IServiceScopeFactory scopeFactory,
        ILogger<PendingBookingExpiryService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        var minutes = configuration.GetValue("BookingExpiry:IntervalMinutes", 15);
        _interval = TimeSpan.FromMinutes(Math.Max(1, minutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to expire past-pending bookings.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<BookingExpiryService>();
        var count = await service.ExpirePastPendingBookingsAsync(ct);
        if (count > 0)
            _logger.LogInformation("Auto-cancelled {Count} past-pending booking(s).", count);
    }
}
