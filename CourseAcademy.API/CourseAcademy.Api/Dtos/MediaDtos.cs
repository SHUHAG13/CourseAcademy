namespace CourseAcademy.Api.Dtos;

/// <summary>Starts an upload for a course. Kind: "video" or "document".</summary>
public sealed record CreateUploadRequest(int CourseId, string Kind, string FileName, string MimeType, long SizeBytes);

/// <summary>Where the browser sends the file bytes (directly to storage, with the given method and headers).</summary>
public sealed record MediaUploadDto(int MediaAssetId, string UploadUrl, string UploadMethod, IReadOnlyDictionary<string, string> Headers, DateTime ExpiresAt);

/// <summary>An uploaded file as course managers see it.</summary>
public sealed record MediaAssetDto(
    int Id, int CourseId, string KindCode, string StatusCode, string FileName, string MimeType, long SizeBytes, int? DurationSeconds, string? ErrorMessage);

/// <summary>A short-lived link to play an uploaded video. Format "hls" (use an HLS player) or "file" (a plain video file).</summary>
public sealed record PlaybackDto(string Url, string Format, DateTime ExpiresAt, int? DurationSeconds);

public sealed record DownloadDto(string Url, DateTime ExpiresAt);
