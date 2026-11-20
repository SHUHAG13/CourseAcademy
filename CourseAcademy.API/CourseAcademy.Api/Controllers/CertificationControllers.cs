using CourseAcademy.Api.Common;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseAcademy.Api.Controllers;

// Permission and course-access checks are made in the services.

/// <summary>Certification rules, the central exam and certificates, for learners and for the people who manage them.</summary>
[Authorize]
[Route("api/v1")]
public sealed class CertificationController(ICertificationService certification) : ApiControllerBase
{
    [HttpGet("admin/courses/{courseId:int}/certification-rule")]
    [ProducesResponseType<ApiResponse<RuleDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRule(int courseId, CancellationToken ct) => Success(await certification.GetRuleAsync(courseId, ct));

    [HttpPut("admin/courses/{courseId:int}/certification-rule")]
    [ProducesResponseType<ApiResponse<RuleDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SaveRule(int courseId, RuleSaveRequest request, CancellationToken ct) =>
        Success(await certification.SaveRuleAsync(courseId, request, ct));

    [HttpGet("me/courses/{courseId:int}/certification")]
    [ProducesResponseType<ApiResponse<CertificationStatusDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Status(int courseId, CancellationToken ct) => Success(await certification.GetStatusAsync(courseId, ct));

    /// <summary>"Get my certificate" for completion rules. Data is null while the requirement is not met.</summary>
    [HttpPost("me/courses/{courseId:int}/certificate")]
    [ProducesResponseType<ApiResponse<CertificateDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Claim(int courseId, CancellationToken ct) => Success(await certification.ClaimAsync(courseId, ct));

    [HttpPost("me/courses/{courseId:int}/exam-submissions")]
    [ProducesResponseType<ApiResponse<SubmissionDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Submit(int courseId, ExamSubmitRequest request, CancellationToken ct) =>
        Created(await certification.SubmitExamAsync(courseId, request, ct));

    [HttpGet("me/certificates")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<CertificateDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Mine(CancellationToken ct) => Success(await certification.GetMineAsync(ct));

    [HttpGet("admin/exam-submissions")]
    [ProducesResponseType<ApiResponse<PagedResult<SubmissionDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Submissions([FromQuery] SubmissionListQuery query, CancellationToken ct) =>
        Success(await certification.GetSubmissionsAsync(query, ct));

    [HttpPost("admin/exam-submissions/{id:int}/review")]
    [ProducesResponseType<ApiResponse<ReviewResultDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Review(int id, ReviewSubmissionRequest request, CancellationToken ct) =>
        Success(await certification.ReviewAsync(id, request, ct));

    [HttpGet("admin/certificates")]
    [ProducesResponseType<ApiResponse<PagedResult<CertificateDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Certificates([FromQuery] CertificateListQuery query, CancellationToken ct) =>
        Success(await certification.GetCertificatesAsync(query, ct));

    [HttpPost("admin/certificates/{id:int}/revoke")]
    [ProducesResponseType<ApiResponse<CertificateDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Revoke(int id, RevokeCertificateRequest request, CancellationToken ct) =>
        Success(await certification.RevokeAsync(id, request, ct));

    [HttpPost("admin/certificates/{id:int}/restore")]
    [ProducesResponseType<ApiResponse<CertificateDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Restore(int id, CancellationToken ct) => Success(await certification.RestoreAsync(id, ct));

    /// <summary>Public certificate check by code.</summary>
    [HttpGet("certificates/verify/{code}")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<VerifyCertificateDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Verify(string code, CancellationToken ct) => Success(await certification.VerifyAsync(code, ct));
}
