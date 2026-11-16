using System.Globalization;
using System.Text.Json;
using CourseAcademy.Api.Common;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Api.Services.Media;

public enum WebhookResult
{
    /// <summary>Stored (and applied when it is new); answer 200.</summary>
    Accepted,
    /// <summary>Missing, wrong or too old signature; answer 401.</summary>
    Unauthorized,
    /// <summary>Signed correctly but unreadable; answer 400.</summary>
    Invalid,
}

public interface IStreamingWebhookService
{
    /// <summary>Handles one webhook (streaming spec, section 6). Throws when the event could not be stored, so Streaming retries.</summary>
    Task<WebhookResult> HandleAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct);
}

public sealed class StreamingWebhookService(
    IMediaAssetRepository assets,
    IRepository<StreamingWebhookEvent> events,
    IUnitOfWork unitOfWork,
    IOptions<StreamingSettings> settings,
    TimeProvider clock,
    ILogger<StreamingWebhookService> logger) : IStreamingWebhookService
{
    public const string EventIdHeader = "X-Streaming-Event-Id";
    public const string TimestampHeader = "X-Streaming-Timestamp";
    public const string SignatureHeader = "X-Streaming-Signature";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record Body(string EventId, string Type, DateTime OccurredAt, string AssetId, long Sequence, BodyData? Data);
    private sealed record BodyData(int? DurationSeconds, long? SizeBytes, string? MimeType, StreamingError? Error);

    public async Task<WebhookResult> HandleAsync(string rawBody, IReadOnlyDictionary<string, string> headers, CancellationToken ct)
    {
        if (!headers.TryGetValue(EventIdHeader, out var eventId) || !headers.TryGetValue(TimestampHeader, out var timestamp)
            || !headers.TryGetValue(SignatureHeader, out var signature))
        {
            return WebhookResult.Unauthorized;
        }
        // 1. Signature over the raw body, and not older than the tolerance (replay protection).
        var now = clock.GetUtcNow();
        if (!long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds)
            || Math.Abs((now - DateTimeOffset.FromUnixTimeSeconds(seconds)).TotalMinutes) > settings.Value.WebhookToleranceMinutes
            || !WebhookSignature.Matches(settings.Value.WebhookSecret, timestamp, rawBody, signature))
        {
            logger.LogWarning("Refused a streaming webhook with a missing, wrong or expired signature (event {EventId}).", eventId);
            return WebhookResult.Unauthorized;
        }

        Body? body;
        try
        {
            body = JsonSerializer.Deserialize<Body>(rawBody, Json);
        }
        catch (JsonException)
        {
            body = null;
        }
        if (body is null || body.EventId != eventId || string.IsNullOrWhiteSpace(body.AssetId) || string.IsNullOrWhiteSpace(body.Type)
            || body.EventId.Length > 100 || body.AssetId.Length > 100 || body.Type.Length > 50 || body.Sequence < 0)
        {
            return WebhookResult.Invalid;
        }

        // 2. A repeated event is acknowledged without being applied again.
        if (await events.AnyAsync(e => e.EventId == body.EventId, ct)) return WebhookResult.Accepted;

        var asset = await assets.GetByAssetIdAsync(body.AssetId, ct);
        var outcome = asset is null ? "unknown_asset" : Apply(asset, body);
        await events.AddAsync(new StreamingWebhookEvent
        {
            EventId = body.EventId, Type = body.Type, AssetId = body.AssetId, Sequence = body.Sequence,
            OccurredAt = body.OccurredAt.ToUniversalTime(), Outcome = outcome, Payload = rawBody,
        }, ct);

        // 4. Only answer 200 once the event row is stored; any other failure becomes a 5xx and Streaming retries.
        try
        {
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception) when (DbErrors.IsUniqueViolation(exception))
        {
            // The same event arrived twice at the same moment; the other copy was stored.
            return WebhookResult.Accepted;
        }
        logger.LogInformation("Streaming webhook {Type} for asset {AssetId} (sequence {Sequence}): {Outcome}.", body.Type, body.AssetId, body.Sequence, outcome);
        return WebhookResult.Accepted;
    }

    /// <summary>Updates the asset when the event is newer than the last one applied (3. out-of-order events are ignored).</summary>
    private string Apply(MediaAsset asset, Body body)
    {
        if (body.Sequence <= asset.LastSequence) return "stale";
        var current = MediaStatusCodes.Code(asset.MediaAssetStatusId);
        var next = body.Type switch
        {
            "asset.queued" => MediaStatusCodes.Queued,
            "asset.processing" => MediaStatusCodes.Processing,
            "asset.ready" => MediaStatusCodes.Ready,
            "asset.failed" => MediaStatusCodes.Failed,
            "asset.deleted" => MediaStatusCodes.Deleted,
            _ => null,
        };
        if (next is null) return "ignored";
        // A file the Main App is removing never comes back to life.
        if (MediaStatusCodes.Removed(current) && next != MediaStatusCodes.Deleted) return "ignored";

        asset.LastSequence = body.Sequence;
        asset.MediaAssetStatusId = MediaStatusCodes.Id(next);
        if (body.Data?.DurationSeconds is { } duration and >= 0) asset.DurationSeconds = duration;
        if (body.Data?.SizeBytes is { } size and > 0) asset.SizeBytes = size;
        if (next == MediaStatusCodes.Ready)
        {
            asset.ReadyAt = clock.GetUtcNow().UtcDateTime;
            asset.ErrorCode = null;
            asset.ErrorMessage = null;
        }
        if (next == MediaStatusCodes.Failed)
        {
            asset.ErrorCode = Truncate(body.Data?.Error?.Code, 100) ?? "FAILED";
            asset.ErrorMessage = Truncate(body.Data?.Error?.Message, 500) ?? "The file could not be processed.";
        }
        return "applied";
    }

    private static string? Truncate(string? value, int length) => value is null ? null : value.Length <= length ? value : value[..length];
}
