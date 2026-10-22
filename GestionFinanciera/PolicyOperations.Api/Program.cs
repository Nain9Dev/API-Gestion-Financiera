using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using PolicyOperations.Api.Configuration;
using PolicyOperations.Api.Demo;
using PolicyOperations.Api.Errors;
using PolicyOperations.Api.Health;
using Scalar.AspNetCore;
using PolicyOperations.Api.Security;
using PolicyOperations.Application.Abstractions;
using PolicyOperations.Application.Policies;
using PolicyOperations.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection must be configured outside source control.");
}

var supportedCurrencies = builder.Configuration
    .GetSection("PolicyOperations:SupportedCurrencies")
    .Get<string[]>() ?? [];
var publicDemo = builder.Configuration
    .GetSection(PublicDemoOptions.SectionName)
    .Get<PublicDemoOptions>() ?? new PublicDemoOptions();
var sqlResilience = builder.Configuration
    .GetSection(SqlResilienceOptions.SectionName)
    .Get<SqlResilienceOptions>() ?? new SqlResilienceOptions();

builder.Services.AddDbContext<PolicyOperationsDbContext>(options =>
    options.UseSqlServer(
        connectionString,
        sqlServerOptions =>
        {
            sqlServerOptions.EnableRetryOnFailure(
                maxRetryCount: sqlResilience.MaxRetryCount,
                maxRetryDelay: TimeSpan.FromSeconds(sqlResilience.MaxRetryDelaySeconds),
                errorNumbersToAdd: null);

            // Azure SQL Serverless resumes from AutoPause on the first connection after an idle
            // period. The provider default of 30 seconds truncates that resume and surfaces it as
            // a failure, so the demo deployment needs an explicit, configurable budget.
            sqlServerOptions.CommandTimeout(sqlResilience.CommandTimeoutSeconds);
        }));
builder.Services.AddScoped<IPolicyRepository, EfPolicyRepository>();
builder.Services.AddScoped<IUnitOfWork>(serviceProvider =>
    serviceProvider.GetRequiredService<PolicyOperationsDbContext>());
builder.Services.AddSingleton<ICurrencyCatalog>(
    new ConfiguredCurrencyCatalog(supportedCurrencies));
builder.Services.AddScoped<PolicyService>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<CurrentActorAccessor>();
builder.Services.AddScoped<PublicDemoDataPruner>();
builder.Services
    .AddOptions<PublicDemoOptions>()
    .Bind(builder.Configuration.GetSection(PublicDemoOptions.SectionName))
    .Validate(
        options => !options.Enabled || options.OrganizationId != Guid.Empty,
        "PublicDemo:OrganizationId must be a non-empty GUID when the demo is enabled.")
    .Validate(
        options => options.RetentionHours is >= 1 and <= 168,
        "PublicDemo:RetentionHours must be between 1 and 168.")
    .Validate(
        options => options.RequestsPerMinute is >= 1 and <= 30,
        "PublicDemo:RequestsPerMinute must be between 1 and 30.")
    .ValidateOnStart();

builder.Services
    .AddOptions<SqlResilienceOptions>()
    .Bind(builder.Configuration.GetSection(SqlResilienceOptions.SectionName))
    .Validate(
        options => options.MaxRetryCount is >= 0 and <= 20,
        "PolicyOperations:SqlResilience:MaxRetryCount must be between 0 and 20.")
    .Validate(
        options => options.MaxRetryDelaySeconds is >= 1 and <= 60,
        "PolicyOperations:SqlResilience:MaxRetryDelaySeconds must be between 1 and 60.")
    .Validate(
        options => options.CommandTimeoutSeconds is >= 5 and <= 180,
        "PolicyOperations:SqlResilience:CommandTimeoutSeconds must be between 5 and 180.")
    .Validate(
        options => options.RetryAfterSeconds is >= 1 and <= 300,
        "PolicyOperations:SqlResilience:RetryAfterSeconds must be between 1 and 300.")
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization(PolicyAuthorization.Configure);
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler,
    ApiAuthorizationMiddlewareResultHandler>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(
        PublicDemoRateLimit.PolicyName,
        httpContext => RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = publicDemo.RequestsPerMinute,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.OnRejected = async (context, cancellationToken) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = "60";
        var problemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
        {
            Status = StatusCodes.Status429TooManyRequests,
            Title = "Public demo rate limit exceeded.",
            Detail = "Wait one minute before running the public demo again.",
            Instance = context.HttpContext.Request.Path
        };
        problemDetails.Extensions["code"] = "public_demo_rate_limited";
        problemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        await context.HttpContext.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
    };
});

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:5375",
                "http://127.0.0.1:5375",
                "https://naindev.com",
                "https://www.naindev.com",
                "https://policy.naindev.com")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("ETag", "Retry-After");
    });
});
builder.Services.AddOpenApi();
builder.Services
    .AddHealthChecks()
    .AddCheck<SqlServerReadinessHealthCheck>(
        "sqlserver",
        tags: ["ready"]);

var app = builder.Build();

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<PolicyOperationsDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseExceptionHandler();

app.MapOpenApi();
app.MapScalarApiReference("/docs");
app.MapGet("/swagger", () => Results.Redirect("/docs")).ExcludeFromDescription();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Baseline hardening for every response, including API and Problem Details payloads.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["Referrer-Policy"] = "no-referrer";
    headers.XContentTypeOptions = "nosniff";
    headers.XFrameOptions = "DENY";

    if (context.Request.Path.StartsWithSegments("/demo"))
    {
        headers.ContentSecurityPolicy =
            "default-src 'self'; base-uri 'self'; connect-src 'self'; " +
            "font-src 'self'; form-action 'none'; frame-ancestors 'none'; " +
            "img-src 'self' data:; object-src 'none'; script-src 'self'; style-src 'self'";
    }

    await next(context);
});

if (publicDemo.Enabled)
{
    app.UseDefaultFiles();
    app.UseStaticFiles(new StaticFileOptions
    {
        OnPrepareResponse = context =>
        {
            // The page must always reflect the deployed build; its assets may be cached briefly.
            context.Context.Response.Headers.CacheControl =
                context.File.Name.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
                    ? "no-cache"
                    : "public, max-age=300";
        }
    });
}

app.UseRouting();
app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks(
        "/health",
        new HealthCheckOptions { Predicate = _ => false })
    .AllowAnonymous();
app.MapHealthChecks(
        "/health/ready",
        new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains("ready")
        })
    .AllowAnonymous();

if (publicDemo.Enabled)
{
    app.MapGet("/", () => Results.Redirect("/demo/"))
        .AllowAnonymous()
        .ExcludeFromDescription();
}

app.MapControllers();

app.Run();

public partial class Program;
