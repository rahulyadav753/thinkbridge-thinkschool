using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using QuotesApi.Data;
using QuotesApi.Services;
using Microsoft.Extensions.Hosting;

namespace Quotes.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext configuration
            var descriptors = services
                .Where(d =>
                    d.ServiceType == typeof(DbContextOptions<QuotesDbContext>) ||
                    d.ServiceType == typeof(QuotesDbContext))
                .ToList();

            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            // Fresh in-memory SQLite database for this factory/test
            _connection = new SqliteConnection(
                "Data Source=:memory:");

            _connection.Open();

            services.AddDbContext<QuotesDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Replace real clock
            var clockDescriptors = services
                .Where(d => d.ServiceType == typeof(IClock))
                .ToList();

            foreach (var descriptor in clockDescriptors)
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<IClock, FakeClock>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<QuotesDbContext>();

        // Apply migrations to fresh test database
        db.Database.Migrate();

        // Seed test user
        if (!db.Users.Any())
        {
            db.Users.Add(new QuotesApi.Models.User
            {
                Email = "test@example.com",
                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        "Password123!")
            });

            db.SaveChanges();
        }

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
        }

        base.Dispose(disposing);
    }

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow =>
            new(
                2026,
                8,
                12,
                12,
                0,
                0,
                TimeSpan.Zero);
    }
}