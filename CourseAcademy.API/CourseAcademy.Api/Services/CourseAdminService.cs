using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using CourseAcademy.Api.Services.Media;
using CourseAcademy.Api.Validators;

namespace CourseAcademy.Api.Services;

/// <summary>Creating, editing, publishing and deleting courses and their curriculum (course-management module).</summary>
public interface ICourseAdminService
{
    Task<CourseDetailDto> CreateAsync(CourseUpsertRequest request, CancellationToken ct);
    Task<CourseDetailDto> UpdateAsync(int courseId, CourseUpsertRequest request, CancellationToken ct);
    Task<CourseDetailDto> SetPublishedAsync(int courseId, bool isPublished, CancellationToken ct);
    Task<CourseDetailDto> SaveCurriculumAsync(int courseId, CurriculumRequest request, CancellationToken ct);
    Task DeleteAsync(int courseId, CancellationToken ct);
    Task<SlugAvailabilityDto> IsSlugAvailableAsync(string slug, int? exceptId, CancellationToken ct);
}

public sealed class CourseAdminService(
    ICourseRepository courses,
    IInstructorRepository instructors,
    ILookupRepository lookups,
    IRepository<CourseOutcome> outcomes,
    IRepository<CourseRequirement> requirements,
    IRepository<LiveSession> sessions,
    IRepository<Material> materials,
    IRepository<CourseModule> modules,
    IRepository<Submodule> submodules,
    IRepository<Section> sectionRepository,
    IRepository<SectionVideo> videos,
    IRepository<SectionDocument> documents,
    IRepository<CourseGrant> grants,
    IEnrollmentRepository enrollments,
    IPostRepository posts,
    ICommentRepository comments,
    IMediaAssetRepository mediaAssets,
    MediaDeletionSignal mediaDeletions,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock) : ICourseAdminService
{
    public const string NeedsSectionsMessage = "A published course needs at least one section. Save it as a draft, then add the curriculum.";
    public const string NeedsSessionsMessage = "A published Live or Hybrid course needs at least one session.";
    public const string ReplaceLinkMessage = "A video link can no longer be changed. Upload the video file instead.";
    private const string StaleMessage = "Something in this course changed in the meantime. Reload the page and try again.";
    private const string FileMissingMessage = "An uploaded file was not found. Upload it again.";

    // ── Course details ──────────────────────────────────────────

    public async Task<CourseDetailDto> CreateAsync(CourseUpsertRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.CoursesCreate, null, ct);
        var course = new Course();
        await ApplyDetailsAsync(course, request, ct);
        // A new course has no curriculum yet, so it can only be published once lessons are added (BR-CMS-04).
        if (course.IsPublished) throw new BusinessRuleException(NeedsSectionsMessage, "isPublished");

        // Files are uploaded for an existing course, so a new course cannot bring any.
        if ((request.Materials ?? []).Any(m => m.MediaAssetId is not null))
        {
            throw new BusinessRuleException("Save the course first, then upload its files.", "materials");
        }

        await courses.AddAsync(course, ct);
        ReplaceTexts(course, request);
        foreach (var (input, index) in (request.Schedule ?? []).Select((s, i) => (s, i))) course.Sessions.Add(NewSession(input, index));
        foreach (var (input, index) in (request.Materials ?? []).Select((m, i) => (m, i))) course.Materials.Add(await NewMaterialAsync(input, index, new Dictionary<int, MediaAsset>(), ct));
        await unitOfWork.SaveChangesAsync(ct);
        return await LoadDetailAsync(course.Id, ct);
    }

    public async Task<CourseDetailDto> UpdateAsync(int courseId, CourseUpsertRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.CoursesEdit, courseId, ct);
        var course = await LoadTrackedAsync(courseId, ct);
        var formatCode = await ApplyDetailsAsync(course, request, ct);

        foreach (var old in course.Outcomes.ToList()) outcomes.Remove(old);
        foreach (var old in course.Requirements.ToList()) requirements.Remove(old);
        course.Outcomes.Clear();
        course.Requirements.Clear();
        ReplaceTexts(course, request);
        SyncSessions(course, request.Schedule ?? []);
        var heldBefore = course.Materials.Where(m => m.MediaAssetId is not null).Select(m => m.MediaAssetId!.Value).ToHashSet();
        await SyncMaterialsAsync(course, request.Materials ?? [], heldBefore, ct);

        await EnsurePublishableAsync(course, formatCode, request.Schedule?.Count ?? 0, ct);
        await unitOfWork.SaveChangesAsync(ct);
        if (await ReleaseUnusedAsync(heldBefore, ct))
        {
            await unitOfWork.SaveChangesAsync(ct);
            mediaDeletions.Notify();
        }
        return await LoadDetailAsync(course.Id, ct);
    }

    public async Task<CourseDetailDto> SetPublishedAsync(int courseId, bool isPublished, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.CoursesEdit, courseId, ct);
        var course = await LoadTrackedAsync(courseId, ct);
        course.IsPublished = isPublished;
        await EnsurePublishableAsync(course, course.Format.Code, course.Sessions.Count, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await LoadDetailAsync(course.Id, ct);
    }

    public async Task DeleteAsync(int courseId, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.CoursesDelete, courseId, ct);
        var course = await courses.GetByIdAsync(courseId, ct) ?? throw new NotFoundException("Course not found.");
        courses.Remove(course);
        // Its enrollments go with it (BR-CMS-13); payments stay as records, and so do certificates, which still verify.
        foreach (var enrollment in await enrollments.ListForCourseAsync(courseId, ct)) enrollments.Remove(enrollment);
        // Its community goes too (BR-COM-15).
        foreach (var comment in await comments.ListForCourseAsync(courseId, ct)) comments.Remove(comment);
        foreach (var post in await posts.ListForCourseAsync(courseId, ct)) posts.Remove(post);
        // Grants on a deleted course mean nothing (BR-ACL-14).
        foreach (var grant in await grants.ListAsync(g => g.CourseId == courseId, ct))
        {
            var tracked = await grants.GetByIdAsync(grant.Id, ct);
            if (tracked is not null) grants.Remove(tracked);
        }
        // Its uploaded files are deleted from Streaming.
        var files = await mediaAssets.ListActiveForCourseAsync(courseId, ct);
        foreach (var file in files) file.MediaAssetStatusId = MediaStatusCodes.Id(MediaStatusCodes.Deleting);
        await unitOfWork.SaveChangesAsync(ct);
        if (files.Count > 0) mediaDeletions.Notify();
    }

    public async Task<SlugAvailabilityDto> IsSlugAvailableAsync(string slug, int? exceptId, CancellationToken ct)
    {
        var access = await currentUser.GetAccessAsync(ct);
        if (!access.Can(PermissionCodes.CoursesCreate) && !access.CanOnAnyCourse(PermissionCodes.CoursesEdit)) throw new ForbiddenException();
        return new SlugAvailabilityDto(!await courses.SlugExistsAsync(slug.Trim(), exceptId, ct));
    }

    /// <summary>Copies the request onto the course and returns its format code.</summary>
    private async Task<string> ApplyDetailsAsync(Course course, CourseUpsertRequest r, CancellationToken ct)
    {
        var slug = r.Slug.Trim();
        if (await courses.SlugExistsAsync(slug, course.Id == 0 ? null : course.Id, ct))
        {
            throw new BusinessRuleException("Another course already uses this slug.", "slug");
        }
        var category = await lookups.FindByCodeAsync<Category>(r.CategoryCode, ct) ?? throw new BusinessRuleException("Choose a category.", "categoryCode");
        var format = await lookups.FindByCodeAsync<CourseFormat>(r.FormatCode, ct) ?? throw new BusinessRuleException("Choose a format.", "formatCode");
        var level = await lookups.FindByCodeAsync<CourseLevel>(r.LevelCode, ct) ?? throw new BusinessRuleException("Choose a level.", "levelCode");
        CourseBadge? badge = null;
        if (!string.IsNullOrWhiteSpace(r.BadgeCode))
        {
            badge = await lookups.FindByCodeAsync<CourseBadge>(r.BadgeCode, ct) ?? throw new BusinessRuleException("Choose a badge.", "badgeCode");
        }
        if (await instructors.GetByIdAsync(r.InstructorId, ct) is null) throw new BusinessRuleException("Choose an instructor.", "instructorId");

        course.Slug = slug;
        course.Title = r.Title.Trim();
        course.Subtitle = r.Subtitle.Trim();
        course.Description = r.Description.Trim();
        course.CategoryId = category.Id;
        course.CourseFormatId = format.Id;
        course.CourseLevelId = level.Id;
        course.CourseBadgeId = badge?.Id;
        course.InstructorId = r.InstructorId;
        course.Language = r.Language.Trim();
        course.DurationHours = r.DurationHours;
        course.OriginalPrice = r.OriginalPrice;
        course.Price = r.Price;
        course.CoverLabel = r.CoverLabel.Trim();
        course.CoverIcon = r.CoverIcon.Trim();
        course.ImageUrl = string.IsNullOrWhiteSpace(r.ImageUrl) ? null : r.ImageUrl.Trim();
        course.IsFeatured = r.IsFeatured;
        course.IsPublished = r.IsPublished;
        // Shown as "Updated" on the course page (BR-CMS-11).
        course.ContentUpdatedMonth = clock.GetUtcNow().UtcDateTime.ToString("yyyy-MM");
        return format.Code;
    }

    private static void ReplaceTexts(Course course, CourseUpsertRequest r)
    {
        var order = 0;
        foreach (var text in r.Outcomes.Select(t => t.Trim()).Where(t => t.Length > 0)) course.Outcomes.Add(new CourseOutcome { Text = text, SortOrder = ++order });
        order = 0;
        foreach (var text in (r.Requirements ?? []).Select(t => t.Trim()).Where(t => t.Length > 0)) course.Requirements.Add(new CourseRequirement { Text = text, SortOrder = ++order });
    }

    private static LiveSession NewSession(LiveSessionInput input, int index)
    {
        var session = new LiveSession();
        ApplySession(session, input, index);
        return session;
    }

    private static void ApplySession(LiveSession session, LiveSessionInput input, int index)
    {
        BangladeshTime.TryParseToUtc(input.StartsAt, out var startsAt);
        session.Title = input.Title.Trim();
        session.StartsAtUtc = startsAt;
        session.DurationMinutes = input.DurationMinutes;
        session.MeetingUrl = input.MeetingUrl.Trim();
        session.SortOrder = index + 1;
    }

    private void SyncSessions(Course course, IReadOnlyList<LiveSessionInput> inputs)
    {
        var existing = course.Sessions.ToDictionary(s => s.Id);
        var kept = new HashSet<int>();
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            if (input.Id is { } id)
            {
                if (!existing.TryGetValue(id, out var session) || !kept.Add(id)) throw new BusinessRuleException(StaleMessage);
                ApplySession(session, input, i);
            }
            else
            {
                course.Sessions.Add(NewSession(input, i));
            }
        }
        foreach (var removed in existing.Values.Where(s => !kept.Contains(s.Id))) sessions.Remove(removed);
    }

    private async Task<Material> NewMaterialAsync(MaterialInput input, int index, IReadOnlyDictionary<int, MediaAsset> files, CancellationToken ct)
    {
        var material = new Material();
        await ApplyMaterialAsync(material, input, index, files, ct);
        return material;
    }

    private async Task ApplyMaterialAsync(Material material, MaterialInput input, int index, IReadOnlyDictionary<int, MediaAsset> files, CancellationToken ct)
    {
        var type = await lookups.FindByCodeAsync<MaterialType>(input.TypeCode, ct) ?? throw new BusinessRuleException("Choose a material type.", "materials");
        material.Title = input.Title.Trim();
        material.MaterialTypeId = type.Id;
        // The request is the full state: an upload, a link, or (for a file material) nothing yet.
        material.MediaAssetId = input.MediaAssetId is { } fileId && files.ContainsKey(fileId) ? fileId : null;
        material.ExternalUrl = material.MediaAssetId is null && !string.IsNullOrWhiteSpace(input.Url) ? input.Url.Trim() : null;
        material.SortOrder = index + 1;
    }

    private async Task SyncMaterialsAsync(Course course, IReadOnlyList<MaterialInput> inputs, IReadOnlySet<int> heldBefore, CancellationToken ct)
    {
        var files = await LoadAttachableAsync(course.Id,
            inputs.Where(m => m.MediaAssetId is not null).Select(m => (m.MediaAssetId!.Value, MediaKindCodes.Document)).ToList(), heldBefore, ct);
        var existing = course.Materials.ToDictionary(m => m.Id);
        var kept = new HashSet<int>();
        for (var i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            if (input.Id is { } id)
            {
                if (!existing.TryGetValue(id, out var material) || !kept.Add(id)) throw new BusinessRuleException(StaleMessage);
                await ApplyMaterialAsync(material, input, i, files, ct);
            }
            else
            {
                course.Materials.Add(await NewMaterialAsync(input, i, files, ct));
            }
        }
        foreach (var removed in existing.Values.Where(m => !kept.Contains(m.Id))) materials.Remove(removed);
    }

    // ── Uploaded files ──────────────────────────────────────────

    /// <summary>
    /// The uploaded files a save attaches, checked: each belongs to this course, has the right kind, has been
    /// uploaded and not failed or deleted, is used once, and is not already used outside the items being saved
    /// (<paramref name="heldInScope"/>).
    /// </summary>
    private async Task<Dictionary<int, MediaAsset>> LoadAttachableAsync(
        int courseId, IReadOnlyList<(int Id, string Kind)> wanted, IReadOnlySet<int> heldInScope, CancellationToken ct)
    {
        if (wanted.Count == 0) return [];
        var ids = wanted.Select(w => w.Id).ToList();
        if (ids.Distinct().Count() != ids.Count) throw new BusinessRuleException("The same uploaded file is used twice. Upload it again for the second item.");

        var found = (await mediaAssets.ListByIdsAsync(ids, ct)).ToDictionary(a => a.Id);
        var usedElsewhere = await mediaAssets.ReferencedIdsAsync(ids, ct);
        usedElsewhere.ExceptWith(heldInScope);
        foreach (var (id, kind) in wanted)
        {
            if (!found.TryGetValue(id, out var file) || file.CourseId != courseId) throw new BusinessRuleException(FileMissingMessage);
            if (MediaKindCodes.Code(file.MediaKindId) != kind)
            {
                throw new BusinessRuleException(kind == MediaKindCodes.Video ? "Upload a video file for a video." : "Upload a PDF, DOCX or PPTX file here.");
            }
            var status = MediaStatusCodes.Code(file.MediaAssetStatusId);
            if (status == MediaStatusCodes.Failed) throw new BusinessRuleException($"\"{file.FileName}\" could not be processed. Upload it again.");
            if (status == MediaStatusCodes.PendingUpload || MediaStatusCodes.Removed(status)) throw new BusinessRuleException(FileMissingMessage);
            if (usedElsewhere.Contains(id)) throw new BusinessRuleException($"\"{file.FileName}\" is already used elsewhere in this course.");
        }
        return found;
    }

    /// <summary>
    /// Marks files that a save detached and that nothing uses any more for deletion from Streaming. The caller saves
    /// and wakes the deletion worker. Returns whether anything was released.
    /// </summary>
    private async Task<bool> ReleaseUnusedAsync(IReadOnlyCollection<int> heldBefore, CancellationToken ct)
    {
        if (heldBefore.Count == 0) return false;
        var stillUsed = await mediaAssets.ReferencedIdsAsync(heldBefore, ct);
        var released = heldBefore.Where(id => !stillUsed.Contains(id)).ToList();
        if (released.Count == 0) return false;
        foreach (var file in await mediaAssets.ListByIdsAsync(released, ct))
        {
            if (!MediaStatusCodes.Removed(MediaStatusCodes.Code(file.MediaAssetStatusId))) file.MediaAssetStatusId = MediaStatusCodes.Id(MediaStatusCodes.Deleting);
        }
        return true;
    }

    /// <summary>A published course must have lessons, and Live or Hybrid courses a live session (BR-CMS-04, 05).</summary>
    private async Task EnsurePublishableAsync(Course course, string formatCode, int sessionCount, CancellationToken ct)
    {
        if (!course.IsPublished) return;
        var sectionCount = (await courses.CountSectionsAsync([course.Id], ct)).GetValueOrDefault(course.Id);
        if (sectionCount == 0) throw new BusinessRuleException(NeedsSectionsMessage, "isPublished");
        if (formatCode != "Pre-recorded" && sessionCount == 0)
        {
            throw new BusinessRuleException(NeedsSessionsMessage, "schedule");
        }
    }

    // ── Curriculum ──────────────────────────────────────────────

    public async Task<CourseDetailDto> SaveCurriculumAsync(int courseId, CurriculumRequest request, CancellationToken ct)
    {
        await currentUser.EnsureCanAsync(PermissionCodes.CoursesEdit, courseId, ct);
        var documentTypes = (await lookups.ListAsync<DocumentType>(ct)).ToDictionary(d => d.Code);
        var published = (await courses.GetByIdAsync(courseId, ct) ?? throw new NotFoundException("Course not found.")).IsPublished;

        var error = CurriculumRules.FirstError(request, documentTypes);
        if (error is null && published && CurriculumRules.SectionCount(request) == 0)
        {
            error = "A published course needs at least one section. Unpublish it first to remove them all.";
        }
        if (error is not null) throw new BusinessRuleException(error);

        var released = await unitOfWork.ExecuteInTransactionAsync(
            token => ApplyCurriculumAsync(courseId, request, documentTypes, token), ct);
        if (released) mediaDeletions.Notify();
        return await LoadDetailAsync(courseId, ct);
    }

    /// <summary>Every uploaded file the request attaches to a video or document, with the kind it must be.</summary>
    private static IEnumerable<(int Id, string Kind)> WantedFiles(IEnumerable<SectionInput> sections)
    {
        foreach (var section in sections)
        {
            foreach (var video in section.Videos ?? []) if (video.MediaAssetId is { } id) yield return (id, MediaKindCodes.Video);
            foreach (var document in section.Documents ?? []) if (document.MediaAssetId is { } id) yield return (id, MediaKindCodes.Document);
            foreach (var file in WantedFiles(section.Topics ?? [])) yield return file;
        }
    }

    /// <summary>
    /// Updates items the request mentions by id, creates the ones without id and soft-deletes the rest.
    /// Saved level by level so new parents have ids before their children point at them.
    /// Returns whether uploaded files were released (and marked for deletion).
    /// </summary>
    private async Task<bool> ApplyCurriculumAsync(int courseId, CurriculumRequest request, IReadOnlyDictionary<string, DocumentType> documentTypes, CancellationToken ct)
    {
        var course = await LoadTrackedAsync(courseId, ct);
        var allSections = await courses.ListSectionsAsync([courseId], tracking: true, ct);
        var moduleById = course.Modules.ToDictionary(m => m.Id);
        var submoduleById = course.Modules.SelectMany(m => m.Submodules).ToDictionary(s => s.Id);
        var sectionById = allSections.ToDictionary(s => s.Id);
        var videoById = allSections.SelectMany(s => s.Videos).ToDictionary(v => v.Id);
        var documentById = allSections.SelectMany(s => s.Documents).ToDictionary(d => d.Id);
        var heldBefore = videoById.Values.Select(v => v.MediaAssetId).Concat(documentById.Values.Select(d => d.MediaAssetId))
            .OfType<int>().ToHashSet();
        var files = await LoadAttachableAsync(courseId,
            WantedFiles(request.Modules.SelectMany(m => m.Submodules ?? []).SelectMany(s => s.Sections ?? [])).ToList(), heldBefore, ct);
        var kept = (Modules: new HashSet<int>(), Submodules: new HashSet<int>(), Sections: new HashSet<int>(), Videos: new HashSet<int>(), Documents: new HashSet<int>());

        static T Existing<T>(Dictionary<int, T> all, HashSet<int> keptIds, int id) =>
            all.TryGetValue(id, out var item) && keptIds.Add(id) ? item : throw new BusinessRuleException(StaleMessage);

        // 1. Modules
        var moduleEntities = new List<(ModuleInput Input, CourseModule Entity)>();
        for (var i = 0; i < request.Modules.Count; i++)
        {
            var input = request.Modules[i];
            var module = input.Id is { } id ? Existing(moduleById, kept.Modules, id) : new CourseModule { CourseId = courseId };
            module.Title = input.Title.Trim();
            module.SortOrder = i + 1;
            if (module.Id == 0) await modules.AddAsync(module, ct);
            moduleEntities.Add((input, module));
        }
        await unitOfWork.SaveChangesAsync(ct);

        // 2. Submodules
        var level = new List<(SectionInput Input, int? SubmoduleId, Section? Parent, int Index)>();
        foreach (var (moduleInput, module) in moduleEntities)
        {
            var created = new List<(SubmoduleInput Input, Submodule Entity)>();
            for (var i = 0; i < moduleInput.Submodules.Count; i++)
            {
                var input = moduleInput.Submodules[i];
                var submodule = input.Id is { } id ? Existing(submoduleById, kept.Submodules, id) : new Submodule();
                submodule.CourseModuleId = module.Id;
                submodule.Title = input.Title.Trim();
                submodule.SortOrder = i + 1;
                if (submodule.Id == 0) await submodules.AddAsync(submodule, ct);
                created.Add((input, submodule));
            }
            await unitOfWork.SaveChangesAsync(ct);
            foreach (var (input, submodule) in created)
            {
                level.AddRange(input.Sections.Select((s, index) => (s, (int?)submodule.Id, (Section?)null, index)));
            }
        }

        // 3. Sections, one depth at a time
        var sectionEntities = new List<(SectionInput Input, Section Entity)>();
        for (var depth = 0; level.Count > 0; depth++)
        {
            var next = new List<(SectionInput, int?, Section?, int)>();
            var current = new List<(SectionInput Input, Section Entity)>();
            foreach (var (input, submoduleId, parent, index) in level)
            {
                var section = input.Id is { } id ? Existing(sectionById, kept.Sections, id) : new Section { CourseId = courseId };
                section.SubmoduleId = submoduleId;
                section.ParentSectionId = parent?.Id;
                section.Depth = depth;
                section.Title = input.Title.Trim();
                section.SortOrder = index + 1;
                if (section.Id == 0) await sectionRepository.AddAsync(section, ct);
                current.Add((input, section));
            }
            await unitOfWork.SaveChangesAsync(ct);
            foreach (var (input, section) in current)
            {
                next.AddRange((input.Topics ?? []).Select((t, index) => (t, (int?)null, (Section?)section, index)));
            }
            sectionEntities.AddRange(current);
            level = next;
        }

        // 4. Videos
        var videoKeys = new Dictionary<(int SectionId, string Key), SectionVideo>();
        foreach (var (input, section) in sectionEntities)
        {
            var inputs = input.Videos ?? [];
            for (var i = 0; i < inputs.Count; i++)
            {
                var v = inputs[i];
                var video = v.Id is { } id ? Existing(videoById, kept.Videos, id) : new SectionVideo();
                video.SectionId = section.Id;
                video.Title = v.Title.Trim();
                video.Minutes = v.Minutes;
                video.IsPreview = v.IsPreview;
                video.SortOrder = i + 1;
                // An upload replaces the stored media. An older video may keep its link but not change it;
                // sending neither keeps the stored media (the rules make new videos bring an upload).
                if (v.MediaAssetId is { } fileId)
                {
                    video.MediaAssetId = fileId;
                    video.ExternalUrl = null;
                }
                else if (!string.IsNullOrWhiteSpace(v.Url) && video.ExternalUrl != v.Url.Trim())
                {
                    throw new BusinessRuleException(ReplaceLinkMessage);
                }
                if (video.Id == 0) await videos.AddAsync(video, ct);
                if (!string.IsNullOrEmpty(v.TempKey)) videoKeys[(section.Id, v.TempKey)] = video;
            }
        }
        await unitOfWork.SaveChangesAsync(ct);

        // 5. Documents
        foreach (var (input, section) in sectionEntities)
        {
            var inputs = input.Documents ?? [];
            for (var i = 0; i < inputs.Count; i++)
            {
                var d = inputs[i];
                var document = d.Id is { } id ? Existing(documentById, kept.Documents, id) : new SectionDocument();
                document.SectionId = section.Id;
                var type = documentTypes[d.TypeCode];
                document.Title = d.Title.Trim();
                document.DocumentTypeId = type.Id;
                document.SortOrder = i + 1;
                // The request is the full state: an upload, a link, or nothing yet.
                if (d.MediaAssetId is { } fileId && MediaRules.DocumentExtensions.GetValueOrDefault(files[fileId].MimeType) != type.FileExtension)
                {
                    throw new BusinessRuleException($"\"{files[fileId].FileName}\" must be a {type.FileExtension} file to match the document type.");
                }
                document.MediaAssetId = d.MediaAssetId;
                document.ExternalUrl = d.MediaAssetId is null && !string.IsNullOrWhiteSpace(d.Url) ? d.Url.Trim() : null;
                document.SectionVideoId =
                    d.VideoTempKey is { Length: > 0 } key && videoKeys.TryGetValue((section.Id, key), out var byKey) ? byKey.Id :
                    d.VideoId is { } videoId && videoById.TryGetValue(videoId, out var byId) && byId.SectionId == section.Id ? byId.Id :
                    d.VideoId is null && d.VideoTempKey is null ? null :
                    throw new BusinessRuleException("A document is attached to a video that is not in its section.");
                if (document.Id == 0) await documents.AddAsync(document, ct);
            }
        }

        // 6. Everything the request no longer lists is removed (soft delete).
        foreach (var d in documentById.Values.Where(d => !kept.Documents.Contains(d.Id))) documents.Remove(d);
        foreach (var v in videoById.Values.Where(v => !kept.Videos.Contains(v.Id))) videos.Remove(v);
        foreach (var s in sectionById.Values.Where(s => !kept.Sections.Contains(s.Id))) sectionRepository.Remove(s);
        foreach (var s in submoduleById.Values.Where(s => !kept.Submodules.Contains(s.Id))) submodules.Remove(s);
        foreach (var m in moduleById.Values.Where(m => !kept.Modules.Contains(m.Id))) modules.Remove(m);

        // 7. Files the curriculum no longer uses are deleted from Streaming.
        await unitOfWork.SaveChangesAsync(ct);
        return await ReleaseUnusedAsync(heldBefore, ct);
    }

    // ── Loading ─────────────────────────────────────────────────

    private async Task<Course> LoadTrackedAsync(int courseId, CancellationToken ct) =>
        (await courses.ListWithDetailsAsync(c => c.Id == courseId, tracking: true, ct)).FirstOrDefault()
        ?? throw new NotFoundException("Course not found.");

    /// <summary>The saved course as its manager sees it.</summary>
    private async Task<CourseDetailDto> LoadDetailAsync(int courseId, CancellationToken ct)
    {
        var course = (await courses.ListWithDetailsAsync(c => c.Id == courseId, tracking: false, ct)).Single();
        var sections = await courses.ListSectionsAsync([courseId], tracking: false, ct);
        var counts = await enrollments.CountByCourseAsync([courseId], ct);
        return CourseMapper.ToDetail(course, sections, new CourseViewer(CanManage: true, HasContentAccess: true),
            counts.TryGetValue(courseId, out var n) ? n : (0, 0));
    }
}

/// <summary>The curriculum rules of BR-CMS-06/07, checked before anything is saved. Messages match the frontend.</summary>
public static class CurriculumRules
{
    public const int MinSectionsPerSubmodule = 2;
    public const int MaxSectionsPerSubmodule = 3;
    private const int MaxTitle = 200;

    public static int SectionCount(CurriculumRequest request) =>
        request.Modules.SelectMany(m => m.Submodules).Sum(s => Count(s.Sections));

    private static int Count(IReadOnlyList<SectionInput>? sections) => (sections ?? []).Sum(s => 1 + Count(s.Topics));

    public static string? FirstError(CurriculumRequest request, IReadOnlyDictionary<string, DocumentType> documentTypes)
    {
        var ids = new HashSet<string>();
        bool Unique(string kind, int? id) => id is null || ids.Add($"{kind}:{id}");

        foreach (var module in request.Modules)
        {
            if (string.IsNullOrWhiteSpace(module.Title)) return "Every module needs a title.";
            if (module.Title.Trim().Length > MaxTitle) return "Titles can be up to 200 characters.";
            if (!Unique("module", module.Id)) return "The curriculum lists an item twice. Reload the page and try again.";
            foreach (var submodule in module.Submodules ?? [])
            {
                if (string.IsNullOrWhiteSpace(submodule.Title)) return "Every submodule needs a title.";
                if (submodule.Title.Trim().Length > MaxTitle) return "Titles can be up to 200 characters.";
                if (!Unique("submodule", submodule.Id)) return "The curriculum lists an item twice. Reload the page and try again.";
                var count = submodule.Sections?.Count ?? 0;
                if (count is < MinSectionsPerSubmodule or > MaxSectionsPerSubmodule) return "Each submodule needs 2 or 3 sections.";
                var error = SectionsError(submodule.Sections!, 0, documentTypes, Unique);
                if (error is not null) return error;
            }
        }
        return null;
    }

    private static string? SectionsError(
        IReadOnlyList<SectionInput> sections, int depth, IReadOnlyDictionary<string, DocumentType> types, Func<string, int?, bool> unique)
    {
        if (sections.Count > 0 && depth > Section.MaxTopicDepth) return "Topics can be nested at most 3 levels deep.";
        foreach (var section in sections)
        {
            if (string.IsNullOrWhiteSpace(section.Title)) return depth > 0 ? "Every topic needs a title." : "Every section needs a title.";
            if (section.Title.Trim().Length > MaxTitle) return "Titles can be up to 200 characters.";
            if (!unique("section", section.Id)) return "The curriculum lists an item twice. Reload the page and try again.";

            var keys = new HashSet<string>();
            var ids = new HashSet<int>();
            foreach (var video in section.Videos ?? [])
            {
                if (string.IsNullOrWhiteSpace(video.Title)) return "Every video needs a title.";
                if (video.Title.Trim().Length > MaxTitle) return "Titles can be up to 200 characters.";
                if (video.Minutes < 1) return "A video needs 1 minute or more.";
                if (!unique("video", video.Id)) return "The curriculum lists an item twice. Reload the page and try again.";
                if (!string.IsNullOrWhiteSpace(video.Url) && !CatalogPatterns.IsUrl(video.Url)) return "Enter a valid http(s) link for every video.";
                if (video.MediaAssetId is not null && !string.IsNullOrWhiteSpace(video.Url)) return "A video has either an uploaded file or its old link, not both.";
                // New videos are uploaded; links stay only on older videos.
                if (video.Id is null && !string.IsNullOrWhiteSpace(video.Url)) return "New videos can't be added as links. Upload the video file.";
                if (video.Id is null && video.MediaAssetId is null) return "Upload a file for every new video.";
                if (video.Id is { } id) ids.Add(id);
                if (!string.IsNullOrEmpty(video.TempKey)) keys.Add(video.TempKey);
            }
            foreach (var document in section.Documents ?? [])
            {
                if (string.IsNullOrWhiteSpace(document.Title)) return "Every document needs a title.";
                if (document.Title.Trim().Length > MaxTitle) return "Titles can be up to 200 characters.";
                if (!unique("document", document.Id)) return "The curriculum lists an item twice. Reload the page and try again.";
                if (!types.TryGetValue(document.TypeCode ?? string.Empty, out var type)) return "Choose a document type.";
                if (document.MediaAssetId is not null && !string.IsNullOrWhiteSpace(document.Url)) return "A document has either an uploaded file or a link, not both.";
                if (!string.IsNullOrWhiteSpace(document.Url))
                {
                    if (!CatalogPatterns.IsUrl(document.Url)) return "Enter a valid http(s) link for every document.";
                    if (!document.Url.Trim().Split('?', '#')[0].EndsWith(type.FileExtension, StringComparison.OrdinalIgnoreCase))
                    {
                        return "A document link must end in .pdf, .docx or .pptx to match its type.";
                    }
                }
                var attached = document.VideoTempKey is { Length: > 0 } key ? keys.Contains(key)
                    : document.VideoId is { } videoId ? ids.Contains(videoId)
                    : true;
                if (!attached) return "A document is attached to a video that is not in its section.";
            }

            var error = SectionsError(section.Topics ?? [], depth + 1, types, unique);
            if (error is not null) return error;
        }
        return null;
    }
}
