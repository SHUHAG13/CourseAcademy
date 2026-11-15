using CourseAcademy.Api.Common;
using CourseAcademy.Api.Dtos;
using CourseAcademy.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CourseAcademy.Api.Controllers;

// Permission checks for these endpoints are made in AccessService, so they hold however the service is called.

/// <summary>Screens that access can be granted to, with their permissions.</summary>
[Authorize]
public sealed class ScreensController(IAccessService access) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<ScreenDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Success(await access.GetScreensAsync(ct));
}

[Authorize]
public sealed class RolesController(IAccessService access) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<IReadOnlyList<RoleDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct) => Success(await access.GetRolesAsync(ct));

    [HttpPost]
    [ProducesResponseType<ApiResponse<RoleDto>>(StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(RoleSaveRequest request, CancellationToken ct) =>
        Created(await access.CreateRoleAsync(request, ct));

    [HttpPut("{code}")]
    [ProducesResponseType<ApiResponse<RoleDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(string code, RoleSaveRequest request, CancellationToken ct) =>
        Success(await access.UpdateRoleAsync(code, request, ct));

    [HttpDelete("{code}")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(string code, CancellationToken ct)
    {
        await access.DeleteRoleAsync(code, ct);
        return Success();
    }
}

[Authorize]
public sealed class UsersController(IAccessService access) : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<UserListItemDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] UserListQuery query, CancellationToken ct) =>
        Success(await access.GetUsersAsync(query, ct));

    [HttpGet("{id:int}/access")]
    [ProducesResponseType<ApiResponse<UserAccessDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccess(int id, CancellationToken ct) => Success(await access.GetUserAccessAsync(id, ct));

    [HttpPut("{id:int}/role")]
    [ProducesResponseType<ApiResponse<UserAccessDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetRole(int id, SetUserRoleRequest request, CancellationToken ct) =>
        Success(await access.SetUserRoleAsync(id, request, ct));

    /// <summary>Gives the user their own screen list, which replaces their role's screens.</summary>
    [HttpPut("{id:int}/access")]
    [ProducesResponseType<ApiResponse<UserAccessDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetAccess(int id, SetUserAccessRequest request, CancellationToken ct) =>
        Success(await access.SetUserAccessAsync(id, request, ct));

    /// <summary>Returns the user to their role's screens.</summary>
    [HttpDelete("{id:int}/access")]
    [ProducesResponseType<ApiResponse<UserAccessDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ResetAccess(int id, CancellationToken ct) => Success(await access.ResetUserAccessAsync(id, ct));

    /// <summary>Sets the user's permissions on one course. An empty list removes the grant.</summary>
    [HttpPut("{id:int}/course-grants/{courseId:int}")]
    [ProducesResponseType<ApiResponse<UserAccessDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> SetCourseGrant(int id, int courseId, SetCourseGrantRequest request, CancellationToken ct) =>
        Success(await access.SetCourseGrantAsync(id, courseId, request, ct));
}
