using CourseAcademy.Api.Common.Middleware;
using CourseAcademy.Api.Common.Options;
using CourseAcademy.Api.Data;
using CourseAcademy.Api.Data.Seed;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Serilog;

namespace CourseAcademy.Api.Common;

public sealed record HealthDto(string Status, IReadOnlyDictionary<string, string> Checks);

public static class ApplicationBuilderExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseSerilogRequestLogging();

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
