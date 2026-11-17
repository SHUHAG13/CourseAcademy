using CourseAcademy.Api.Common.Middleware;
using CourseAcademy.Api.Common.Options;
using CourseAcademy.Api.Data;
using CourseAcademy.Api.Data.Seed;
using CourseAcademy.Api.Services.Media;
using CourseAcademy.Api.Services.Payments;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;

namespace CourseAcademy.Api.Common;

public sealed record HealthDto(string Status, IReadOnlyDictionary<string, string> Checks);

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        // The demo gateway opens courses without payment, so it must never run outside development.
        var gateway = app.Services.GetRequiredService<IPaymentGatewayResolver>().Active;
        if (!gateway.AllowedInProduction && !app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException($"Payment gateway \"{gateway.Name}\" may only be used in Development. Set Payments:Gateway to a real provider.");
        }
        // The fake streaming service keeps files on this machine and is not secured for real use.
        if (app.Services.GetRequiredService<IOptions<StreamingSettings>>().Value.IsFake && !app.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("Streaming:Mode Fake may only be used in Development. Set Streaming:Mode to Http and configure the Streaming service.");
        }

        // Basic security headers on every response: no MIME sniffing, no framing, no referrer leaks.
        app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            await next();
        });
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        // Requests the client cancelled (e.g. by leaving the page) are not errors.
        app.UseSerilogRequestLogging(options => options.GetLevel = (context, _, exception) =>
            context.RequestAborted.IsCancellationRequested ? LogEventLevel.Debug
            : exception is not null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError ? LogEventLevel.Error
            : LogEventLevel.Information);

        // Empty error responses (unknown route, wrong method) get the standard envelope too.
        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;
            if (response.ContentLength is > 0 || response.ContentType is not null) return;
            var message = response.StatusCode switch
            {
                StatusCodes.Status401Unauthorized => "Please log in to continue.",
                StatusCodes.Status403Forbidden => "You do not have permission to do this.",
                StatusCodes.Status404NotFound => "The requested resource was not found.",
                _ => ReasonPhrases.GetReasonPhrase(response.StatusCode),
            };
            await response.WriteAsJsonAsync(ApiResponse.Fail(message));
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "Course Academy API v1"));
            // Second API testing page at /scalar; it reads the same OpenAPI document as Swagger UI.
            app.MapScalarApiReference(options => options
                .WithTitle("Course Academy API")
                .WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json")
                .AddPreferredSecuritySchemes(ServiceCollectionExtensions.BearerScheme)
                .EnablePersistentAuthentication());
        }
        else
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseCors(CorsSettings.PolicyName);
        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllers();
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = (context, report) => context.Response.WriteAsJsonAsync(new ApiResponse<HealthDto>(
                report.Status == HealthStatus.Healthy,
                new HealthDto(report.Status.ToString(), report.Entries.ToDictionary(e => e.Key, e => e.Value.Status.ToString())),
                report.Status.ToString(),
                [])),
        });
        return app;
    }

    /// <summary>Applies pending migrations when Database:MigrateOnStartup is true (development only by default).</summary>
    public static async Task MigrateDatabaseAsync(this WebApplication app)
    {
        var settings = app.Services.GetRequiredService<IOptions<DatabaseSettings>>().Value;
        await using var scope = app.Services.CreateAsyncScope();
        if (settings.MigrateOnStartup)
        {
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        }
        if (settings.SeedDemoData)
        {
            if (!app.Environment.IsDevelopment())
            {
                throw new InvalidOperationException("Database:SeedDemoData must not be enabled outside Development.");
            }
            await scope.ServiceProvider.GetRequiredService<DemoDataSeeder>().SeedAsync(CancellationToken.None);
            await scope.ServiceProvider.GetRequiredService<DemoCourseSeeder>().SeedAsync(CancellationToken.None);
        }
    }
}
