using CourseAcademy.Api.Common;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseAcademy.Api.Controllers;

// Permission and course-access checks are made in the services.

/// <summary>The signed-in user's learning progress.</summary>
[Authorize]
public sealed class ProgressController(IProgressService progress) : ApiControllerBase
{
    [HttpGet("mine")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<CourseProgressSummaryDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken ct) => Success(await progress.GetMineAsync(ct));

    [HttpGet("courses/{courseSlug}")]
    [ProducesResponseType<ApiResponse<CourseProgressDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Course(string courseSlug, CancellationToken ct) => Success(await progress.GetCourseAsync(courseSlug, ct));

    [HttpPut("videos/{videoId:int}/complete")]
    [ProducesResponseType<ApiResponse<CourseProgressDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteVideo(int videoId, SetCompletedRequest request, CancellationToken ct) =>
        Success(await progress.SetVideoCompletedAsync(videoId, request.Completed, ct));

    /// <summary>Sent by the player every few seconds while a direct video file or an upload plays.</summary>
    [HttpPut("videos/{videoId:int}/position")]
    [ProducesResponseType<ApiResponse<CourseProgressDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SavePosition(int videoId, VideoPositionRequest request, CancellationToken ct) =>
        Success(await progress.SavePositionAsync(videoId, request, ct));

    /// <summary>For sections without videos.</summary>
    [HttpPut("sections/{sectionId:int}/complete")]
    [ProducesResponseType<ApiResponse<CourseProgressDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> CompleteSection(int sectionId, SetCompletedRequest request, CancellationToken ct) =>
        Success(await progress.SetSectionCompletedAsync(sectionId, request.Completed, ct));
}

[Authorize]
[Route("api/v1/admin/overview")]
public sealed class AdminOverviewController(IOverviewService overview) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<AdminOverviewDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken ct) => Success(await overview.GetAsync(ct));
}
