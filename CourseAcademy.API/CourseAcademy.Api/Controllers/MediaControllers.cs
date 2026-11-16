using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services.Media;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Api.Controllers;

// Permission and course-access checks are made in the services.

/// <summary>Uploading course files and opening them (short-lived playback and download links).</summary>
[Authorize]
public sealed class MediaController(IMediaService media) : ApiControllerBase
{
    /// <summary>Starts an upload; the browser then sends the file to the returned link and calls complete.</summary>
    [HttpPost("uploads")]
    [ProducesResponseType<ApiResponse<MediaUploadDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateUpload(CreateUploadRequest request, CancellationToken ct) => Created(await media.CreateUploadAsync(request, ct));

    [HttpPost("uploads/{id:int}/complete")]
    [ProducesResponseType<ApiResponse<MediaAssetDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteUpload(int id, CancellationToken ct) => Success(await media.CompleteUploadAsync(id, ct));

    [HttpGet("assets/{id:int}")]
    [ProducesResponseType<ApiResponse<MediaAssetDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsset(int id, CancellationToken ct) => Success(await media.GetAsync(id, ct));

    [HttpDelete("assets/{id:int}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteAsset(int id, CancellationToken ct)
    {
        await media.DeleteAsync(id, ct);
        return Success();
    }

    /// <summary>Anonymous visitors may play free preview videos.</summary>
    [HttpPost("videos/{id:int}/playback")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<PlaybackDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Playback(int id, CancellationToken ct) => Success(await media.GetVideoPlaybackAsync(id, ct));

    /// <summary>Anonymous visitors may open the documents of free preview videos.</summary>
    [HttpPost("documents/{id:int}/download")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<DownloadDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> DocumentDownload(int id, CancellationToken ct) => Success(await media.GetDocumentDownloadAsync(id, ct));

    [HttpPost("materials/{id:int}/download")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<DownloadDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> MaterialDownload(int id, CancellationToken ct) => Success(await media.GetMaterialDownloadAsync(id, ct));
}

/// <summary>Status reports from the Streaming service. Anonymous: every call is verified by its HMAC signature.</summary>
[AllowAnonymous]
[Route("api/v1/internal/streaming/webhooks")]
public sealed class StreamingWebhooksController(IStreamingWebhookService webhooks) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Receive(CancellationToken ct)
    {
        // The signature covers the raw body, so read it as text before anything parses it.
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync(ct);
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        return await webhooks.HandleAsync(body, headers, ct) switch
        {
            WebhookResult.Accepted => Success(),
            WebhookResult.Unauthorized => Unauthorized(ApiResponse.Fail("The webhook signature is not valid.")),
            _ => BadRequest(ApiResponse.Fail("The webhook body is not valid.")),
        };
    }
}

/// <summary>
/// Development only (Streaming:Mode = Fake): stands in for the Streaming service's storage. Receives uploaded bytes
/// and serves files through signed, expiring links, like real object storage would.
/// </summary>
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route(FakeStreamingClient.RoutePrefix)]
public sealed class FakeStreamingController(IOptions<StreamingSettings> settings, IServiceProvider services, TimeProvider clock) : ApiControllerBase
{
    [HttpPut("uploads/{assetId}")]
    [DisableRequestSizeLimit]
    public async Task<IActionResult> Upload(string assetId, [FromQuery] long expires, [FromQuery] string? signature, CancellationToken ct)
    {
        var store = Store();
        if (!store.Verify("upload", assetId, expires, signature, clock.GetUtcNow())) return Forbidden();
        var asset = store.Get(assetId);
        if (asset is null || asset.Status != "awaiting_upload") return Forbidden();

        var limit = asset.Kind == MediaKindCodes.Video ? MediaRules.MaxVideoBytes : MediaRules.MaxDocumentBytes;
        var path = store.FilePath(assetId);
        long written = 0;
        await using (var file = System.IO.File.Create(path))
        {
            var buffer = new byte[81920];
            int read;
            while ((read = await Request.Body.ReadAsync(buffer, ct)) > 0)
            {
                written += read;
                if (written > limit) break;
                await file.WriteAsync(buffer.AsMemory(0, read), ct);
            }
        }
        if (written > limit || written == 0)
        {
            System.IO.File.Delete(path);
            return BadRequest(ApiResponse.Fail(written == 0 ? "The file is empty." : "The file is larger than allowed."));
        }
        store.Update(assetId, a => a.SizeBytes = written);
        return Success();
    }

    [HttpGet("files/{assetId}")]
    public IActionResult GetFile(string assetId, [FromQuery] long expires, [FromQuery] string? signature, [FromQuery] bool download = false)
    {
        var store = Store();
        if (!store.Verify("file", assetId, expires, signature, clock.GetUtcNow())) return Forbidden();
        var asset = store.Get(assetId);
        var path = store.FilePath(assetId);
        if (asset?.Status != MediaStatusCodes.Ready || !System.IO.File.Exists(path)) return NotFound(ApiResponse.Fail("File not found."));
        // Range requests let the video player seek.
        return PhysicalFile(path, asset.MimeType, download ? asset.FileName : null, enableRangeProcessing: true);
    }

    /// <summary>The fake store, or a 404 when the API talks to the real Streaming service.</summary>
    private FakeStreamingStore Store() =>
        settings.Value.IsFake ? services.GetRequiredService<FakeStreamingStore>() : throw new NotFoundException();

    private ObjectResult Forbidden() =>
        StatusCode(StatusCodes.Status403Forbidden, ApiResponse.Fail("This link is not valid or has expired."));
}
