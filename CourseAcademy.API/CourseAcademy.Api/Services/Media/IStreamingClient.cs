namespace CourseAcademy.Api.Services.Media;

// The Streaming service's internal API (docs/integration/streaming-integration.md, section 5).
// Only the Main App calls it; failures surface as ServiceUnavailableException (503) or ConflictException (409).

public sealed record StreamingUploadRequest(string Kind, string FileName, string MimeType, long SizeBytes, string ExternalRef);

public sealed record StreamingUpload(string AssetId, string UploadUrl, string UploadMethod, IReadOnlyDictionary<string, string> Headers, DateTime ExpiresAt);

public sealed record StreamingError(string Code, string Message);

public sealed record StreamingAsset(string AssetId, string Kind, string Status, int? DurationSeconds, long? SizeBytes, string? MimeType, StreamingError? Error);

/// <summary>A short-lived signed link. Format: "hls" (playlist) or "file" (plain file, e.g. downloads and the fake service).</summary>
public sealed record StreamingLink(string Url, DateTime ExpiresAt, string Format);

public interface IStreamingClient
{
    Task<StreamingUpload> CreateUploadAsync(StreamingUploadRequest request, CancellationToken ct);

    /// <summary>Tells Streaming the bytes are uploaded. Returns its new status.</summary>
    Task<string> CompleteUploadAsync(string assetId, CancellationToken ct);

    Task<StreamingAsset> GetAssetAsync(string assetId, CancellationToken ct);

    Task<StreamingLink> CreatePlaybackAsync(string assetId, string viewerRef, int ttlSeconds, CancellationToken ct);

    Task<StreamingLink> CreateDownloadAsync(string assetId, string viewerRef, int ttlSeconds, string fileName, CancellationToken ct);

    Task DeleteAsync(string assetId, CancellationToken ct);
}
