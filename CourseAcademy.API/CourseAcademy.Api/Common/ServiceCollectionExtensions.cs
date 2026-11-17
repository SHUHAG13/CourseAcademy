using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using CourseAcademy.Api.Common.Auth;
using CourseAcademy.Api.Common.Options;
using CourseAcademy.Api.Common.Validation;
using CourseAcademy.Api.Data;
using CourseAcademy.Api.Data.Seed;
using CourseAcademy.Api.Entities;
using CourseAcademy.Api.Repositories;
using CourseAcademy.Api.Services;
using CourseAcademy.Api.Services.Media;
using CourseAcademy.Api.Services.Payments;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CourseAcademy.Api.Common;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<CorsSettings>().Bind(configuration.GetSection(CorsSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<DatabaseSettings>().Bind(configuration.GetSection(DatabaseSettings.SectionName));
        services.AddOptions<PaymentSettings>().Bind(configuration.GetSection(PaymentSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        services.AddDatabase(configuration);
        services.AddApplicationServices();
        services.AddStreaming(configuration);
        services.AddAuth(configuration);
        services.AddWebApi(configuration);
        return services;
    }

    private static void AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>().Bind(configuration.GetSection(JwtSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        services.AddOptions<AuthSettings>().Bind(configuration.GetSection(AuthSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddMemoryCache();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((options, jwt) =>
            {
                var settings = jwt.Value;
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = settings.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TokenService.SigningKey(settings),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = JwtRegisteredClaimNames.Sub,
                };
            });
        services.AddAuthorization();

        var auth = configuration.GetSection(AuthSettings.SectionName).Get<AuthSettings>() ?? new AuthSettings();
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = (context, ct) => new ValueTask(context.HttpContext.Response.WriteAsJsonAsync(
                ApiResponse.Fail("Too many attempts. Please wait a minute and try again."), ct));
            options.AddPolicy(AuthSettings.RateLimitPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = auth.RequestsPerMinute, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
    }

    private static void AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Connection string 'DefaultConnection' is missing. Set ConnectionStrings__DefaultConnection in the environment or user secrets.");
        }

        services.AddSingleton<AuditInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null))
            .AddInterceptors(sp.GetRequiredService<AuditInterceptor>()));
    }

    private static void AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ILookupRepository, LookupRepository>();
        services.AddScoped<ILookupService, LookupService>();

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IScreenRepository, ScreenRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IScreenCatalog, ScreenCatalog>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccessService, AccessService>();
        services.AddScoped<DemoDataSeeder>();
        services.AddScoped<DemoCourseSeeder>();

        services.AddScoped<ICourseRepository, CourseRepository>();
        services.AddScoped<IInstructorRepository, InstructorRepository>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<ICourseAdminService, CourseAdminService>();
        services.AddScoped<IInstructorService, InstructorService>();

        services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IEnrollmentService, EnrollmentService>();
        services.AddScoped<IPaymentService, PaymentService>();

        services.AddScoped<ILessonProgressRepository, LessonProgressRepository>();
        services.AddScoped<IOverviewRepository, OverviewRepository>();
        services.AddScoped<IProgressService, ProgressService>();
        services.AddScoped<IOverviewService, OverviewService>();
        // Payment gateways: register each provider here; Payments:Gateway picks the one new payments use.
        services.AddSingleton<IPaymentGateway, DemoPaymentGateway>();
        services.AddSingleton<IPaymentGatewayResolver, PaymentGatewayResolver>();
        services.AddHostedService<PendingPaymentExpiryService>();

        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);
    }

    /// <summary>
    /// The Streaming integration. Streaming:Mode picks the real service (Http, with retries, timeouts and a circuit
    /// breaker) or the built-in fake (Development only, checked at start-up).
    /// </summary>
    private static void AddStreaming(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<StreamingSettings>().Bind(configuration.GetSection(StreamingSettings.SectionName)).ValidateDataAnnotations().ValidateOnStart();
        // The fake signs its own webhooks; without a configured secret it uses a random one for this run.
        services.PostConfigure<StreamingSettings>(s =>
        {
            if (s.IsFake && string.IsNullOrEmpty(s.WebhookSecret)) s.WebhookSecret = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        });

        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<IStreamingWebhookService, StreamingWebhookService>();
        services.AddSingleton<MediaDeletionSignal>();
        services.AddScoped<MediaDeletionSender>();
        services.AddHostedService<MediaDeletionService>();

        var streaming = configuration.GetSection(StreamingSettings.SectionName).Get<StreamingSettings>() ?? new StreamingSettings();
        if (streaming.IsFake)
        {
            services.AddSingleton<FakeStreamingStore>();
            services.AddSingleton<FakeStreamingQueue>();
            services.AddScoped<IStreamingClient, FakeStreamingClient>();
            services.AddHostedService<FakeStreamingProcessor>();
            return;
        }
        services.AddHttpClient<IStreamingClient, HttpStreamingClient>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<StreamingSettings>>().Value;
                client.BaseAddress = new Uri(settings.BaseUrl.TrimEnd('/') + "/internal/v1/");
                client.DefaultRequestHeaders.Add("X-Api-Key", settings.ApiKey);
            })
            // Retries only GET/HEAD-like calls, so a slow "create upload" is never sent twice.
            .AddStandardResilienceHandler(options => options.Retry.DisableForUnsafeHttpMethods());
    }

    private static void AddWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ValidationFilter>();
        services
            .AddControllers(options => options.Filters.AddService<ValidationFilter>())
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options =>
            {
                // Model-binding errors (wrong types, unreadable JSON) use the same envelope as everything else.
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .SelectMany(entry => entry.Value!.Errors.Select(error => new ErrorItem(
                            ValidationFilter.ToCamelCasePath(entry.Key.TrimStart('$', '.')),
                            string.IsNullOrWhiteSpace(error.ErrorMessage) ? "This value is not valid." : error.ErrorMessage)))
                        .ToList();
                    return new BadRequestObjectResult(ApiResponse.Fail("Please fix the highlighted fields.", errors));
                };
            });

        var cors = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
        services.AddCors(options => options.AddPolicy(CorsSettings.PolicyName, policy => policy
            .WithOrigins(cors.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()));

        services.AddHealthChecks().AddDbContextCheck<AppDbContext>("database");

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new() { Title = "Course Academy API", Version = "v1" });
            var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xml)) options.IncludeXmlComments(xml);
        });
    }
}
