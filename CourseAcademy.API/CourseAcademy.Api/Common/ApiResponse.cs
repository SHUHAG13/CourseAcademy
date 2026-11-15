namespace CourseAcademy.Api.Common;

/// <summary>One validation or business error. <see cref="Field"/> is the camelCase property name, or null.</summary>
public sealed record ErrorItem(string? Field, string Message);

/// <summary>The envelope every endpoint returns: <c>{ success, data, message, errors }</c>. All four fields are always present.</summary>
public sealed record ApiResponse<T>(bool Success, T? Data, string? Message, IReadOnlyList<ErrorItem> Errors);

/// <summary>Factory methods for <see cref="ApiResponse{T}"/>.</summary>
public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string? message = null) => new(true, data, message, []);

    public static ApiResponse<object?> Ok(string? message = null) => new(true, null, message, []);

    public static ApiResponse<object?> Fail(string message, IReadOnlyList<ErrorItem>? errors = null) =>
        new(false, null, message, errors ?? []);
}
