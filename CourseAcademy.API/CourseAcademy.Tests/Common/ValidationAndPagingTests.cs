using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Common.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace CourseAcademy.Tests.Common;

public class ValidationAndPagingTests
{
    public sealed record SampleRequest(string Email, int Quantity);

    private sealed class SampleValidator : AbstractValidator<SampleRequest>
    {
        public SampleValidator()
        {
            RuleFor(r => r.Email).EmailAddress().WithMessage("Enter a valid email.");
            RuleFor(r => r.Quantity).GreaterThan(0).WithMessage("Enter 1 or more.");
        }
    }

    private static async Task<bool> RunFilter(object argument)
    {
        var services = new ServiceCollection().AddScoped<IValidator<SampleRequest>, SampleValidator>().BuildServiceProvider();
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        var context = new ActionExecutingContext(actionContext, [], new Dictionary<string, object?> { ["request"] = argument }, controller: null!);
        var called = false;

        await new ValidationFilter(services).OnActionExecutionAsync(context, () =>
        {
            called = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], controller: null!));
        });
        return called;
    }

    [Fact]
    public async Task Valid_requests_reach_the_action()
    {
        Assert.True(await RunFilter(new SampleRequest("a@b.com", 1)));
    }

    [Fact]
    public async Task Invalid_requests_fail_with_camel_case_field_errors()
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() => RunFilter(new SampleRequest("nope", 0)));

        Assert.Equal(["email", "quantity"], exception.Errors.Select(e => e.Field));
        Assert.Equal(400, exception.StatusCode);
    }

    [Fact]
    public async Task Arguments_without_a_validator_are_not_checked()
    {
        Assert.True(await RunFilter("plain string"));
    }

    [Theory]
    [InlineData("Schedule[0].StartsAt", "schedule[0].startsAt")]
    [InlineData("Email", "email")]
    [InlineData("", null)]
    public void Converts_property_paths_to_camel_case(string path, string? expected)
    {
        Assert.Equal(expected, ValidationFilter.ToCamelCasePath(path));
    }

    [Theory]
    [InlineData(0, 0, 1, 20)]
    [InlineData(-3, 500, 1, 100)]
    [InlineData(4, 10, 4, 10)]
    public void Page_query_clamps_out_of_range_values(int page, int pageSize, int expectedPage, int expectedPageSize)
    {
        var query = new PageQuery { Page = page, PageSize = pageSize };

        Assert.Equal(expectedPage, query.Page);
        Assert.Equal(expectedPageSize, query.PageSize);
        Assert.Equal((expectedPage - 1) * expectedPageSize, query.Skip);
    }
}
