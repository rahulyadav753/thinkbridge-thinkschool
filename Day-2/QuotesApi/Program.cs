using Microsoft.EntityFrameworkCore;
using QuotesApi.Data;
using QuotesApi.Extensions;
using QuotesApi.Repositories;
using QuotesApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddInfrastructure(builder.Configuration);

// Repositories - Scoped
builder.Services.AddScoped<IQuoteRepository, QuoteRepository>();
builder.Services.AddScoped<ICollectionRepository, CollectionRepository>();

// DI lifetime exercise
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddTransient<QuoteFormatter>();

var app = builder.Build();

// API endpoints
app.MapQuoteEndpoints();
app.MapCollectionEndpoints();

// Create/update database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QuotesDbContext>();
    db.Database.Migrate();
}

app.Run();