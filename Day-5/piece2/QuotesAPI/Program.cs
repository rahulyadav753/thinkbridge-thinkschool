var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.MapGet("/", () => "Hello World!");

app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy"
}));

app.Run();