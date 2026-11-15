using System.Text.Json;
using CourseAcademy.Api.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CourseAcademy.Api.Common.Validation;

/// <summary>Runs the registered FluentValidation validator for every action argument before the action executes.</summary>
public sealed class ValidationFilter(IServiceProvider services) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new List<ErrorItem>();
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;
            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            errors.AddRange(result.Errors.Select(e => new ErrorItem(ToCamelCasePath(e.PropertyName), e.ErrorMessage)));
        }

        if (errors.Count > 0) throw new ValidationFailedException(errors);
        await next();
    }

    /// <summary>"Schedule[0].StartsAt" → "schedule[0].startsAt", matching the JSON the client sent.</summary>
    internal static string? ToCamelCasePath(string? path) =>
        string.IsNullOrEmpty(path)
            ? null
            : string.Join('.', path.Split('.').Select(part => JsonNamingPolicy.CamelCase.ConvertName(part)));
}
