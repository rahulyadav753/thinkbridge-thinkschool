using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Trace;
using QuotesApi.Authorization;
using QuotesApi.Data;
using QuotesApi.Extensions;
using QuotesApi.Models;
using QuotesApi.Repositories;
using QuotesApi.Services;
using Serilog;
using Serilog.Context;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Custom ActivitySource
// ============================================================

var activitySource = new ActivitySource("QuotesApi");


// ============================================================
// Serilog
// ============================================================

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()

        // ----------------------------------------------------
        // Console
        // ----------------------------------------------------
        .WriteTo.Console(
            outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] " +
                "[TraceId:{TraceId}] " +
                "[SpanId:{SpanId}] " +
                "{Message:lj}{NewLine}{Exception}")

        // ----------------------------------------------------
        // Aspire Dashboard / OpenTelemetry
        // ----------------------------------------------------
        .WriteTo.OpenTelemetry(options =>
        {
            options.Endpoint = "http://localhost:4317";

            options.ResourceAttributes =
                new Dictionary<string, object>
                {
                    ["service.name"] = "QuotesApi"
                };
        });
});


// ============================================================
// OpenTelemetry
// ============================================================

builder.Services
    .AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            // Custom application spans
            .AddSource("QuotesApi")

            // Automatic instrumentation
            .AddAspNetCoreInstrumentation()
            .AddEntityFrameworkCoreInstrumentation()
            .AddHttpClientInstrumentation()

            // Aspire / OTLP
            .AddOtlpExporter(options =>
            {
                options.Endpoint =
                    new Uri("http://localhost:4317");
            });
    });


// ============================================================
// Configuration
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

var entraTenantId = builder.Configuration["Entra:TenantId"]
    ?? throw new InvalidOperationException(
        "Entra tenant ID is not configured.");

var entraAudience = builder.Configuration["Entra:Audience"]
    ?? throw new InvalidOperationException(
        "Entra audience is not configured.");


// ============================================================
// Authentication
// ============================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })

    // ========================================================
    // Internal JWT
    // ========================================================
    .AddJwtBearer("InternalJwt", options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtKey))
            };
    })

    // ========================================================
    // Microsoft Entra JWT
    // ========================================================
    .AddJwtBearer("EntraJwt", options =>
    {
        options.Authority =
            $"https://login.microsoftonline.com/{entraTenantId}/v2.0";

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,

                ValidAudience = entraAudience
            };
    })

    // ========================================================
    // Smart Policy Scheme
    // ========================================================
    .AddPolicyScheme(
        "Smart",
        "Internal JWT or Microsoft Entra JWT",
        options =>
        {
            options.ForwardDefaultSelector = context =>
            {
                var authorization =
                    context.Request.Headers.Authorization.ToString();

                // No Bearer token
                if (!authorization.StartsWith(
                        "Bearer ",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return "InternalJwt";
                }

                var token =
                    authorization["Bearer ".Length..].Trim();

                try
                {
                    var jwt =
                        new JwtSecurityTokenHandler()
                            .ReadJwtToken(token);

                    // Microsoft Entra issuer
                    if (jwt.Issuer.Contains(
                            "login.microsoftonline.com",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return "EntraJwt";
                    }

                    // Internal JWT
                    return "InternalJwt";
                }
                catch
                {
                    return "InternalJwt";
                }
            };
        });


// ============================================================
// Authorization
// ============================================================

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("can-edit-quotes", policy =>
    {
        policy.RequireClaim(
            "scope",
            "quotes.write");

        policy.AddRequirements(
            new CanDeleteQuoteRequirement());
    });
});


// ============================================================
// Database
// ============================================================

builder.Services.AddInfrastructure(
    builder.Configuration);


// ============================================================
// Repositories
// ============================================================

builder.Services.AddScoped<
    IQuoteRepository,
    QuoteRepository>();

builder.Services.AddScoped<
    ICollectionRepository,
    CollectionRepository>();


// ============================================================
// Authorization Handlers
// ============================================================

builder.Services.AddScoped<
    IAuthorizationHandler,
    CanDeleteQuoteHandler>();


// ============================================================
// DI lifetime exercise
// ============================================================

builder.Services.AddSingleton<
    IClock,
    QuotesApi.Services.SystemClock>();

builder.Services.AddTransient<
    QuoteFormatter>();

builder.Services.AddTransient<
    RefreshTokenManager>();


// ============================================================
// Build application
// ============================================================

var app = builder.Build();


// ============================================================
// Request TraceId / SpanId correlation
// ============================================================

app.Use(async (context, next) =>
{
    var activity = Activity.Current;

    using (LogContext.PushProperty(
        "TraceId",
        activity?.TraceId.ToString() ?? "none"))
    using (LogContext.PushProperty(
        "SpanId",
        activity?.SpanId.ToString() ?? "none"))
    {
        // ----------------------------------------------------
        // Custom application span
        // ----------------------------------------------------
        using var customActivity =
            activitySource.StartActivity(
                "application-processing");

        customActivity?.SetTag(
            "application.component",
            "QuotesApi");

        customActivity?.SetTag(
            "http.method",
            context.Request.Method);

        customActivity?.SetTag(
            "http.path",
            context.Request.Path.ToString());

        await next();
    }
});


// ============================================================
// Middleware
// ============================================================

app.UseAuthentication();
app.UseAuthorization();


// ============================================================
// Create / update database
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider
        .GetRequiredService<QuotesDbContext>();

    db.Database.Migrate();

    if (!db.Users.Any())
    {
        db.Users.Add(new User
        {
            Email = "test@example.com",

            PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(
                    "Password123!")
        });

        db.SaveChanges();
    }
}


// ============================================================
// API endpoints
// ============================================================

app.MapAuthEndpoints(
    builder.Configuration);

app.MapQuoteEndpoints();

app.MapCollectionEndpoints();


// ============================================================
// Run
// ============================================================

app.Run();


// ============================================================
// Required for integration tests
// ============================================================

public partial class Program
{
}