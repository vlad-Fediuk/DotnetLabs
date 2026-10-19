using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Ordering.IntegrationTests.Infrastructure;
using Xunit;

namespace Ordering.IntegrationTests;

[Collection(OrderingCollection.Name)]
public class GetOrderTests : OrderingTestBase
{
    public GetOrderTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetById_OwnOrder_Returns200WithSameFieldsAsCreated()
    {
        var userId = Guid.NewGuid();
        var item = Factory.Catalog.AddItem("Headphones", 199.99, 100);
        var client = CreateClient(userId);
        var created = await CreateOrderAsync(client, Guid.Parse(item.Id), 1);

        var response = await client.GetAsync($"/api/orders/{created.GetProperty("id").GetGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("id").GetGuid().Should().Be(created.GetProperty("id").GetGuid());
        body.GetProperty("orderNumber").GetString().Should().Be(created.GetProperty("orderNumber").GetString());
        body.GetProperty("userId").GetGuid().Should().Be(userId);
        body.GetProperty("status").GetInt32().Should().Be(1);
        body.GetProperty("totalAmount").GetDecimal().Should().Be(199.99m);
        body.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "id", "orderNumber", "userId", "status", "totalAmount", "createdAtUtc", "items");
        body.GetProperty("items")[0].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "productId", "productName", "quantity", "unitPrice", "totalPrice");
    }

    [Fact]
    public async Task GetById_ForeignOrder_Returns404WithoutLeakingData()
    {
        var item = Factory.Catalog.AddItem("Private", 10, 100);
        var ownerClient = CreateClient(Guid.NewGuid());
        var created = await CreateOrderAsync(ownerClient, Guid.Parse(item.Id), 1);
        var orderId = created.GetProperty("id").GetGuid();
        var intruderClient = CreateClient(Guid.NewGuid());

        var response = await intruderClient.GetAsync($"/api/orders/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ReadJsonAsync(response);
        body.GetProperty("title").GetString().Should().Be("Ресурс не найден");
        body.GetProperty("detail").GetString().Should().Be($"Заказ с Id '{orderId}' не найден.");
        body.TryGetProperty("items", out _).Should().BeFalse();
        body.TryGetProperty("userId", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetById_ForeignOrderAsAdmin_Returns200()
    {
        var item = Factory.Catalog.AddItem("Visible to admin", 10, 100);
        var ownerId = Guid.NewGuid();
        var created = await CreateOrderAsync(CreateClient(ownerId), Guid.Parse(item.Id), 1);
        var adminClient = CreateClient(Guid.NewGuid(), "Admin");

        var response = await adminClient.GetAsync($"/api/orders/{created.GetProperty("id").GetGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("userId").GetGuid().Should().Be(ownerId);
    }

    [Fact]
    public async Task GetById_UnknownOrder_Returns404()
    {
        var client = CreateClient(Guid.NewGuid());

        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_WithoutToken_Returns401()
    {
        var response = await CreateClient().GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task List_ReturnsOnlyOwnOrdersNewestFirstWithExpectedFields()
    {
        var userId = Guid.NewGuid();
        var item = Factory.Catalog.AddItem("Listed", 5, 1000);
        var client = CreateClient(userId);
        var createdIds = new List<Guid>();
        for (var i = 1; i <= 3; i++)
        {
            createdIds.Add((await CreateOrderAsync(client, Guid.Parse(item.Id), i)).GetProperty("id").GetGuid());
            await Task.Delay(15);
        }

        await CreateOrderAsync(CreateClient(Guid.NewGuid()), Guid.Parse(item.Id), 1);

        var response = await client.GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetArrayLength().Should().Be(3);
        body.EnumerateArray().Select(o => o.GetProperty("id").GetGuid()).Should().Equal(Enumerable.Reverse(createdIds));
        body.EnumerateArray().Should().OnlyContain(o => o.GetProperty("userId").GetGuid() == userId);
        body[0].EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "id", "orderNumber", "userId", "status", "totalAmount", "createdAtUtc", "items");
    }

    [Fact]
    public async Task List_WithPagingAndStatusFilter_ReturnsRequestedSlice()
    {
        var userId = Guid.NewGuid();
        var item = Factory.Catalog.AddItem("Paged", 5, 1000);
        var client = CreateClient(userId);
        var orderIds = new List<Guid>();
        for (var i = 0; i < 3; i++)
        {
            orderIds.Add((await CreateOrderAsync(client, Guid.Parse(item.Id), 1)).GetProperty("id").GetGuid());
            await Task.Delay(15);
        }

        var cancelResponse = await client.PostAsync($"/api/orders/{orderIds[0]}/cancel", null);
        cancelResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondPage = await ReadJsonAsync(await client.GetAsync("/api/orders?page=2&pageSize=2"));
        var cancelled = await ReadJsonAsync(await client.GetAsync("/api/orders?status=5"));

        secondPage.GetArrayLength().Should().Be(1);
        secondPage[0].GetProperty("id").GetGuid().Should().Be(orderIds[0]);
        cancelled.GetArrayLength().Should().Be(1);
        cancelled[0].GetProperty("status").GetInt32().Should().Be(5);
    }

    [Fact]
    public async Task List_WithInvalidPageSize_Returns400()
    {
        var response = await CreateClient(Guid.NewGuid()).GetAsync("/api/orders?page=1&pageSize=0");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task List_WithoutToken_Returns401()
    {
        var response = await CreateClient().GetAsync("/api/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
