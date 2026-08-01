using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using PolicyOperations.Api.Configuration;
using PolicyOperations.Api.Errors;
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

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();
builder.Services.AddAuthorization(PolicyAuthorization.Configure);
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler,
    ApiAuthorizationMiddlewareResultHandler>();

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
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();

app.Run();

public partial class Program;
