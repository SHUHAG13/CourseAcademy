namespace CourseAcademy.Api.Common.Exceptions;

/// <summary>
/// An expected failure with a message that is safe to show to the client.
/// Services throw these; the exception middleware turns them into the response envelope.
/// </summary>
public abstract class AppException(int statusCode, string message, IReadOnlyList<ErrorItem>? errors = null) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public IReadOnlyList<ErrorItem> Errors { get; } = errors ?? [];
}

public sealed class ValidationFailedException(IReadOnlyList<ErrorItem> errors, string message = "Please fix the highlighted fields.")
    : AppException(StatusCodes.Status400BadRequest, message, errors);

/// <summary>A business rule refused the request, e.g. a field-level rule checked in a service.</summary>
public sealed class BusinessRuleException(string message, string? field = null)
    : AppException(StatusCodes.Status400BadRequest, message, field is null ? [] : [new ErrorItem(field, message)]);

public sealed class UnauthorizedException(string message = "Please log in to continue.")
    : AppException(StatusCodes.Status401Unauthorized, message);

public sealed class ForbiddenException(string message = "You do not have permission to do this.")
    : AppException(StatusCodes.Status403Forbidden, message);

public sealed class NotFoundException(string message = "The requested item was not found.")
    : AppException(StatusCodes.Status404NotFound, message);

/// <summary>The request conflicts with the current state: duplicate value, invalid status change, etc.</summary>
public sealed class ConflictException(string message) : AppException(StatusCodes.Status409Conflict, message);
