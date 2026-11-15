using CourseAcademy.Api.Common;
using Microsoft.AspNetCore.Mvc;

namespace CourseAcademy.Api.Controllers;

/// <summary>Base for every API controller: version prefix, JSON, and helpers that wrap results in the response envelope.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
[ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
public abstract class ApiControllerBase : ControllerBase
{
    protected OkObjectResult Success<T>(T data, string? message = null) => Ok(ApiResponse.Ok(data, message));

    protected OkObjectResult Success(string? message = null) => Ok(ApiResponse.Ok(message));

    protected ObjectResult Created<T>(T data, string? message = null) =>
        StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(data, message));
}
