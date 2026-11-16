using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using CourseAcademy.Api.Common.Exceptions;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Api.Services.Media;

// A stand-in for the Streaming service, for Development only (Streaming:Mode = Fake, streaming spec section 8).
// Files are kept in a local folder; "processing" is a short wait; status changes are sent as signed webhooks
// to the real webhook handler, so that code path is exercised too. Videos are served as plain files, not HLS.

/// <summary>What the fake service knows about one file. Stored as JSON next to the file, so it survives restarts.</summary>
public sealed class FakeAsset
{
    public string AssetId { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Status { get; set; } = "awaiting_upload";
    public long Sequence { get; set; }
    public DateTime UploadExpiresAt { get; set; }
}

public sealed class FakeStreamingStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, object> _locks = new();
    private readonly string _root;
    private readonly byte[] _secret;

    public FakeStreamingStore(IOptions<StreamingSettings> settings, IWebHostEnvironment environment)
    {
        _root = Path.GetFullPath(Path.Combine(environment.ContentRootPath, settings.Value.FakeStoragePath));
        Directory.CreateDirectory(_root);
        _secret = Encoding.UTF8.GetBytes(settings.Value.WebhookSecret);
    }

    public string FilePath(string assetId) => Path.Combine(_root, SafeId(assetId) + ".bin");

    public FakeAsset? Get(string assetId)
    {
        var path = MetaPath(assetId);
        return File.Exists(path) ? JsonSerializer.Deserialize<FakeAsset>(File.ReadAllText(path), Json) : null;
    }

    public void Save(FakeAsset asset) => File.WriteAllText(MetaPath(asset.AssetId), JsonSerializer.Serialize(asset, Json));

    /// <summary>Runs <paramref name="change"/> on the stored asset under a per-asset lock and saves it.</summary>
    public FakeAsset? Update(string assetId, Action<FakeAsset> change)
    {
        lock (_locks.GetOrAdd(assetId, _ => new object()))
        {
            var asset = Get(assetId);
            if (asset is null) return null;
            change(asset);
            Save(asset);
            return asset;
        }
    }

    public void DeleteFile(string assetId)
    {
        if (File.Exists(FilePath(assetId))) File.Delete(FilePath(assetId));
    }

    /// <summary>Signature of a fake upload or file link, valid until <paramref name="expires"/> (Unix seconds).</summary>
    public string Sign(string purpose, string assetId, long expires) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(_secret, Encoding.UTF8.GetBytes($"{purpose}:{assetId}:{expires}")));

    public bool Verify(string purpose, string assetId, long expires, string? signature, DateTimeOffset now) =>
        signature is not null && expires >= now.ToUnixTimeSeconds()
        && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(Sign(purpose, assetId, expires)), Encoding.ASCII.GetBytes(signature));

    private string MetaPath(string assetId) => Path.Combine(_root, SafeId(assetId) + ".json");

    private static string SafeId(string assetId) =>
        assetId.All(c => char.IsAsciiLetterOrDigit(c) || c == '-') ? assetId : throw new NotFoundException("File not found.");
}

/// <summary>Work for the fake processor: process an uploaded file, or delete one.</summary>
public sealed record FakeStreamingJob(string AssetId, bool Delete);

public sealed class FakeStreamingQueue
{
    private readonly Channel<FakeStreamingJob> _channel = Channel.CreateUnbounded<FakeStreamingJob>();

    public void Enqueue(FakeStreamingJob job) => _channel.Writer.TryWrite(job);

    public IAsyncEnumerable<FakeStreamingJob> ReadAllAsync(CancellationToken ct) => _channel.Reader.ReadAllAsync(ct);
}

public sealed class FakeStreamingClient(
    FakeStreamingStore store, FakeStreamingQueue queue, IHttpContextAccessor http, IOptions<StreamingSettings> settings, TimeProvider clock) : IStreamingClient
{
    public const string RoutePrefix = "api/v1/dev/fake-streaming";

    public Task<StreamingUpload> CreateUploadAsync(StreamingUploadRequest request, CancellationToken ct)
    {
        var expiresAt = clock.GetUtcNow().AddMinutes(settings.Value.UploadUrlMinutes);
        var asset = new FakeAsset
        {
            AssetId = "fake-" + Guid.NewGuid().ToString("N"), Kind = request.Kind, FileName = request.FileName,
            MimeType = request.MimeType, SizeBytes = request.SizeBytes, UploadExpiresAt = expiresAt.UtcDateTime,
        };
        store.Save(asset);
        var url = Link("uploads", asset.AssetId, "upload", expiresAt.ToUnixTimeSeconds());
        return Task.FromResult(new StreamingUpload(asset.AssetId, url, "PUT",
            new Dictionary<string, string> { ["Content-Type"] = request.MimeType }, expiresAt.UtcDateTime));
    }

    public Task<string> CompleteUploadAsync(string assetId, CancellationToken ct)
    {
        var asset = store.Get(assetId) ?? throw new ConflictException("The file service does not know this upload. Upload the file again.");
        if (asset.Status == "awaiting_upload")
        {
            if (!File.Exists(store.FilePath(assetId)) || new FileInfo(store.FilePath(assetId)).Length == 0)
            {
                throw new ConflictException("The file has not been uploaded yet.");
            }
            store.Update(assetId, a => a.Status = MediaStatusCodes.Uploaded);
            queue.Enqueue(new FakeStreamingJob(assetId, Delete: false));
        }
        return Task.FromResult(store.Get(assetId)!.Status);
    }

    public Task<StreamingAsset> GetAssetAsync(string assetId, CancellationToken ct)
    {
        var asset = store.Get(assetId) ?? throw new ConflictException("The file service does not know this file.");
        var status = asset.Status == "awaiting_upload" ? MediaStatusCodes.Uploaded : asset.Status;
        return Task.FromResult(new StreamingAsset(asset.AssetId, asset.Kind, status, null, asset.SizeBytes, asset.MimeType, null));
    }

    public Task<StreamingLink> CreatePlaybackAsync(string assetId, string viewerRef, int ttlSeconds, CancellationToken ct) =>
        Task.FromResult(FileLink(assetId, ttlSeconds, download: false));

    public Task<StreamingLink> CreateDownloadAsync(string assetId, string viewerRef, int ttlSeconds, string fileName, CancellationToken ct) =>
        Task.FromResult(FileLink(assetId, ttlSeconds, download: true));

    public Task DeleteAsync(string assetId, CancellationToken ct)
    {
        if (store.Update(assetId, a => a.Status = MediaStatusCodes.Deleting) is not null) queue.Enqueue(new FakeStreamingJob(assetId, Delete: true));
        return Task.CompletedTask;
    }

    private StreamingLink FileLink(string assetId, int ttlSeconds, bool download)
    {
        var asset = store.Get(assetId);
        if (asset?.Status != MediaStatusCodes.Ready) throw new ConflictException("The file is not ready.");
        var expiresAt = clock.GetUtcNow().AddSeconds(ttlSeconds);
        var url = Link("files", assetId, "file", expiresAt.ToUnixTimeSeconds()) + (download ? "&download=true" : string.Empty);
        return new StreamingLink(url, expiresAt.UtcDateTime, "file");
    }

    /// <summary>An absolute link to this API (the browser opens it directly), signed and with an expiry.</summary>
    private string Link(string path, string assetId, string purpose, long expires)
    {
        var request = http.HttpContext?.Request ?? throw new InvalidOperationException("Fake streaming links need an HTTP request.");
        return $"{request.Scheme}://{request.Host}{request.PathBase}/{RoutePrefix}/{path}/{assetId}" +
            $"?expires={expires.ToString(CultureInfo.InvariantCulture)}&signature={store.Sign(purpose, assetId, expires)}";
    }
}

/// <summary>
/// Simulates Streaming's work: after an upload it waits, then reports queued → processing → ready; after a delete
/// request it removes the file and reports deleted. Each report is a signed webhook given to the real handler.
/// </summary>
public sealed class FakeStreamingProcessor(
    FakeStreamingQueue queue, FakeStreamingStore store, IServiceScopeFactory scopes, IOptions<StreamingSettings> settings,
    TimeProvider clock, ILogger<FakeStreamingProcessor> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var job in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                if (job.Delete)
                {
                    store.DeleteFile(job.AssetId);
                    await SendAsync(job.AssetId, "asset.deleted", MediaStatusCodes.Deleted, stoppingToken);
                    continue;
                }
                await SendAsync(job.AssetId, "asset.queued", MediaStatusCodes.Queued, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(settings.Value.FakeProcessingSeconds / 2.0), clock, stoppingToken);
                await SendAsync(job.AssetId, "asset.processing", MediaStatusCodes.Processing, stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(settings.Value.FakeProcessingSeconds / 2.0), clock, stoppingToken);
                await SendAsync(job.AssetId, "asset.ready", MediaStatusCodes.Ready, stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Fake streaming failed for asset {AssetId}.", job.AssetId);
            }
        }
    }

    private async Task SendAsync(string assetId, string type, string status, CancellationToken ct)
    {
        var asset = store.Update(assetId, a =>
        {
            a.Status = status;
            a.Sequence++;
        });
        if (asset is null) return;

        var eventId = Guid.NewGuid().ToString();
        var body = JsonSerializer.Serialize(new
        {
            eventId,
            type,
            occurredAt = clock.GetUtcNow().UtcDateTime,
            assetId,
            sequence = asset.Sequence,
            data = new { sizeBytes = asset.SizeBytes, mimeType = asset.MimeType },
        }, Json);
        var timestamp = clock.GetUtcNow().ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [StreamingWebhookService.EventIdHeader] = eventId,
            [StreamingWebhookService.TimestampHeader] = timestamp,
            [StreamingWebhookService.SignatureHeader] = WebhookSignature.Compute(settings.Value.WebhookSecret, timestamp, body),
        };

        await using var scope = scopes.CreateAsyncScope();
        var result = await scope.ServiceProvider.GetRequiredService<IStreamingWebhookService>().HandleAsync(body, headers, ct);
        if (result != WebhookResult.Accepted) logger.LogWarning("Fake webhook {Type} for {AssetId} was refused: {Result}.", type, assetId, result);
    }
}
