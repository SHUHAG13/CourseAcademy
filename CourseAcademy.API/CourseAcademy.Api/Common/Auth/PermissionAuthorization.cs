using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CourseAcademy.Api.Common.Auth;

/// <summary>
/// Requires a signed-in user with at least one of the given permissions on every course (not through a course grant).
/// Course-specific checks are made in services with <see cref="ICurrentUser.EnsureCanAsync"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequirePermissionAttribute(params string[] anyOf)
    : AuthorizeAttribute(PermissionPolicyProvider.Prefix + string.Join(PermissionPolicyProvider.Separator, anyOf));

public sealed record PermissionRequirement(IReadOnlyList<string> AnyOf) : IAuthorizationRequirement;

/// <summary>Builds a policy for each "perm:a,b" name, so permissions need no registration.</summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : DefaultAuthorizationPolicyProvider(options)
{
    public const string Prefix = "perm:";
    public const char Separator = ',';

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(Prefix, StringComparison.Ordinal)) return await base.GetPolicyAsync(policyName);
        var permissions = policyName[Prefix.Length..].Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permissions))
            .Build();
    }
}

public sealed class PermissionHandler(ICurrentUser currentUser, IHttpContextAccessor http) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (currentUser.UserId is null) return;
        var access = await currentUser.GetAccessAsync(http.HttpContext?.RequestAborted ?? CancellationToken.None);
        if (requirement.AnyOf.Any(permission => access.Can(permission))) context.Succeed(requirement);
    }
}
