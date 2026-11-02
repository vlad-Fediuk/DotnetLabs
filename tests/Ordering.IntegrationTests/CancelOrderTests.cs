using System.Net;
using System.Net.Http.Json;
using ECommerce.Shared.Security;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Ordering.IntegrationTests.Infrastructure;
using OrderingService.Domain;
using Xunit;

namespace Ordering.IntegrationTests;

[Collection(OrderingCollection.Name)]
public class CancelOrderTests : OrderingTestBase
{
    public CancelOrderTests(ApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Cancel_OwnOrder_ChangesStatusKeepsRecordAndReleasesStock()
    {
        var item = Factory.Catalog.AddItem("Cancelable", 10, 100);
        var client = CreateClient(Guid.NewGuid());
        var created = await CreateOrderAsync(client, Guid.Parse(item.Id), 3);
        var orderId = created.GetProperty("id").GetGuid();

        Factory.Catalog.GetStock(item.Id).Should().Be(97);

        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await ReadJsonAsync(response);
        body.GetProperty("id").GetGuid().Should().Be(orderId);
        body.GetProperty("status").GetInt32().Should().Be((int)OrderStatus.Cancelled);

        Factory.Catalog.GetStock(item.Id).Should().Be(100);

        await Factory.WithDbContextAsync(async db =>
        {
            var stored = await db.Orders.AsNoTracking().SingleAsync(o => o.Id == orderId);
            stored.Status.Should().Be(OrderStatus.Cancelled);
            stored.UpdatedAtUtc.Should().NotBeNull();
        });
    }

    [Fact]
    public async Task Cancel_Repeatedly_Returns200WithCancelledStatusAndReleasesStockOnce()
    {
        var item = Factory.Catalog.AddItem("Cancelable", 10, 50);
        var client = CreateClient(Guid.NewGuid());
        var orderId = (await CreateOrderAsync(client, Guid.Parse(item.Id), 5)).GetProperty("id").GetGuid();

        var first = await client.PostAsync($"/api/orders/{orderId}/cancel", null);
        var second = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(second)).GetProperty("status").GetInt32().Should().Be((int)OrderStatus.Cancelled);
        Factory.Catalog.GetStock(item.Id).Should().Be(50);
    }

    [Fact]
    public async Task Cancel_ForeignOrder_Returns404AndLeavesOrderUntouched()
    {
        var item = Factory.Catalog.AddItem("Not yours", 10, 100);
        var ownerClient = CreateClient(Guid.NewGuid());
        var orderId = (await CreateOrderAsync(ownerClient, Guid.Parse(item.Id), 1)).GetProperty("id").GetGuid();

        var response = await CreateClient(Guid.NewGuid()).PostAsync($"/api/orders/{orderId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var stillPending = await ReadJsonAsync(await ownerClient.GetAsync($"/api/orders/{orderId}"));
        stillPending.GetProperty("status").GetInt32().Should().Be((int)OrderStatus.Pending);
    }

    [Fact]
    public async Task Cancel_ShippedOrder_Returns409()
    {
        var item = Factory.Catalog.AddItem("Shipped", 10, 100);
        var client = CreateClient(Guid.NewGuid());
        var orderId = (await CreateOrderAsync(client, Guid.Parse(item.Id), 1)).GetProperty("id").GetGuid();
        var adminClient = CreateClient(Guid.NewGuid(), RoleNames.Admin);
        (await adminClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { status = (int)OrderStatus.Shipped }))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PostAsync($"/api/orders/{orderId}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(response)).GetProperty("title").GetString().Should().Be("Конфлікт даних");
    }

    [Fact]
    public async Task Cancel_WithoutToken_Returns401()
    {
        var response = await CreateClient().PostAsync($"/api/orders/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateStatus_AsAdmin_Returns200WithNewStatus()
    {
        var item = Factory.Catalog.AddItem("Admin updated", 10, 100);
        var orderId = (await CreateOrderAsync(CreateClient(Guid.NewGuid()), Guid.Parse(item.Id), 1)).GetProperty("id").GetGuid();
        var adminClient = CreateClient(Guid.NewGuid(), RoleNames.Admin);

        var response = await adminClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { status = (int)OrderStatus.Paid });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("status").GetInt32().Should().Be((int)OrderStatus.Paid);
    }

    [Fact]
    public async Task UpdateStatus_AsOrderOperatorRole_Returns200ViaPbac()
    {
        var item = Factory.Catalog.AddItem("Operator updated", 10, 100);
        var orderId = (await CreateOrderAsync(CreateClient(Guid.NewGuid()), Guid.Parse(item.Id), 1)).GetProperty("id").GetGuid();
        var operatorClient = CreateClient(Guid.NewGuid(), RoleNames.OrderOperator);

        var response = await operatorClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { status = (int)OrderStatus.Paid });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadJsonAsync(response)).GetProperty("status").GetInt32().Should().Be((int)OrderStatus.Paid);
    }

    [Fact]
    public async Task UpdateStatus_AsRegularUser_Returns403()
    {
        var item = Factory.Catalog.AddItem("Protected", 10, 100);
        var userClient = CreateClient(Guid.NewGuid());
        var orderId = (await CreateOrderAsync(userClient, Guid.Parse(item.Id), 1)).GetProperty("id").GetGuid();

        var response = await userClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { status = (int)OrderStatus.Paid });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateStatus_OfCancelledOrder_Returns409()
    {
        var item = Factory.Catalog.AddItem("Final", 10, 100);
        var client = CreateClient(Guid.NewGuid());
        var orderId = (await CreateOrderAsync(client, Guid.Parse(item.Id), 1)).GetProperty("id").GetGuid();
        await client.PostAsync($"/api/orders/{orderId}/cancel", null);
        var adminClient = CreateClient(Guid.NewGuid(), RoleNames.Admin);

        var response = await adminClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { status = (int)OrderStatus.Paid });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await ReadJsonAsync(response)).GetProperty("detail").GetString()
            .Should().Be("Неможливо змінити статус вже скасованого замовлення.");
    }

    [Fact]
    public async Task UpdateStatus_BackwardFromShippedToPending_Returns409FsmViolation()
    {
        var item = Factory.Catalog.AddItem("FSM Item", 10, 100);
        var client = CreateClient(Guid.NewGuid());
        var orderId = (await CreateOrderAsync(client, Guid.Parse(item.Id), 1)).GetProperty("id").GetGuid();
        var adminClient = CreateClient(Guid.NewGuid(), RoleNames.Admin);

        await adminClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { status = (int)OrderStatus.Shipped });
        var backwardResponse = await adminClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new { status = (int)OrderStatus.Pending });

        backwardResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
