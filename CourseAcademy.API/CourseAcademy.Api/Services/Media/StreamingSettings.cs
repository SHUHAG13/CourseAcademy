using System.ComponentModel.DataAnnotations;

namespace CourseAcademy.Api.Services.Media;

/// <summary>
/// Section "Streaming" in configuration. ApiKey and WebhookSecret are secrets: set them with user secrets or
/// environment variables (Streaming__ApiKey, Streaming__WebhookSecret).
/// </summary>
public sealed class StreamingSettings : IValidatableObject
{
    public const string SectionName = "Streaming";
    public const string FakeMode = "Fake";
    public const string HttpMode = "Http";

    /// <summary>Fake (built-in stand-in, Development only) or Http (the real Streaming service).</summary>
    [Required]
    public string Mode { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>Shared secret of the webhook signatures. In Fake mode a random one is made at start-up when empty.</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>Lifetime of the temporary upload link.</summary>
    [Range(1, 60)]
    public int UploadUrlMinutes { get; set; } = 15;

    /// <summary>Lifetime of playback and download links.</summary>
    [Range(1, 60)]
    public int LinkMinutes { get; set; } = 10;

    /// <summary>Webhooks signed longer ago than this are refused (replay protection).</summary>
    [Range(1, 60)]
    public int WebhookToleranceMinutes { get; set; } = 5;

    /// <summary>Fake mode: folder for uploaded files, relative to the content root.</summary>
    public string FakeStoragePath { get; set; } = "App_Data/fake-streaming";

    /// <summary>Fake mode: simulated processing time before a file becomes ready.</summary>
    [Range(0, 600)]
    public int FakeProcessingSeconds { get; set; } = 3;

    public bool IsFake => string.Equals(Mode, FakeMode, StringComparison.OrdinalIgnoreCase);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsFake && !string.Equals(Mode, HttpMode, StringComparison.OrdinalIgnoreCase))
        {
            yield return new ValidationResult("Streaming:Mode must be Fake or Http.", [nameof(Mode)]);
            yield break;
        }
        if (IsFake) yield break;
        if (!Uri.TryCreate(BaseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
        {
            yield return new ValidationResult("Streaming:BaseUrl must be the Streaming service's http(s) address.", [nameof(BaseUrl)]);
        }
        if (ApiKey.Length < 16) yield return new ValidationResult("Streaming:ApiKey must be set (16+ characters).", [nameof(ApiKey)]);
        if (WebhookSecret.Length < 32) yield return new ValidationResult("Streaming:WebhookSecret must be set (32+ characters).", [nameof(WebhookSecret)]);
    }
}
