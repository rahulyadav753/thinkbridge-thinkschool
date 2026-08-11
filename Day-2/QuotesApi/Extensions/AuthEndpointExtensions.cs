using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Data;
using QuotesApi.Models;
using QuotesApi.Services;

namespace QuotesApi.Extensions;

public static class AuthEndpointExtensions
{
    private sealed record RefreshRequest(string RefreshToken);

    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder app,
        IConfiguration configuration)
    {
        var group = app.MapGroup("/api/auth");

        // POST /api/auth/login
        group.MapPost("/login", async (
            LoginRequest request,
            QuotesDbContext db,
            CancellationToken cancellationToken) =>
        {
            var user = await db.Users
                .SingleOrDefaultAsync(
                    x => x.Email == request.Email,
                    cancellationToken);

            if (user is null ||
                !BCrypt.Net.BCrypt.Verify(
                    request.Password,
                    user.PasswordHash))
            {
                return Results.Unauthorized();
            }

            var accessToken = CreateAccessToken(
                user,
                configuration);

            var refreshToken =
                RefreshTokenService.Generate();

            var refreshTokenHash =
                RefreshTokenService.Hash(refreshToken);

            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshTokenHash,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            db.RefreshTokens.Add(refreshTokenEntity);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(
                new LoginResponse(
                    accessToken,
                    refreshToken,
                    15 * 60));
        });

        // POST /api/auth/refresh
        group.MapPost("/refresh", async (
            RefreshRequest request,
            QuotesDbContext db,
            IConfiguration config,
            CancellationToken cancellationToken) =>
        {
            var tokenHash =
                RefreshTokenService.Hash(request.RefreshToken);

            var storedToken = await db.RefreshTokens
                .SingleOrDefaultAsync(
                    x => x.Token == tokenHash,
                    cancellationToken);

            if (storedToken is null)
                return Results.Unauthorized();

            // Refresh token reuse detection
            if (storedToken.RevokedAt is not null &&
                storedToken.ReplacedByToken is not null)
            {
                Console.WriteLine(
                    $"SECURITY EVENT: Refresh token reuse detected. UserId={storedToken.UserId}");

                var familyTokens = await db.RefreshTokens
                    .Where(x => x.UserId == storedToken.UserId)
                    .ToListAsync(cancellationToken);

                foreach (var token in familyTokens)
                {
                    if (token.RevokedAt is null)
                        token.RevokedAt = DateTime.UtcNow;
                }

                await db.SaveChangesAsync(cancellationToken);

                return Results.Unauthorized();
            }

            if (storedToken.RevokedAt is not null)
                return Results.Unauthorized();

            if (storedToken.ExpiresAt <= DateTime.UtcNow)
                return Results.Unauthorized();

            var user = await db.Users
                .SingleOrDefaultAsync(
                    x => x.Id == storedToken.UserId,
                    cancellationToken);

            if (user is null)
                return Results.Unauthorized();

            var accessToken = CreateAccessToken(
                user,
                config);

            var newRefreshToken =
                RefreshTokenService.Generate();

            var newRefreshTokenHash =
                RefreshTokenService.Hash(newRefreshToken);

            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshTokenHash,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            storedToken.RevokedAt = DateTime.UtcNow;
            storedToken.ReplacedByToken =
                newRefreshTokenHash;

            db.RefreshTokens.Add(newRefreshTokenEntity);

            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(
                new LoginResponse(
                    accessToken,
                    newRefreshToken,
                    15 * 60));
        });

        // POST /api/auth/logout
        group.MapPost("/logout", async (
            RefreshRequest request,
            QuotesDbContext db,
            CancellationToken cancellationToken) =>
        {
            var tokenHash =
                RefreshTokenService.Hash(request.RefreshToken);

            var storedToken = await db.RefreshTokens
                .SingleOrDefaultAsync(
                    x => x.Token == tokenHash,
                    cancellationToken);

            if (storedToken is null)
                return Results.NoContent();

            if (storedToken.RevokedAt is null)
            {
                storedToken.RevokedAt = DateTime.UtcNow;

                await db.SaveChangesAsync(
                    cancellationToken);
            }

            return Results.NoContent();
        });

        return app;
    }

    private static string CreateAccessToken(
        User user,
        IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "JWT key is not configured.");

        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];

        const int expiresIn = 15 * 60;

        var claims = new[]
        {
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email)
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddSeconds(expiresIn),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}