using System.Net;
using System.Net.Http.Json;
using ApiGateway.DTOs;
using ApiGateway.IntegrationTests.Infrastructure;
using ECommerce.Shared.Security;
using FluentAssertions;
using Xunit;

namespace ApiGateway.IntegrationTests;

public class CachingAndPersonalDataTests : IClassFixture<GatewayTestFactory>
{
    private readonly GatewayTestFactory _factory;

    public CachingAndPersonalDataTests(GatewayTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ПовторнийЗапитТихСамихСпільнихДаних_ВикористовуєКешІНеВикликаєСервісПовторно()
    {
        _factory.Reset();
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var item = new CatalogItemSummaryDto(itemId, "Logitech MX Master 3S", "LOGI-MX", "Mouse", 99m, 50, "Accessories", true);
        _factory.CatalogStub.SetItem(item);

        var client = _factory.CreateAuthorizedClient(userId);

        var firstResponse = await client.GetAsync($"/api/v1/screens/item-details/{itemId}");
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.CatalogStub.CallCount.Should().Be(1);

        var secondResponse = await client.GetAsync($"/api/v1/screens/item-details/{itemId}");
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.CatalogStub.CallCount.Should().Be(1);

        var adminClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.Admin);
        var metricsResponse = await adminClient.GetAsync("/api/v1/cache/metrics");
        metricsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var metrics = await metricsResponse.Content.ReadFromJsonAsync<CacheMetricsDto>();
        metrics.Should().NotBeNull();
        metrics!.Hits.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task ДваРізніКористувачі_ТойСамийМаршрут_ОтримуютьВласніДані_ПерсональнеНеКешується()
    {
        _factory.Reset();
        var itemId = Guid.NewGuid();
        var user1Id = Guid.NewGuid();
        var user2Id = Guid.NewGuid();

        var item = new CatalogItemSummaryDto(itemId, "Dell UltraSharp 27", "DELL-U27", "Monitor", 600m, 15, "Monitors", true);
        _factory.CatalogStub.SetItem(item);

        var orderUser1 = new OrderSummaryDto(
            Guid.NewGuid(),
            "ORD-USER1-001",
            1,
            600m,
            DateTime.UtcNow,
            new List<OrderItemSummaryDto> { new(itemId, "Dell UltraSharp 27", 1, 600m, 600m) }
        );

        var orderUser2 = new OrderSummaryDto(
            Guid.NewGuid(),
            "ORD-USER2-002",
            1,
            1200m,
            DateTime.UtcNow,
            new List<OrderItemSummaryDto> { new(itemId, "Dell UltraSharp 27", 2, 600m, 1200m) }
        );

        _factory.OrderingStub.SetUserOrders(user1Id, new List<OrderSummaryDto> { orderUser1 });
        _factory.OrderingStub.SetUserOrders(user2Id, new List<OrderSummaryDto> { orderUser2 });

        var clientUser1 = _factory.CreateAuthorizedClient(user1Id);
        var clientUser2 = _factory.CreateAuthorizedClient(user2Id);

        var response1 = await clientUser1.GetAsync($"/api/v1/screens/item-details/{itemId}");
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        var screen1 = await response1.Content.ReadFromJsonAsync<ItemDetailsScreenDto>();
        screen1!.MyActiveOperation.Should().NotBeNull();
        screen1.MyActiveOperation!.OrderNumber.Should().Be("ORD-USER1-001");
        screen1.MyActiveOperation.Quantity.Should().Be(1);

        var response2 = await clientUser2.GetAsync($"/api/v1/screens/item-details/{itemId}");
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        var screen2 = await response2.Content.ReadFromJsonAsync<ItemDetailsScreenDto>();
        screen2!.MyActiveOperation.Should().NotBeNull();
        screen2.MyActiveOperation!.OrderNumber.Should().Be("ORD-USER2-002");
        screen2.MyActiveOperation.Quantity.Should().Be(2);
    }

    [Fact]
    public async Task ІнвалідизаціяКешу_ПісляЗміниДаних_ОчищуєКешТаОновлюєЗначення()
    {
        _factory.Reset();
        var userId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        var initialItem = new CatalogItemSummaryDto(itemId, "Mechanical Keyboard", "KB-01", "Keyboard", 120m, 20, "Keyboards", true);
        _factory.CatalogStub.SetItem(initialItem);

        var client = _factory.CreateAuthorizedClient(userId);

        var firstResponse = await client.GetAsync($"/api/v1/screens/item-details/{itemId}");
        var screen1 = await firstResponse.Content.ReadFromJsonAsync<ItemDetailsScreenDto>();
        screen1!.Item.Price.Should().Be(120m);
        _factory.CatalogStub.CallCount.Should().Be(1);

        var updatedItem = new CatalogItemSummaryDto(itemId, "Mechanical Keyboard RGB", "KB-01", "Keyboard", 150m, 20, "Keyboards", true);
        _factory.CatalogStub.SetItem(updatedItem);

        var adminClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.Admin);
        var invalidateResponse = await adminClient.PostAsync("/api/v1/cache/invalidate", null);
        invalidateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var secondResponse = await client.GetAsync($"/api/v1/screens/item-details/{itemId}");
        var screen2 = await secondResponse.Content.ReadFromJsonAsync<ItemDetailsScreenDto>();
        screen2!.Item.Price.Should().Be(150m);
        screen2.Item.Title.Should().Be("Mechanical Keyboard RGB");
        _factory.CatalogStub.CallCount.Should().Be(2);
    }
}
