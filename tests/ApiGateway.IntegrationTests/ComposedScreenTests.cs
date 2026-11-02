using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiGateway.DTOs;
using ApiGateway.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ApiGateway.IntegrationTests;

public class ComposedScreenTests : IClassFixture<GatewayTestFactory>
{
    private readonly GatewayTestFactory _factory;

    public ComposedScreenTests(GatewayTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task СкладенийЕндпоінт_КолиВсіСервісиДоступні_ПовертаєПовнуВідповідь()
    {
        _factory.Reset();
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var item = new CatalogItemSummaryDto(itemId, "MacBook Pro 16", "MBP16", "Apple Laptop", 2500m, 10, "Laptops", true);
        _factory.CatalogStub.SetItem(item);

        var order = new OrderSummaryDto(
            Guid.NewGuid(),
            "ORD-20261028-A1B2",
            1,
            2500m,
            DateTime.UtcNow,
            new List<OrderItemSummaryDto>
            {
                new(itemId, "MacBook Pro 16", 1, 2500m, 2500m)
            }
        );
        _factory.OrderingStub.SetUserOrders(userId, new List<OrderSummaryDto> { order });

        var client = _factory.CreateAuthorizedClient(userId);

        var response = await client.GetAsync($"/api/v1/screens/item-details/{itemId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var screen = await response.Content.ReadFromJsonAsync<ItemDetailsScreenDto>();
        screen.Should().NotBeNull();
        screen!.Item.Id.Should().Be(itemId);
        screen.Item.Title.Should().Be("MacBook Pro 16");
        screen.Item.Price.Should().Be(2500m);
        screen.ActiveOperationStatus.Should().Be("Available");
        screen.ActiveOperationMessage.Should().BeNull();
        screen.MyActiveOperation.Should().NotBeNull();
        screen.MyActiveOperation!.OrderId.Should().Be(order.Id);
        screen.MyActiveOperation.OrderNumber.Should().Be("ORD-20261028-A1B2");
        screen.MyActiveOperation.Quantity.Should().Be(1);
    }

    [Fact]
    public async Task СкладенийЕндпоінт_КолиСервісЗамовленьНедоступний_ПовертаєЧастковуВідповідьЗЯвнимСтатусом()
    {
        _factory.Reset();
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var item = new CatalogItemSummaryDto(itemId, "Sony WH-1000XM5", "SONY-XM5", "Headphones", 399m, 25, "Audio", true);
        _factory.CatalogStub.SetItem(item);
        _factory.OrderingStub.ShouldFail = true;

        var client = _factory.CreateAuthorizedClient(userId);

        var response = await client.GetAsync($"/api/v1/screens/item-details/{itemId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var screen = await response.Content.ReadFromJsonAsync<ItemDetailsScreenDto>();
        screen.Should().NotBeNull();
        screen!.Item.Id.Should().Be(itemId);
        screen.Item.Title.Should().Be("Sony WH-1000XM5");
        screen.ActiveOperationStatus.Should().Be("Unavailable");
        screen.ActiveOperationMessage.Should().Be("Сервіс замовлень тимчасово недоступний.");
        screen.MyActiveOperation.Should().BeNull();
    }

    [Fact]
    public async Task СкладенийЕндпоінт_КолиСервісКаталогуНедоступний_Повертає503ProblemDetails()
    {
        _factory.Reset();
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        _factory.CatalogStub.ShouldFail = true;

        var client = _factory.CreateAuthorizedClient(userId);

        var response = await client.GetAsync($"/api/v1/screens/item-details/{itemId}");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        response.Headers.RetryAfter.Should().NotBeNull();

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(503);
        problem.GetProperty("title").GetString().Should().Be("Сервіс тимчасово недоступний");
        problem.GetProperty("detail").GetString().Should().Be("Сервіс каталогу товарів тимчасово недоступний.");
    }

    [Fact]
    public async Task СкладенийЕндпоінт_КолиТоварНеІснує_Повертає404ProblemDetails()
    {
        _factory.Reset();
        var userId = Guid.NewGuid();
        var missingId = Guid.NewGuid();

        var client = _factory.CreateAuthorizedClient(userId);

        var response = await client.GetAsync($"/api/v1/screens/item-details/{missingId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        problem.GetProperty("status").GetInt32().Should().Be(404);
        problem.GetProperty("title").GetString().Should().Be("Ресурс не знайдено");
        problem.GetProperty("detail").GetString().Should().Be($"Товар з Id '{missingId}' не знайдено.");
    }
}
