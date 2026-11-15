using System.Text.Json;
using CourseAcademy.Api.Common;
using CourseAcademy.Api.Common.Exceptions;
using CourseAcademy.Api.Common.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CourseAcademy.Tests.Common;

public class ExceptionHandlingMiddlewareTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private static async Task<(int Status, JsonElement Body)> Run(Exception exception)
    {
        var middleware = new ExceptionHandlingMiddleware(_ => throw exception, NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body, Json);
        return (context.Response.StatusCode, body);
    }

    [Theory]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ForbiddenException), 403)]
    [InlineData(typeof(UnauthorizedException), 401)]
    public async Task Maps_app_exceptions_to_their_status_and_message(Type type, int expectedStatus)
    {
        var exception = (AppException)Activator.CreateInstance(type, "Custom message")!;

        var (status, body) = await Run(exception);

        Assert.Equal(expectedStatus, status);
        Assert.False(body.GetProperty("success").GetBoolean());
        Assert.Equal("Custom message", body.GetProperty("message").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
        Assert.Equal(0, body.GetProperty("errors").GetArrayLength());
    }

    [Fact]
    public async Task Returns_field_errors_for_validation_failures()
    {
        var (status, body) = await Run(new ValidationFailedException([new ErrorItem("email", "Enter a valid email.")]));

        Assert.Equal(400, status);
        var error = body.GetProperty("errors")[0];
        Assert.Equal("email", error.GetProperty("field").GetString());
        Assert.Equal("Enter a valid email.", error.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Hides_details_of_unexpected_errors()
    {
        var (status, body) = await Run(new InvalidOperationException("Connection string leaked: secret"));

        Assert.Equal(500, status);
        Assert.Equal(ExceptionHandlingMiddleware.UnexpectedErrorMessage, body.GetProperty("message").GetString());
        Assert.DoesNotContain("secret", body.GetRawText());
    }

    [Fact]
    public async Task Maps_concurrency_conflicts_to_409()
    {
        var (status, _) = await Run(new DbUpdateConcurrencyException());

        Assert.Equal(409, status);
    }

    [Fact]
    public async Task Writes_nothing_when_the_client_cancelled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var middleware = new ExceptionHandlingMiddleware(_ => throw new OperationCanceledException(), NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext { RequestAborted = cts.Token };
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        Assert.Equal(0, context.Response.Body.Length);
    }
}
