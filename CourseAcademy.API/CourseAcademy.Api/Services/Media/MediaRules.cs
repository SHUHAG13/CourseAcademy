using System.Security.Cryptography;
using System.Text;
using CourseAcademy.Api.Data.Seed;

namespace CourseAcademy.Api.Services.Media;

public static class MediaKindCodes
{
    public const string Video = "video";
    public const string Document = "document";

    public static int Id(string code) => LookupSeed.MediaKinds.Single(k => k.Code == code).Id;
    public static string Code(int id) => LookupSeed.MediaKinds.Single(k => k.Id == id).Code;
}

public static class MediaStatusCodes
{
    public const string PendingUpload = "pending_upload";
    public const string Uploaded = "uploaded";
    public const string Queued = "queued";
    public const string Processing = "processing";
    public const string Ready = "ready";
    public const string Failed = "failed";
    public const string Deleting = "deleting";
    public const string Deleted = "deleted";

    public static int Id(string code) => LookupSeed.MediaAssetStatuses.Single(s => s.Code == code).Id;
    public static string Code(int id) => LookupSeed.MediaAssetStatuses.Single(s => s.Id == id).Code;

    /// <summary>Still on its way to ready: shown to managers as "processing", hidden from students.</summary>
    public static bool InProgress(string code) => code is PendingUpload or Uploaded or Queued or Processing;

    /// <summary>On its way out or gone: cannot be attached or played any more.</summary>
    public static bool Removed(string code) => code is Deleting or Deleted;
}

/// <summary>Upload limits of the streaming spec (section 4).</summary>
public static class MediaRules
{
    public const long MaxVideoBytes = 2L * 1024 * 1024 * 1024;
    public const long MaxDocumentBytes = 50L * 1024 * 1024;
    public const int MaxFileNameLength = 255;

    public static readonly IReadOnlySet<string> VideoMimeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "video/mp4", "video/webm", "video/quicktime",
    };

    /// <summary>Document MIME type → file extension. Section documents must match their type (PDF, DOCX, PPTX).</summary>
    public static readonly IReadOnlyDictionary<string, string> DocumentExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = ".pdf",
        ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ".docx",
        ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = ".pptx",
    };

    /// <summary>The first problem with an upload, or null when it is acceptable.</summary>
    public static string? UploadError(string kind, string mimeType, long sizeBytes) => kind switch
    {
        MediaKindCodes.Video when !VideoMimeTypes.Contains(mimeType) => "Upload an MP4, WebM or MOV video.",
        MediaKindCodes.Video when sizeBytes > MaxVideoBytes => "A video can be up to 2 GB.",
        MediaKindCodes.Document when !DocumentExtensions.ContainsKey(mimeType) => "Upload a PDF, DOCX or PPTX file.",
        MediaKindCodes.Document when sizeBytes > MaxDocumentBytes => "A document can be up to 50 MB.",
        MediaKindCodes.Video or MediaKindCodes.Document => sizeBytes < 1 ? "The file is empty." : null,
        _ => "Choose video or document.",
    };
}

/// <summary>Webhook signature of the streaming spec: sha256=hex(HMAC-SHA256(secret, "{timestamp}.{rawBody}")).</summary>
public static class WebhookSignature
{
    public static string Compute(string secret, string timestamp, string rawBody)
    {
        var hash = HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes($"{timestamp}.{rawBody}"));
        return "sha256=" + Convert.ToHexStringLower(hash);
    }

    /// <summary>Constant-time comparison, so the check leaks nothing through timing.</summary>
    public static bool Matches(string secret, string timestamp, string rawBody, string signature) =>
        CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Compute(secret, timestamp, rawBody)),
            Encoding.ASCII.GetBytes(signature.Trim().ToLowerInvariant()));
}
