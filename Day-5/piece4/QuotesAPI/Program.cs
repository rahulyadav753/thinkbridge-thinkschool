using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .UseAzureMonitor()
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddSource("QuotesApi");
    });

var app = builder.Build();

var activitySource = new ActivitySource("QuotesApi");

app.MapGet("/", () => "Hello World!");

app.MapGet("/health", () =>
    Results.Ok(new { status = "Healthy" }));

app.Run();