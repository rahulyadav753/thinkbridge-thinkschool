using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using QuotesApi.Data;
using QuotesApi.Services;

namespace Quotes.Tests.Integration;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private SqliteConnection? _connection;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the production database registration.
            services.RemoveAll<DbContextOptions<QuotesDbContext>>();

            // Create a fresh SQLite in-memory database.
            _connection = new SqliteConnection("Data Source=:memory:");
            _connection.Open();

            services.AddDbContext<QuotesDbContext>(options =>
            {
                options.UseSqlite(_connection);
            });

            // Replace the real clock with a fake clock.
            services.RemoveAll<IClock>();

            services.AddSingleton<IClock, FakeClock>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        _connection?.Dispose();

        base.Dispose(disposing);
    }
}

public sealed class FakeClock : IClock
{
    public DateTimeOffset UtcNow { get; set; } =
        new DateTimeOffset(
            2026,
            8,
            12,
            12,
            0,
            0,
            TimeSpan.Zero);
}