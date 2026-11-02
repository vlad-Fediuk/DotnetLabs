using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ordering.IntegrationTests.Infrastructure;
using OrderingService.Domain;
using Xunit;

namespace Ordering.IntegrationTests;

[Collection(OrderingCollection.Name)]
public class CreateOrderTests : OrderingTestBase
{
    public CreateOrderTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Create_WithValidData_Returns201WithLocationBodyAndPersistedSnapshot()
    {
        var userId = Guid.NewGuid();
        var item = Factory.Catalog.AddItem("Domain-Driven Design Book", 49.99, 100);
        var client = CreateClient(userId);

        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest((Guid.Parse(item.Id), 2)));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await ReadJsonAsync(response);
        var orderId = body.GetProperty("id").GetGuid();
        response.Headers.Location!.ToString().Should().Be($"/api/orders/{orderId}");
        body.GetProperty("orderNumber").GetString().Should().StartWith("ORD-");
        body.GetProperty("userId").GetGuid().Should().Be(userId);
        body.GetProperty("status").GetInt32().Should().Be((int)OrderStatus.Pending);
        body.GetProperty("totalAmount").GetDecimal().Should().Be(99.98m);
        body.GetProperty("createdAtUtc").GetDateTime().Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));

        var line = body.GetProperty("items").EnumerateArray().Single();
        line.GetProperty("productId").GetString().Should().Be(item.Id);
        line.GetProperty("productName").GetString().Should().Be("Domain-Driven Design Book");
        line.GetProperty("quantity").GetInt32().Should().Be(2);
        line.GetProperty("unitPrice").GetDecimal().Should().Be(49.99m);
        line.GetProperty("totalPrice").GetDecimal().Should().Be(99.98m);

        await Factory.WithDbContextAsync(async db =>
        {
            var stored = await db.Orders.Include(o => o.Items).SingleAsync(o => o.Id == orderId);
            stored.UserId.Should().Be(userId);
            stored.Status.Should().Be(OrderStatus.Pending);
            stored.Items.Single().ProductName.Should().Be("Domain-Driven Design Book");
            stored.Items.Single().UnitPrice.Should().Be(49.99m);
        });
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var item = Factory.Catalog.AddItem("Book", 10, 10);
        var client = CreateClient();

        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest((Guid.Parse(item.Id), 1)));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Create_WithUnknownItem_Returns404ProblemWithOriginalMessage()
    {
        var client = CreateClient(Guid.NewGuid());
        var missingId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest((missingId, 1)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(404);
        body.GetProperty("title").GetString().Should().Be("Ресурс не найден");
        body.GetProperty("detail").GetString().Should().Be($"Товар с Id '{missingId}' не найден.");
        body.GetProperty("instance").GetString().Should().Be("/api/orders");
    }

    [Fact]
    public async Task Create_WithInactiveItem_Returns404()
    {
        var item = Factory.Catalog.AddItem("Retired", 10, 10, isActive: false);
        var client = CreateClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest((Guid.Parse(item.Id), 1)));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithNonPositiveQuantity_Returns400ValidationProblem()
    {
        var item = Factory.Catalog.AddItem("Book", 10, 10);
        var client = CreateClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest((Guid.Parse(item.Id), 0)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ReadJsonAsync(response);
        body.GetProperty("status").GetInt32().Should().Be(400);
        body.GetProperty("title").GetString().Should().Be("One or more validation errors occurred.");
        body.GetProperty("errors").GetProperty("Items[0].Quantity")[0].GetString()
            .Should().Be("Количество товара должно быть больше 0");
    }

    [Fact]
    public async Task Create_WithEmptyItems_Returns400ValidationProblem()
    {
        var client = CreateClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await ReadJsonAsync(response);
        body.GetProperty("errors").GetProperty("Items")[0].GetString()
            .Should().Be("Заказ должен содержать хотя бы один товар");
    }

    [Fact]
    public async Task Create_WithQuantityAboveStock_Returns409Conflict()
    {
        var item = Factory.Catalog.AddItem("Scarce", 10, 3);
        var client = CreateClient(Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/orders", OrderRequest((Guid.Parse(item.Id), 5)));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await ReadJsonAsync(response);
        body.GetProperty("title").GetString().Should().Be("Конфликт данных");
        body.GetProperty("detail").GetString().Should().Be("Недостаточно товара 'Scarce' на складе. Доступно: 3, запрошено: 5.");
    }

    [Fact]
    public async Task Create_RepeatedWithSameIdempotencyKey_ReturnsSameOrderAndStoresOne()
    {
        var userId = Guid.NewGuid();
        var item = Factory.Catalog.AddItem("Book", 25, 100);
        var client = CreateClient(userId);
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
        var second = await Send();

        first.StatusCode.Should().Be(HttpStatusCode.Created);
        second.StatusCode.Should().Be(HttpStatusCode.Created);
        var firstBody = await ReadJsonAsync(first);
        var secondBody = await ReadJsonAsync(second);
        secondBody.GetProperty("id").GetGuid().Should().Be(firstBody.GetProperty("id").GetGuid());
        secondBody.GetProperty("orderNumber").GetString().Should().Be(firstBody.GetProperty("orderNumber").GetString());
        secondBody.GetProperty("totalAmount").GetDecimal().Should().Be(25m);
        second.Headers.GetValues("Idempotency-Replayed").Should().ContainSingle().Which.Should().Be("true");
        first.Headers.Contains("Idempotency-Replayed").Should().BeFalse();

        await Factory.WithDbContextAsync(async db =>
        {
            (await db.Orders.CountAsync(o => o.UserId == userId)).Should().Be(1);
        });
    }

    [Fact]
    public async Task Create_WithSameIdempotencyKeyFromDifferentUsers_CreatesSeparateOrders()
    {
        var item = Factory.Catalog.AddItem("Book", 25, 100);
        var key = Guid.NewGuid().ToString();

        async Task<Guid> CreateAs(Guid userId)
        {
            var client = CreateClient(userId);
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/orders")
            {
                Content = JsonContent.Create(OrderRequest((Guid.Parse(item.Id), 1)))
            };
            request.Headers.Add("Idempotency-Key", key);
            var response = await client.SendAsync(request);
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            return (await ReadJsonAsync(response)).GetProperty("id").GetGuid();
        }

        var firstOrder = await CreateAs(Guid.NewGuid());
        var secondOrder = await CreateAs(Guid.NewGuid());

        secondOrder.Should().NotBe(firstOrder);
    }

    [Fact]
    public async Task Create_IgnoresUserIdSuppliedInBody()
    {
        var tokenUser = Guid.NewGuid();
        var spoofedUser = Guid.NewGuid();
        var item = Factory.Catalog.AddItem("Book", 10, 10);
        var client = CreateClient(tokenUser);

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            userId = spoofedUser,
            items = new[] { new { productId = Guid.Parse(item.Id), quantity = 1 } }
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await ReadJsonAsync(response)).GetProperty("userId").GetGuid().Should().Be(tokenUser);
    }
}
