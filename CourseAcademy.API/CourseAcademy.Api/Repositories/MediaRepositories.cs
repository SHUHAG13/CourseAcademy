using CourseAcademy.Api.Data;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Services.Media;
using Microsoft.EntityFrameworkCore;

namespace CourseAcademy.Api.Repositories;

/// <summary>A curriculum item that holds media, with what access checks need.</summary>
public sealed record MediaItemContext(int CourseId, bool CourseIsPublished, string Title, string? ExternalUrl, MediaAsset? Asset, bool IsPreview);

public interface IMediaAssetRepository : IRepository<MediaAsset>
{
    /// <summary>Tracked asset with kind and status, found by the Streaming asset id.</summary>
    Task<MediaAsset?> GetByAssetIdAsync(string assetId, CancellationToken ct);

    /// <summary>Tracked assets with the given ids.</summary>
    Task<List<MediaAsset>> ListByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);

    /// <summary>Ids among <paramref name="ids"/> used by a video, document or material that is not deleted.</summary>
    Task<HashSet<int>> ReferencedIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct);

    /// <summary>Tracked assets of one course that are not yet being removed.</summary>
    Task<List<MediaAsset>> ListActiveForCourseAsync(int courseId, CancellationToken ct);

    /// <summary>Tracked assets marked for deletion whose request has not been sent to Streaming yet.</summary>
    Task<List<MediaAsset>> ListDeletionsToSendAsync(int deletingStatusId, int take, CancellationToken ct);

    Task<MediaItemContext?> FindVideoAsync(int videoId, CancellationToken ct);

    /// <summary>IsPreview: the document belongs to a free preview video, so it is open to visitors (BR-CAT-13).</summary>
    Task<MediaItemContext?> FindDocumentAsync(int documentId, CancellationToken ct);

    Task<MediaItemContext?> FindMaterialAsync(int materialId, CancellationToken ct);
}

public sealed class MediaAssetRepository(AppDbContext db) : Repository<MediaAsset>(db), IMediaAssetRepository
{
    public Task<MediaAsset?> GetByAssetIdAsync(string assetId, CancellationToken ct) =>
        Set.FirstOrDefaultAsync(a => a.AssetId == assetId, ct);

    public Task<List<MediaAsset>> ListByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct) =>
        Set.Where(a => ids.Contains(a.Id)).ToListAsync(ct);

    public async Task<HashSet<int>> ReferencedIdsAsync(IReadOnlyCollection<int> ids, CancellationToken ct)
    {
        var videos = await Db.SectionVideos.Where(v => v.MediaAssetId != null && ids.Contains(v.MediaAssetId.Value)).Select(v => v.MediaAssetId!.Value).ToListAsync(ct);
        var documents = await Db.SectionDocuments.Where(d => d.MediaAssetId != null && ids.Contains(d.MediaAssetId.Value)).Select(d => d.MediaAssetId!.Value).ToListAsync(ct);
        var materials = await Db.Materials.Where(m => m.MediaAssetId != null && ids.Contains(m.MediaAssetId.Value)).Select(m => m.MediaAssetId!.Value).ToListAsync(ct);
        return [.. videos, .. documents, .. materials];
    }

    public Task<List<MediaAsset>> ListActiveForCourseAsync(int courseId, CancellationToken ct)
    {
        var removed = new[] { MediaStatusCodes.Id(MediaStatusCodes.Deleting), MediaStatusCodes.Id(MediaStatusCodes.Deleted) };
        return Set.Where(a => a.CourseId == courseId && !removed.Contains(a.MediaAssetStatusId)).ToListAsync(ct);
    }

    public Task<List<MediaAsset>> ListDeletionsToSendAsync(int deletingStatusId, int take, CancellationToken ct) =>
        Set.Where(a => a.MediaAssetStatusId == deletingStatusId && a.DeleteRequestedAt == null).OrderBy(a => a.Id).Take(take).ToListAsync(ct);

    public Task<MediaItemContext?> FindVideoAsync(int videoId, CancellationToken ct) =>
        (from v in Db.SectionVideos.AsNoTracking().Include(v => v.Asset)
         join s in Db.Sections on v.SectionId equals s.Id
         join c in Db.Courses on s.CourseId equals c.Id
         where v.Id == videoId
         select new MediaItemContext(c.Id, c.IsPublished, v.Title, v.ExternalUrl, v.Asset, v.IsPreview))
        .FirstOrDefaultAsync(ct);

    public Task<MediaItemContext?> FindDocumentAsync(int documentId, CancellationToken ct) =>
        (from d in Db.SectionDocuments.AsNoTracking().Include(d => d.Asset).Include(d => d.Video)
         join s in Db.Sections on d.SectionId equals s.Id
         join c in Db.Courses on s.CourseId equals c.Id
         where d.Id == documentId
         select new MediaItemContext(c.Id, c.IsPublished, d.Title, d.ExternalUrl, d.Asset, d.Video != null && d.Video.IsPreview))
        .FirstOrDefaultAsync(ct);

    public Task<MediaItemContext?> FindMaterialAsync(int materialId, CancellationToken ct) =>
        (from m in Db.Materials.AsNoTracking().Include(m => m.Asset)
         join c in Db.Courses on m.CourseId equals c.Id
         where m.Id == materialId
         select new MediaItemContext(c.Id, c.IsPublished, m.Title, m.ExternalUrl, m.Asset, false))
        .FirstOrDefaultAsync(ct);
}
