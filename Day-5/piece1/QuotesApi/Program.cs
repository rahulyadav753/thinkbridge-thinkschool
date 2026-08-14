using System.Diagnostics;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddSource("QuotesApi")
            .AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
            });
    });

var app = builder.Build();

var activitySource = new ActivitySource("QuotesApi");

app.MapGet("/", () => "Hello World!");

app.MapGet("/slow", () =>
{
    using var activity = activitySource.StartActivity("IntentionalSlowOperation");

    return Results.Ok(new
    {
        message = "Slow operation completed"
    });
});

app.Run();