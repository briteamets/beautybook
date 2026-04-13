using App.DAL.EF;
using App.Domain;
using Microsoft.EntityFrameworkCore;

namespace WebApp.Services;

public class BookingCompletionHostedService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingCompletionHostedService> _logger;

    public BookingCompletionHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingCompletionHostedService> logger)
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
                await CompleteOverdueBookingsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to auto-complete overdue bookings.");
            }

            await Task.Delay(CheckInterval, stoppingToken);
        }
    }

    private async Task CompleteOverdueBookingsAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var candidates = await context.Bookings
            .AsTracking()
            .Where(b =>
                b.To == null &&
                b.Status == Booking.StatusConfirmed &&
                b.BookingDate <= today)
            .ToListAsync(cancellationToken);

        foreach (var booking in candidates.Where(HasEnded))
        {
            var oldStatus = booking.Status;
            booking.Status = Booking.StatusCompleted;

            context.BookingHistories.Add(new BookingHistory
            {
                BookingId = booking.Id,
                OldStatus = oldStatus,
                NewStatus = Booking.StatusCompleted,
                UserId = booking.UserId,
                ChangedAt = now,
                Reason = "Automatically completed after the booking end time.",
                From = now
            });
        }

        if (context.ChangeTracker.HasChanges())
        {
            await context.SaveChangesAsync(cancellationToken);
        }

        return;

        bool HasEnded(Booking booking)
        {
            var endDateTime = booking.BookingDate.ToDateTime(
                TimeOnly.FromTimeSpan(booking.EndTime),
                DateTimeKind.Utc);
            return endDateTime <= now;
        }
    }
}
