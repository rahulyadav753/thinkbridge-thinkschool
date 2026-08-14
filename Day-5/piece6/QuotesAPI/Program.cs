using Microsoft.Extensions.Http.Resilience;
using Polly;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient("my-service", client =>
{
    client.BaseAddress = new Uri("https://localhost:59999");
})
.AddResilienceHandler("default", resilienceBuilder =>
{
    resilienceBuilder.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        OnRetry = args =>
        {
            Console.WriteLine(
                $"RETRY: Attempt {args.AttemptNumber + 1}, " +
                $"Delay: {args.RetryDelay.TotalMilliseconds}ms");

            return default;
        }
    });

    resilienceBuilder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 2,
        BreakDuration = TimeSpan.FromSeconds(10)
    });

    resilienceBuilder.AddTimeout(TimeSpan.FromSeconds(10));
});

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/health", () =>
    Results.Ok(new { status = "Healthy" }));

app.MapGet("/test-resilience", async (IHttpClientFactory factory) =>
{
    var client = factory.CreateClient("my-service");

    try
    {
        var response = await client.GetAsync("/test");
        return Results.Ok(new
        {
            status = (int)response.StatusCode
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"FINAL FAILURE: {ex.Message}");

        return Results.Problem(
            "External service failed after resilience attempts.");
    }
});

app.Run();