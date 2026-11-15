using System.ComponentModel.DataAnnotations;

namespace CourseAcademy.Api.Common.Options;

/// <summary>Browser origins allowed to call the API (the Angular app). Section "Cors" in configuration.</summary>
public sealed class CorsSettings
{
    public const string SectionName = "Cors";
    public const string PolicyName = "AngularApp";

    [MinLength(1, ErrorMessage = "Configure at least one allowed origin in Cors:AllowedOrigins.")]
    public string[] AllowedOrigins { get; set; } = [];
}

/// <summary>Section "Database" in configuration.</summary>
public sealed class DatabaseSettings
{
    public const string SectionName = "Database";

    /// <summary>Apply pending migrations at start-up. Meant for development; production applies migrations as a release step.</summary>
    public bool MigrateOnStartup { get; set; }

    /// <summary>Add the demo accounts at start-up. Development only; never enable in production.</summary>
    public bool SeedDemoData { get; set; }
}
