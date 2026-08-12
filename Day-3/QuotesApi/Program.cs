using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Data;
using QuotesApi.Extensions;
using QuotesApi.Models;
using QuotesApi.Repositories;
using QuotesApi.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Configuration
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

var entraTenantId = builder.Configuration["Entra:TenantId"]
    ?? throw new InvalidOperationException("Entra tenant ID is not configured.");

var entraAudience = builder.Configuration["Entra:Audience"]
    ?? throw new InvalidOperationException("Entra audience is not configured.");


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
    // 1. Internal JWT
    // ========================================================
    .AddJwtBearer("InternalJwt", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    })

    // ========================================================
    // 2. Microsoft Entra JWT
    // ========================================================
    .AddJwtBearer("EntraJwt", options =>
    {
        options.Authority =
            $"https://login.microsoftonline.com/{entraTenantId}/v2.0";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,

            ValidAudience = entraAudience
        };
    })

    // ========================================================
    // 3. Smart Policy Scheme
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
                    // Read issuer without validating the token here.
                    // The selected JwtBearer handler performs
                    // the actual validation.
                    var jwt =
                        new JwtSecurityTokenHandler()
                            .ReadJwtToken(token);

                    var issuer = jwt.Issuer;

                    // Microsoft Entra issuer
                    if (issuer.Contains(
                            "login.microsoftonline.com",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return "EntraJwt";
                    }

                    // Our internal JWT
                    return "InternalJwt";
                }
                catch
                {
                    // Invalid token will be rejected by
                    // InternalJwt.
                    return "InternalJwt";
                }
            };
        });


// ============================================================
// Authorization
// ============================================================

builder.Services.AddAuthorization();


// ============================================================
// Database
// ============================================================

builder.Services.AddInfrastructure(builder.Configuration);


// ============================================================
// Repositories
// ============================================================

builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();


// ============================================================
// DI lifetime exercise
// ============================================================

builder.Services.AddSingleton<IClock, QuotesApi.Services.SystemClock>();
builder.Services.AddTransient<QuoteFormatter>();


// ============================================================
// Build application
// ============================================================

var app = builder.Build();


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
                BCrypt.Net.BCrypt.HashPassword("Password123!")
        });

        db.SaveChanges();
    }
}


// ============================================================
// API endpoints
// ============================================================

app.MapAuthEndpoints(builder.Configuration);
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