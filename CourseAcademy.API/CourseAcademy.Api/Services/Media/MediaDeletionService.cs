using System.Threading.Channels;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Repositories;

namespace CourseAcademy.Api.Services.Media;

/// <summary>Wakes the deletion worker right away (e.g. after a curriculum save released files) instead of at the next minute.</summary>
public sealed class MediaDeletionSignal
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });

    public void Notify() => _channel.Writer.TryWrite(true);

    public ValueTask<bool> WaitAsync(CancellationToken ct) => _channel.Reader.WaitToReadAsync(ct);

    public void Reset() => _channel.Reader.TryRead(out _);
}

/// <summary>
/// Sends Streaming the delete requests for files marked "deleting" (no longer used by any lesson or material).
/// Runs when signalled and once a minute, so a failed request is retried. Streaming confirms with asset.deleted.
/// </summary>
public sealed class MediaDeletionService(
    MediaDeletionSignal signal, IServiceScopeFactory scopes, ILogger<MediaDeletionService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                signal.Reset();
                await using var scope = scopes.CreateAsyncScope();
                var sent = await scope.ServiceProvider.GetRequiredService<MediaDeletionSender>().SendPendingAsync(stoppingToken);
                if (sent > 0) logger.LogInformation("Sent {Count} file delete request(s) to Streaming.", sent);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Sending file delete requests failed.");
            }

            using var wait = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            wait.CancelAfter(Interval);
            try
            {
                await signal.WaitAsync(wait.Token);
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                // The minute passed without a signal.
            }
        }
    }
}

/// <summary>Sends Streaming the delete requests of files marked "deleting".</summary>
public sealed class MediaDeletionSender(
    IMediaAssetRepository assets, IStreamingClient streaming, IUnitOfWork unitOfWork, TimeProvider clock, ILogger<MediaDeletionSender> logger)
{
    private const int BatchSize = 50;

    /// <summary>Sends the pending delete requests. Returns how many were sent; failed ones are tried again next time.</summary>
    public async Task<int> SendPendingAsync(CancellationToken ct)
    {
        var sent = 0;
        foreach (var asset in await assets.ListDeletionsToSendAsync(MediaStatusCodes.Id(MediaStatusCodes.Deleting), BatchSize, ct))
        {
            if (asset.AssetId is null)
            {
                // Streaming never created it, so there is nothing to delete there.
                asset.MediaAssetStatusId = MediaStatusCodes.Id(MediaStatusCodes.Deleted);
            }
            else
            {
                try
                {
                    await streaming.DeleteAsync(asset.AssetId, ct);
                    asset.DeleteRequestedAt = clock.GetUtcNow().UtcDateTime;
                    sent++;
                }
                catch (AppException exception)
                {
                    logger.LogWarning(exception, "Delete request for asset {AssetId} failed; it is retried later.", asset.AssetId);
                    continue;
                }
            }
            await unitOfWork.SaveChangesAsync(ct);
        }
        return sent;
    }
}
