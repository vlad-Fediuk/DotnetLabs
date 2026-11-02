using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace Ordering.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public class OrderingCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Ordering";
}

public abstract class OrderingTestBase
{
    protected OrderingTestBase(ApiFactory factory)
    {
        Factory = factory;
    }

    protected ApiFactory Factory { get; }

    protected HttpClient CreateClient(Guid? userId = null, params string[] roles)
    {
        var client = Factory.CreateClient();
        if (userId is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Factory.Tokens.CreateToken(userId.Value, roles));
        }

        return client;
    }

    protected static object OrderRequest(params (Guid ProductId, int Quantity)[] items) => new
    {
        items = items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToArray()
    };

    protected static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body).RootElement.Clone();
    }

    protected async Task<JsonElement> CreateOrderAsync(HttpClient client, Guid productId, int quantity)
    {
        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest((productId, quantity)));
        response.EnsureSuccessStatusCode();
        return await ReadJsonAsync(response);
    }
}
