using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using Ordering.IntegrationTests.Infrastructure;
using Xunit;

namespace Ordering.IntegrationTests;

[Collection(OrderingCollection.Name)]
public class CatalogFailureTests : OrderingTestBase
{
    public CatalogFailureTests(ApiFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData(StatusCode.Unavailable)]
    [InlineData(StatusCode.DeadlineExceeded)]
    public async Task Create_WhenCatalogIsDown_Returns503WithRetryAfterAndStoresNothing(StatusCode catalogFailure)
    {
        var userId = Guid.NewGuid();
        var item = Factory.Catalog.AddItem("Unreachable", 10, 10);
        var client = CreateClient(userId);

        HttpResponseMessage response;
        using (Factory.Catalog.FailWith(catalogFailure))
        {
            response = await client.PostAsJsonAsync("/api/orders", OrderRequest((Guid.Parse(item.Id), 1)));
        }

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter.Should().NotBeNull();
        var body = await ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(503);
        body.GetProperty("title").GetString().Should().Be("Сервис временно недоступен");
        body.GetProperty("detail").GetString().Should().Be("Каталог товаров временно недоступен. Повторите запрос позже.");

        await Factory.WithDbContextAsync(async db =>
        {
            (await db.Orders.CountAsync(o => o.UserId == userId)).Should().Be(0);
        });
    }

    [Fact]
    public async Task Create_WithRepeatedIdempotencyKey_StillReplaysWhenCatalogIsDown()
    {
        var item = Factory.Catalog.AddItem("Replayable", 10, 10);
        var client = CreateClient(Guid.NewGuid());
        var key = Guid.NewGuid().ToString();

        async Task<HttpResponseMessage> Send()
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
            {
                Content = JsonContent.Create(OrderRequest((Guid.Parse(item.Id), 1)))
            };
            request.Headers.Add("Idempotency-Key", key);
            return await client.SendAsync(request);
        }

        var first = await Send();
        HttpResponseMessage replay;
        using (Factory.Catalog.FailWith(StatusCode.Unavailable))
        {
            replay = await Send();
        }

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        replay.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ReadJsonAsync(replay)).GetProperty("id").GetGuid()
            .Should().Be((await ReadJsonAsync(first)).GetProperty("id").GetGuid());
    }

    [Fact]
    public async Task Health_WhenCatalogIsReachable_Returns200()
    {
        var response = await CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("service").GetString().Should().Be("OrderingService");
        body.GetProperty("status").GetString().Should().Be("Healthy");
        body.GetProperty("database").GetString().Should().Be("Connected");
        body.GetProperty("catalog").GetString().Should().Be("Reachable");
    }

    [Fact]
    public async Task Health_WhenCatalogIsDown_Returns503()
    {
        HttpResponseMessage response;
        using (Factory.Catalog.FailWith(StatusCode.Unavailable))
        {
            response = await CreateClient().GetAsync("/health");
        }

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await ReadJsonAsync(response);
        body.GetProperty("status").GetString().Should().Be("Unhealthy");
        body.GetProperty("catalog").GetString().Should().Be("Unreachable");
    }
}
