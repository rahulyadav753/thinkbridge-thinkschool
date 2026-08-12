using Microsoft.AspNetCore.Mvc.Testing;

namespace QuotesApi.Tests;

public class CollectionCancellationTests
{
    [Fact]
    public async Task CancelledRequest_DoesNotComplete()
    {
        await using var factory = new WebApplicationFactory<Program>();

        using var client = factory.CreateClient();

        using var cancellationSource = new CancellationTokenSource();

        cancellationSource.Cancel();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/collections/1/items")
        {
            Content = new StringContent(
                """
                {
                    "quoteId": 1
                }
                """,
                System.Text.Encoding.UTF8,
                "application/json")
        };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            async () =>
            {
                await client.SendAsync(
                    request,
                    cancellationSource.Token);
            });
    }
}