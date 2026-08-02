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
using PolicyOperations.Api.OpenApi;
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

builder.Services.AddDbContext<PolicyOperationsDbContext>(options =>
    options.UseSqlServer(connectionString));
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        JwtBearerDefaults.AuthenticationScheme,
        new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Local development JWT created with dotnet user-jwts."
        });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(
            JwtBearerDefaults.AuthenticationScheme,
            document)] = []
    });
    options.OperationFilter<PolicyOperationFilter>();
});
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

if (app.Environment.IsDevelopment() || publicDemo.Enabled)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

if (publicDemo.Enabled)
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/demo"))
        {
            context.Response.Headers.ContentSecurityPolicy =
                "default-src 'self'; base-uri 'self'; connect-src 'self'; " +
                "font-src 'self'; form-action 'none'; frame-ancestors 'none'; " +
                "img-src 'self' data:; object-src 'none'; script-src 'self'; style-src 'self'";
            context.Response.Headers["Referrer-Policy"] = "no-referrer";
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.XFrameOptions = "DENY";
        }

        await next(context);
    });
    app.UseDefaultFiles();
    app.UseStaticFiles();
}

app.UseRouting();
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
