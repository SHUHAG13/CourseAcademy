namespace CourseAcademy.Api.Services.Payments;

/// <summary>Once a minute, cancels pending payments whose time ran out (Payments:PendingMinutes, BR-PAY-20).</summary>
public sealed class PendingPaymentExpiryService(IServiceScopeFactory scopes, ILogger<PendingPaymentExpiryService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await using var scope = scopes.CreateAsyncScope();
                var cancelled = await scope.ServiceProvider.GetRequiredService<IPaymentService>().ExpireStaleAsync(stoppingToken);
                if (cancelled > 0) logger.LogInformation("Cancelled {Count} expired pending payment(s).", cancelled);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A failed run (e.g. database briefly down) is retried on the next tick.
                logger.LogError(exception, "Expiring pending payments failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
