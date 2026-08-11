using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuotesApi.Data;
using QuotesApi.Models;

namespace QuotesApi.Extensions;

public static class AuthEndpointExtensions
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder app,
        IConfiguration configuration)
    {
        var group = app.MapGroup("/api/auth");

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

            var jwtKey = configuration["Jwt:Key"]
                ?? throw new InvalidOperationException(
                    "JWT key is not configured.");

            var issuer = configuration["Jwt:Issuer"];
            var audience = configuration["Jwt:Audience"];

            var expiresIn = 15 * 60;
            var expires = DateTime.UtcNow.AddSeconds(expiresIn);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
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
                expires: expires,
                signingCredentials: credentials);

            var accessToken = new JwtSecurityTokenHandler()
                .WriteToken(token);

            var refreshToken = Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32));

            return Results.Ok(
                new LoginResponse(
                    accessToken,
                    refreshToken,
                    expiresIn));
        });

        return app;
    }
}