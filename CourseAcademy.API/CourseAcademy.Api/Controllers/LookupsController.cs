using CourseAcademy.Api.Common;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CourseAcademy.Api.Controllers;

/// <summary>Fixed value lists (categories, formats, statuses, payment methods). Public.</summary>
public sealed class LookupsController(ILookupService lookups) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<LookupsDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Success(await lookups.GetAllAsync(ct));
}
