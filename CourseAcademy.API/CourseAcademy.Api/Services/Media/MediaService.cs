using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Api.Services.Media;

/// <summary>
/// Uploads, status, playback and downloads of course files (streaming spec, sections 4 and 7). Every call to the
/// Streaming service comes after the permission and course checks; Streaming itself makes no access decisions.
/// </summary>
public interface IMediaService
{
    /// <summary>Starts an upload for a course the user may edit. The browser then sends the bytes to the returned link.</summary>
    Task<MediaUploadDto> CreateUploadAsync(CreateUploadRequest request, CancellationToken ct);

    /// <summary>Called by the browser after sending the bytes; Streaming starts processing.</summary>
    Task<MediaAssetDto> CompleteUploadAsync(int mediaAssetId, CancellationToken ct);

    Task<MediaAssetDto> GetAsync(int mediaAssetId, CancellationToken ct);

    /// <summary>A short-lived link to an uploaded video, for course access holders (or anyone for a preview video).</summary>
    Task<PlaybackDto> GetVideoPlaybackAsync(int videoId, CancellationToken ct);

    Task<DownloadDto> GetDocumentDownloadAsync(int documentId, CancellationToken ct);

    Task<DownloadDto> GetMaterialDownloadAsync(int materialId, CancellationToken ct);

    /// <summary>Removes a file no lesson or material uses any more.</summary>
    Task DeleteAsync(int mediaAssetId, CancellationToken ct);
}

public sealed class MediaService(
    IMediaAssetRepository assets,
    ICourseRepository courses,
    IEnrollmentRepository enrollments,
    IStreamingClient streaming,
    MediaDeletionSignal deletions,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IOptions<StreamingSettings> settings,
    TimeProvider clock,
    ILogger<MediaService> logger) : IMediaService
{
    /// <summary>How long an in-progress asset may go without news before its status is asked from Streaming.</summary>
    private static readonly TimeSpan StatusRefreshAfter = TimeSpan.FromSeconds(30);

    public async Task<MediaUploadDto> CreateUploadAsync(CreateUploadRequest request, CancellationToken ct)
    {
        if (await courses.GetByIdAsync(request.CourseId, ct) is null) throw new NotFoundException("Course not found.");
        await currentUser.EnsureCanAsync(PermissionCodes.CoursesEdit, request.CourseId, ct);
        var mimeType = request.MimeType.Trim().ToLowerInvariant();
        var error = MediaRules.UploadError(request.Kind, mimeType, request.SizeBytes);
        if (error is not null) throw new BusinessRuleException(error, "file");

        var asset = new MediaAsset
        {
            CourseId = request.CourseId,
            UploadedByUserId = currentUser.RequireUserId(),
            MediaKindId = MediaKindCodes.Id(request.Kind),
            MediaAssetStatusId = MediaStatusCodes.Id(MediaStatusCodes.PendingUpload),
            FileName = Path.GetFileName(request.FileName.Trim()),
            MimeType = mimeType,
            SizeBytes = request.SizeBytes,
        };
        await assets.AddAsync(asset, ct);
        await unitOfWork.SaveChangesAsync(ct);

        StreamingUpload upload;
        try
        {
            upload = await streaming.CreateUploadAsync(new StreamingUploadRequest(
                request.Kind, asset.FileName, asset.MimeType, asset.SizeBytes, asset.Id.ToString(System.Globalization.CultureInfo.InvariantCulture)), ct);
        }
        catch
        {
            // Nothing was created at Streaming, so the row has no purpose.
            assets.Remove(asset);
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        asset.AssetId = upload.AssetId;
        asset.UploadExpiresAt = upload.ExpiresAt;
        await unitOfWork.SaveChangesAsync(ct);
        return new MediaUploadDto(asset.Id, upload.UploadUrl, upload.UploadMethod, upload.Headers, upload.ExpiresAt);
    }

    public async Task<MediaAssetDto> CompleteUploadAsync(int mediaAssetId, CancellationToken ct)
    {
        var asset = await GetManagedAsync(mediaAssetId, ct);
        if (asset.MediaAssetStatusId != MediaStatusCodes.Id(MediaStatusCodes.PendingUpload)) return ToDto(asset);
        if (asset.UploadExpiresAt < clock.GetUtcNow().UtcDateTime) throw new ConflictException("The upload link expired. Upload the file again.");

        var status = await streaming.CompleteUploadAsync(asset.AssetId!, ct);
        // Webhooks may already have moved it further; only move forward from "waiting for upload".
        if (asset.MediaAssetStatusId == MediaStatusCodes.Id(MediaStatusCodes.PendingUpload))
        {
            asset.MediaAssetStatusId = MediaStatusCodes.Id(status is MediaStatusCodes.Queued or MediaStatusCodes.Processing or MediaStatusCodes.Ready
                ? status : MediaStatusCodes.Uploaded);
        }
        await unitOfWork.SaveChangesAsync(ct);
        return ToDto(asset);
    }

    public async Task<MediaAssetDto> GetAsync(int mediaAssetId, CancellationToken ct)
    {
        var asset = await GetManagedAsync(mediaAssetId, ct);
        await RefreshIfStaleAsync(asset, ct);
        return ToDto(asset);
    }

    public async Task<PlaybackDto> GetVideoPlaybackAsync(int videoId, CancellationToken ct)
    {
        var item = await assets.FindVideoAsync(videoId, ct) ?? throw new NotFoundException("Video not found.");
        var viewer = await EnsureCanOpenAsync(item, "Enroll in this course to watch this video.", ct);
        var asset = ReadyAsset(item, "This video plays from its link.", "This video is still being processed. Please try again in a few minutes.");
        var link = await streaming.CreatePlaybackAsync(asset.AssetId!, viewer, settings.Value.LinkMinutes * 60, ct);
        return new PlaybackDto(link.Url, link.Format, link.ExpiresAt, asset.DurationSeconds);
    }

    public async Task<DownloadDto> GetDocumentDownloadAsync(int documentId, CancellationToken ct)
    {
        var item = await assets.FindDocumentAsync(documentId, ct) ?? throw new NotFoundException("Document not found.");
        return await DownloadAsync(item, ct);
    }

    public async Task<DownloadDto> GetMaterialDownloadAsync(int materialId, CancellationToken ct)
    {
        var item = await assets.FindMaterialAsync(materialId, ct) ?? throw new NotFoundException("Material not found.");
        return await DownloadAsync(item, ct);
    }

    public async Task DeleteAsync(int mediaAssetId, CancellationToken ct)
    {
        var asset = await GetManagedAsync(mediaAssetId, ct);
        if (MediaStatusCodes.Removed(MediaStatusCodes.Code(asset.MediaAssetStatusId))) return;
        if ((await assets.ReferencedIdsAsync([asset.Id], ct)).Count > 0)
        {
            throw new ConflictException("This file is still used in the course. Remove it from the lesson first.");
        }
        asset.MediaAssetStatusId = MediaStatusCodes.Id(MediaStatusCodes.Deleting);
        await unitOfWork.SaveChangesAsync(ct);
        deletions.Notify();
    }

    private async Task<DownloadDto> DownloadAsync(MediaItemContext item, CancellationToken ct)
    {
        var viewer = await EnsureCanOpenAsync(item, "Enroll in this course to open this file.", ct);
        var asset = ReadyAsset(item, "This file opens from its link.", "This file is still being processed. Please try again in a few minutes.");
        var link = await streaming.CreateDownloadAsync(asset.AssetId!, viewer, settings.Value.LinkMinutes * 60, asset.FileName, ct);
        return new DownloadDto(link.Url, link.ExpiresAt);
    }

    /// <summary>
    /// Course access (BR-ENR-03): managing the course or an active enrollment; preview items are open to everyone.
    /// An unpublished course looks missing to everyone else (BR-CAT-02). Returns the viewer reference for Streaming logs.
    /// </summary>
    private async Task<string> EnsureCanOpenAsync(MediaItemContext item, string enrollMessage, CancellationToken ct)
    {
        var access = await currentUser.TryGetAccessAsync(ct);
        var hasAccess = access is not null && (access.Can(PermissionCodes.CoursesView, item.CourseId)
            || (await enrollments.GetForUserCourseAsync(access.UserId, item.CourseId, ct))?.Status.Code == EnrollmentStatusCodes.Active);
        if (!item.CourseIsPublished && !hasAccess) throw new NotFoundException("Course not found.");
        if (!hasAccess && !item.IsPreview)
        {
            if (access is null) throw new UnauthorizedException();
            throw new ForbiddenException(enrollMessage);
        }
        return access is null ? "guest" : $"user:{access.UserId}";
    }

    private static MediaAsset ReadyAsset(MediaItemContext item, string linkMessage, string processingMessage)
    {
        if (item.Asset is null)
        {
            if (item.ExternalUrl is not null) throw new BusinessRuleException(linkMessage);
            throw new NotFoundException("No file has been added yet.");
        }
        return MediaStatusCodes.Code(item.Asset.MediaAssetStatusId) switch
        {
            MediaStatusCodes.Ready => item.Asset,
            MediaStatusCodes.Failed => throw new ConflictException("This file could not be processed."),
            var code when MediaStatusCodes.InProgress(code) => throw new ConflictException(processingMessage),
            _ => throw new NotFoundException("No file has been added yet."),
        };
    }

    /// <summary>A tracked asset of a course the user may edit.</summary>
    private async Task<MediaAsset> GetManagedAsync(int mediaAssetId, CancellationToken ct)
    {
        var asset = await assets.GetByIdAsync(mediaAssetId, ct) ?? throw new NotFoundException("File not found.");
        await currentUser.EnsureCanAsync(PermissionCodes.CoursesEdit, asset.CourseId, ct);
        return asset;
    }

    /// <summary>Fallback when webhooks are late or lost: asks Streaming for the status of a file stuck in progress.</summary>
    private async Task RefreshIfStaleAsync(MediaAsset asset, CancellationToken ct)
    {
        var code = MediaStatusCodes.Code(asset.MediaAssetStatusId);
        if (asset.AssetId is null || code == MediaStatusCodes.PendingUpload || !MediaStatusCodes.InProgress(code)) return;
        if (clock.GetUtcNow().UtcDateTime - (asset.UpdatedAt ?? asset.CreatedAt) < StatusRefreshAfter) return;
        try
        {
            var remote = await streaming.GetAssetAsync(asset.AssetId, ct);
            if (remote.Status is not (MediaStatusCodes.Ready or MediaStatusCodes.Failed)) return;
            asset.MediaAssetStatusId = MediaStatusCodes.Id(remote.Status);
            asset.DurationSeconds = remote.DurationSeconds ?? asset.DurationSeconds;
            if (remote.Status == MediaStatusCodes.Ready) asset.ReadyAt = clock.GetUtcNow().UtcDateTime;
            if (remote.Error is not null)
            {
                asset.ErrorCode = remote.Error.Code[..Math.Min(remote.Error.Code.Length, 100)];
                asset.ErrorMessage = remote.Error.Message[..Math.Min(remote.Error.Message.Length, 500)];
            }
            await unitOfWork.SaveChangesAsync(ct);
        }
        catch (AppException exception)
        {
            // The stored status is still shown; the next poll tries again.
            logger.LogInformation(exception, "Could not refresh the status of asset {AssetId}.", asset.AssetId);
        }
    }

    public static MediaAssetDto ToDto(MediaAsset a) => new(
        a.Id, a.CourseId, MediaKindCodes.Code(a.MediaKindId), MediaStatusCodes.Code(a.MediaAssetStatusId),
        a.FileName, a.MimeType, a.SizeBytes, a.DurationSeconds, a.ErrorMessage);
}
