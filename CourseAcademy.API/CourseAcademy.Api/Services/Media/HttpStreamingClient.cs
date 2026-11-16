using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CourseAcademy.Api.Common.Exceptions;

namespace CourseAcademy.Api.Services.Media;

/// <summary>
/// Calls the real Streaming service. The HttpClient is configured in DI with the base address, the X-Api-Key header
/// and the standard resilience handler (retries for safe methods, timeouts, circuit breaker).
/// </summary>
public sealed class HttpStreamingClient(HttpClient http, ILogger<HttpStreamingClient> logger) : IStreamingClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private sealed record UploadResponse(string AssetId, string UploadUrl, string? UploadMethod, Dictionary<string, string>? Headers, DateTime ExpiresAt);
    private sealed record StatusResponse(string AssetId, string Status);
    private sealed record AssetResponse(string AssetId, string Kind, string Status, int? DurationSeconds, long? SizeBytes, string? MimeType, StreamingError? Error);
    private sealed record PlaybackResponse(string PlaylistUrl, DateTime ExpiresAt);
    private sealed record DownloadResponse(string DownloadUrl, DateTime ExpiresAt);

    public async Task<StreamingUpload> CreateUploadAsync(StreamingUploadRequest request, CancellationToken ct)
    {
        var r = await SendAsync<UploadResponse>(HttpMethod.Post, "uploads", request, ct);
        return new StreamingUpload(r.AssetId, r.UploadUrl, r.UploadMethod ?? "PUT", r.Headers ?? [], r.ExpiresAt.ToUniversalTime());
    }

    public async Task<string> CompleteUploadAsync(string assetId, CancellationToken ct) =>
        (await SendAsync<StatusResponse>(HttpMethod.Post, $"uploads/{Uri.EscapeDataString(assetId)}/complete", null, ct)).Status;

    public async Task<StreamingAsset> GetAssetAsync(string assetId, CancellationToken ct)
    {
        var r = await SendAsync<AssetResponse>(HttpMethod.Get, $"assets/{Uri.EscapeDataString(assetId)}", null, ct);
        return new StreamingAsset(r.AssetId, r.Kind, r.Status, r.DurationSeconds, r.SizeBytes, r.MimeType, r.Error);
    }

    public async Task<StreamingLink> CreatePlaybackAsync(string assetId, string viewerRef, int ttlSeconds, CancellationToken ct)
    {
        var r = await SendAsync<PlaybackResponse>(HttpMethod.Post, $"assets/{Uri.EscapeDataString(assetId)}/playback", new { viewerRef, ttlSeconds }, ct);
        return new StreamingLink(r.PlaylistUrl, r.ExpiresAt.ToUniversalTime(), "hls");
    }

    public async Task<StreamingLink> CreateDownloadAsync(string assetId, string viewerRef, int ttlSeconds, string fileName, CancellationToken ct)
    {
        var r = await SendAsync<DownloadResponse>(HttpMethod.Post, $"assets/{Uri.EscapeDataString(assetId)}/download", new { viewerRef, ttlSeconds, fileName }, ct);
        return new StreamingLink(r.DownloadUrl, r.ExpiresAt.ToUniversalTime(), "file");
    }

    public Task DeleteAsync(string assetId, CancellationToken ct) =>
        SendAsync<StatusResponse>(HttpMethod.Delete, $"assets/{Uri.EscapeDataString(assetId)}", null, ct);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body, options: Json);
        try
        {
            using var response = await http.SendAsync(request, ct);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<T>(Json, ct)
                    ?? throw new JsonException("The Streaming service sent an empty response.");
            }
            logger.LogWarning("Streaming {Method} {Path} answered {Status}.", method, path, (int)response.StatusCode);
            // 4xx means Streaming refused this request (e.g. bytes not uploaded); retrying the same call will not help.
            if (response.StatusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError
                && response.StatusCode is not (HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests))
            {
                throw new ConflictException("The file service did not accept this request. Upload the file again.");
            }
            throw new ServiceUnavailableException();
        }
        catch (Exception exception) when (exception is not AppException && !ct.IsCancellationRequested)
        {
            // Network errors, timeouts, an open circuit or an unreadable answer.
            logger.LogWarning(exception, "Streaming {Method} {Path} failed.", method, path);
            throw new ServiceUnavailableException();
        }
    }
}
