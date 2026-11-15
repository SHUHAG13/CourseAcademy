using CourseAcademy.Api.Common;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseAcademy.Api.Controllers;

// Permission checks are made in the services. Public endpoints still read the signed-in user when a token is
// sent, so people who manage a course also see its drafts and every link.

/// <summary>The public course catalogue.</summary>
[AllowAnonymous]
public sealed class CoursesController(ICatalogService catalog) : ApiControllerBase
{
    /// <summary>Published courses as cards, filtered, sorted and paged.</summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<CourseCardDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] CourseSearchQuery query, CancellationToken ct) =>
        Success(await catalog.SearchAsync(query, ct));

    /// <summary>Every course the caller may see, in full (the frontend keeps the catalogue in memory).</summary>
    [HttpGet("all")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<CourseDetailDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Success(await catalog.GetAllAsync(ct));

    [HttpGet("{slug}")]
    [ProducesResponseType<ApiResponse<CourseDetailDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct) => Success(await catalog.GetBySlugAsync(slug, ct));

    [HttpGet("{slug}/related")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<CourseCardDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRelated(string slug, CancellationToken ct, [FromQuery] int limit = 4) =>
        Success(await catalog.GetRelatedAsync(slug, limit, ct));
}

/// <summary>Instructor profiles: public to read, admin to change.</summary>
public sealed class InstructorsController(ICatalogService catalog, IInstructorService instructors) : ApiControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<IReadOnlyList<InstructorDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Success(await catalog.GetInstructorsAsync(ct));

    [HttpPost]
    [Authorize]
    [ProducesResponseType<ApiResponse<InstructorDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(InstructorSaveRequest request, CancellationToken ct) =>
        Created(await instructors.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    [Authorize]
    [ProducesResponseType<ApiResponse<InstructorDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, InstructorSaveRequest request, CancellationToken ct) =>
        Success(await instructors.UpdateAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [Authorize]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await instructors.DeleteAsync(id, ct);
        return Success();
    }
}

/// <summary>Creating and editing courses and their curriculum.</summary>
[Authorize]
[Route("api/v1/admin/courses")]
public sealed class AdminCoursesController(ICourseAdminService admin) : ApiControllerBase
{
    [HttpPost]
    [ProducesResponseType<ApiResponse<CourseDetailDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(CourseUpsertRequest request, CancellationToken ct) => Created(await admin.CreateAsync(request, ct));

    [HttpPut("{id:int}")]
    [ProducesResponseType<ApiResponse<CourseDetailDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(int id, CourseUpsertRequest request, CancellationToken ct) =>
        Success(await admin.UpdateAsync(id, request, ct));

    [HttpPatch("{id:int}/publish")]
    [ProducesResponseType<ApiResponse<CourseDetailDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetPublished(int id, SetPublishedRequest request, CancellationToken ct) =>
        Success(await admin.SetPublishedAsync(id, request.IsPublished, ct));

    /// <summary>Replaces the whole curriculum. Items without id are created; items left out are removed.</summary>
    [HttpPut("{id:int}/curriculum")]
    [ProducesResponseType<ApiResponse<CourseDetailDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveCurriculum(int id, CurriculumRequest request, CancellationToken ct) =>
        Success(await admin.SaveCurriculumAsync(id, request, ct));

    [HttpDelete("{id:int}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await admin.DeleteAsync(id, ct);
        return Success();
    }

    [HttpGet("slug-available")]
    [ProducesResponseType<ApiResponse<SlugAvailabilityDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SlugAvailable([FromQuery] string slug, [FromQuery] int? exceptId, CancellationToken ct) =>
        Success(await admin.IsSlugAvailableAsync(slug ?? string.Empty, exceptId, ct));
}
