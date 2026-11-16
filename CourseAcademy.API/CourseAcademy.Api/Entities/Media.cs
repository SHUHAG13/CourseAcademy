namespace CourseAcademy.Api.Entities;

/// <summary>
/// A file kept by the Streaming service: a video (played as HLS) or a document (downloaded).
/// The Main App stores only the Streaming asset id and a copy of its status, never a file URL.
/// </summary>
public sealed class MediaAsset : BaseEntity
{
    /// <summary>The Streaming service's id. Null until Streaming has created the upload.</summary>
    public string? AssetId { get; set; }
    public int MediaKindId { get; set; }
    public MediaKind Kind { get; set; } = null!;
    public int MediaAssetStatusId { get; set; }
    public MediaAssetStatus Status { get; set; } = null!;

    /// <summary>The course the file was uploaded for. A file can only be used in that course.</summary>
    public int CourseId { get; set; }
    public int UploadedByUserId { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    /// <summary>Video length, reported by Streaming once processed.</summary>
    public int? DurationSeconds { get; set; }

    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    /// <summary>Sequence of the last webhook applied, so older events arriving late are ignored.</summary>
    public long LastSequence { get; set; }
    public DateTime? UploadExpiresAt { get; set; }
    public DateTime? ReadyAt { get; set; }
    /// <summary>When the delete request was sent to Streaming. Null while it still has to be sent.</summary>
    public DateTime? DeleteRequestedAt { get; set; }
}

/// <summary>Every webhook received from Streaming, stored before it is acted on. EventId is unique, so a repeat is ignored.</summary>
public sealed class StreamingWebhookEvent : BaseEntity
{
    public string EventId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string AssetId { get; set; } = string.Empty;
    public long Sequence { get; set; }
    public DateTime OccurredAt { get; set; }
    /// <summary>applied, stale (older than the last applied event) or unknown_asset.</summary>
    public string Outcome { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
}
