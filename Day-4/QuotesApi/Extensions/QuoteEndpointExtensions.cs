using QuotesApi.Models;
using QuotesApi.Repositories;

namespace QuotesApi.Extensions;

public static class QuoteEndpointExtensions
{
    private sealed record CreateQuoteRequest(string Author, string Text);

    public static IEndpointRouteBuilder MapQuoteEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/quotes");

        group.MapGet("/", async (
            int? page,
            int? size,
            IQuoteRepository repository,
            CancellationToken cancellationToken) =>
        {
            int currentPage = page ?? 1;
            int pageSize = size ?? 10;

            if (currentPage < 1 || pageSize < 1)
                return Results.BadRequest("Page and size must be greater than 0.");

            var quotes = await repository.GetQuotesAsync(
                currentPage,
                pageSize,
                cancellationToken);

            return Results.Ok(quotes);
        });

        group.MapGet("/{id:int}", async (
            int id,
            IQuoteRepository repository,
            CancellationToken cancellationToken) =>
        {
            var quote = await repository.GetByIdAsync(
                id,
                cancellationToken);

            return quote is null
                ? Results.NotFound()
                : Results.Ok(quote);
        });

        group.MapPost("/", async (
            CreateQuoteRequest request,
            IQuoteRepository repository,
            CancellationToken cancellationToken) =>
        {
            var creation = Quote.Create(request.Author, request.Text);

            if (!creation.IsSuccess)
                return Results.BadRequest(creation.Error);

            var createdQuote = await repository.AddAsync(
                creation.Quote!,
                cancellationToken);

            return Results.Created(
                $"/api/quotes/{createdQuote.Id}",
                createdQuote);
        })
        .RequireAuthorization();

        group.MapDelete("/{id:int}", async (
            int id,
            IQuoteRepository repository,
            CancellationToken cancellationToken) =>
        {
            var deleted = await repository.DeleteAsync(
                id,
                cancellationToken);

            return deleted
                ? Results.NoContent()
                : Results.NotFound();
        })
        .RequireAuthorization("can-edit-quotes");

        return app;
    }
}