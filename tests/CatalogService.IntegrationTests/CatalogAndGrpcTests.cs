using System.Net;
using System.Net.Http.Json;
using CatalogService.Features.Categories.DTOs;
using CatalogService.Features.Items.DTOs;
using CatalogService.IntegrationTests.Infrastructure;
using ECommerce.Shared.Common;
using ECommerce.Shared.Protos;
using ECommerce.Shared.Security;
using FluentAssertions;
using Xunit;

namespace CatalogService.IntegrationTests;

public class CatalogAndGrpcTests : IClassFixture<CatalogTestFactory>
{
    private readonly CatalogTestFactory _factory;

    public CatalogAndGrpcTests(CatalogTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetItemsAndCategories_WithoutToken_Returns200Ok()
    {
        var client = _factory.CreateClient();

        var itemsResponse = await client.GetAsync("/api/v1/catalog/items");
        var categoriesResponse = await client.GetAsync("/api/v1/catalog/categories");

        itemsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        categoriesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var categories = await categoriesResponse.Content.ReadFromJsonAsync<IReadOnlyList<CategoryDto>>();
        categories.Should().NotBeNull();
        categories.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetItems_WithSearchCategoryAndPriceFilter_ReturnsFilteredPagedItems()
    {
        var client = _factory.CreateClient();

        var paged = await client.GetFromJsonAsync<PagedResult<ItemDto>>(
            "/api/v1/catalog/items?search=Smartphone&minPrice=500&maxPrice=1000&page=1&pageSize=5");

        paged.Should().NotBeNull();
        paged!.Items.Should().Contain(i => i.Title.Contains("Smartphone"));
        paged.Items.Should().OnlyContain(i => i.Price >= 500m && i.Price <= 1000m);
    }

    [Fact]
    public async Task CreateItem_WithoutToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var request = new CreateItemRequest("New Laptop", "SKU-UNAUTH", "Desc", 1200m, 10, Guid.NewGuid());

        var response = await client.PostAsJsonAsync("/api/v1/catalog/items", request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateItem_AsRegularUser_Returns403Forbidden()
    {
        var userClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.User);
        var request = new CreateItemRequest("New Laptop", "SKU-FORBID", "Desc", 1200m, 10, Guid.NewGuid());

        var response = await userClient.PostAsJsonAsync("/api/v1/catalog/items", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateItem_WithInvalidPayload_Returns400ValidationProblem()
    {
        var managerClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.CatalogManager);
        var request = new CreateItemRequest("", "", "Desc", -10m, -5, Guid.Empty);

        var response = await managerClient.PostAsJsonAsync("/api/v1/catalog/items", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateItem_WithDuplicateSku_Returns409Conflict()
    {
        var managerClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.CatalogManager);
        var categories = await managerClient.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/v1/catalog/categories");
        var categoryId = categories!.First().Id;
        var sku = $"DUP-{Guid.NewGuid():N}"[..12];

        var firstResponse = await managerClient.PostAsJsonAsync(
            "/api/v1/catalog/items",
            new CreateItemRequest("First Item", sku, "Desc", 100m, 5, categoryId));
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await managerClient.PostAsJsonAsync(
            "/api/v1/catalog/items",
            new CreateItemRequest("Second Item", sku, "Desc", 150m, 10, categoryId));
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUpdateAndSoftDeleteItem_AsCatalogManager_WorksEndToEnd()
    {
        var managerClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.CatalogManager);

        var categories = await managerClient.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/v1/catalog/categories");
        var categoryId = categories!.First().Id;
        var sku = $"SKU-{Guid.NewGuid():N}"[..12];

        var createResponse = await managerClient.PostAsJsonAsync(
            "/api/v1/catalog/items",
            new CreateItemRequest("Gaming Monitor 4K", sku, "144Hz HDR", 799.99m, 15, categoryId));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<ItemDto>())!;
        created.Title.Should().Be("Gaming Monitor 4K");
        created.Price.Should().Be(799.99m);
        created.StockQuantity.Should().Be(15);

        var updateResponse = await managerClient.PutAsJsonAsync(
            $"/api/v1/catalog/items/{created.Id}",
            new UpdateItemRequest("Gaming Monitor 4K Pro", sku, "240Hz HDR", 899.99m, 25, categoryId));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetched = await managerClient.GetFromJsonAsync<ItemDto>($"/api/v1/catalog/items/{created.Id}");
        fetched!.Title.Should().Be("Gaming Monitor 4K Pro");
        fetched.Price.Should().Be(899.99m);
        fetched.StockQuantity.Should().Be(25);

        var deleteResponse = await managerClient.DeleteAsync($"/api/v1/catalog/items/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfterDelete = await managerClient.GetAsync($"/api/v1/catalog/items/{created.Id}");
        getAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateCategory_WithInvalidSlug_Returns400ValidationProblem()
    {
        var managerClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.CatalogManager);

        var response = await managerClient.PostAsJsonAsync(
            "/api/v1/catalog/categories",
            new CreateCategoryRequest("Invalid Slug Category", "Invalid Slug With Spaces!", "Desc"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CategoryCrud_AsCatalogManager_CreatesUpdatesAndDeletesEmptyCategory()
    {
        var managerClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.CatalogManager);
        var slug = $"cat-{Guid.NewGuid():N}"[..16];

        var createResponse = await managerClient.PostAsJsonAsync(
            "/api/v1/catalog/categories",
            new CreateCategoryRequest("Smart Home", slug, "Smart devices"));

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var category = (await createResponse.Content.ReadFromJsonAsync<CategoryDto>())!;
        category.Name.Should().Be("Smart Home");
        category.Slug.Should().Be(slug);

        var updateResponse = await managerClient.PutAsJsonAsync(
            $"/api/v1/catalog/categories/{category.Id}",
            new UpdateCategoryRequest("Smart Home & IoT", slug, "Updated description"));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetched = await managerClient.GetFromJsonAsync<CategoryDto>($"/api/v1/catalog/categories/{category.Id}");
        fetched!.Name.Should().Be("Smart Home & IoT");

        var deleteResponse = await managerClient.DeleteAsync($"/api/v1/catalog/categories/{category.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getDeleted = await managerClient.GetAsync($"/api/v1/catalog/categories/{category.Id}");
        getDeleted.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_WhenCategoryHasActiveItems_Returns409Conflict()
    {
        var managerClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.CatalogManager);
        var slug = $"nonempty-{Guid.NewGuid():N}"[..16];

        var catResponse = await managerClient.PostAsJsonAsync(
            "/api/v1/catalog/categories",
            new CreateCategoryRequest("Audio", slug, "Headphones & Speakers"));
        var category = (await catResponse.Content.ReadFromJsonAsync<CategoryDto>())!;

        var itemResponse = await managerClient.PostAsJsonAsync(
            "/api/v1/catalog/items",
            new CreateItemRequest("Studio Headphones", $"SKU-{Guid.NewGuid():N}"[..12], "Hi-Fi", 299m, 5, category.Id));
        itemResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var deleteResponse = await managerClient.DeleteAsync($"/api/v1/catalog/categories/{category.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Grpc_GetItemAndCheckStock_ReturnsPriceStockAndAvailability()
    {
        var grpcClient = _factory.CreateGrpcClient();

        var itemResponse = await grpcClient.GetItemAsync(new GetItemRequest
        {
            Id = "11111111-2222-3333-4444-555555555551"
        });

        itemResponse.IsActive.Should().BeTrue();
        itemResponse.Title.Should().Be("Smartphone X");
        itemResponse.Price.Should().Be(799.99);

        var stockResponse = await grpcClient.CheckStockAsync(new CheckStockRequest
        {
            ItemId = "11111111-2222-3333-4444-555555555551",
            Quantity = 2
        });

        stockResponse.IsAvailable.Should().BeTrue();
        stockResponse.AvailableQuantity.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Grpc_ReserveAndReleaseStock_AtomicallyUpdatesStockQuantity()
    {
        var managerClient = _factory.CreateAuthorizedClient(Guid.NewGuid(), RoleNames.CatalogManager);
        var categories = await managerClient.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/v1/catalog/categories");
        var categoryId = categories!.First().Id;

        var itemResponse = await managerClient.PostAsJsonAsync(
            "/api/v1/catalog/items",
            new CreateItemRequest("SSD 2TB", $"SKU-{Guid.NewGuid():N}"[..12], "NVMe", 199m, 10, categoryId));
        var item = (await itemResponse.Content.ReadFromJsonAsync<ItemDto>())!;

        var grpcClient = _factory.CreateGrpcClient();

        var reserveReq = new ReserveStockRequest();
        reserveReq.Items.Add(new StockItemQuantity { ItemId = item.Id.ToString(), Quantity = 4 });
        var reserveRes = await grpcClient.ReserveStockAsync(reserveReq);
        reserveRes.Success.Should().BeTrue();

        var afterReserve = await managerClient.GetFromJsonAsync<ItemDto>($"/api/v1/catalog/items/{item.Id}");
        afterReserve!.StockQuantity.Should().Be(6);

        var overReserveReq = new ReserveStockRequest();
        overReserveReq.Items.Add(new StockItemQuantity { ItemId = item.Id.ToString(), Quantity = 7 });
        var overReserveRes = await grpcClient.ReserveStockAsync(overReserveReq);
        overReserveRes.Success.Should().BeFalse();
        overReserveRes.AvailableQuantity.Should().Be(6);

        var afterFailedReserve = await managerClient.GetFromJsonAsync<ItemDto>($"/api/v1/catalog/items/{item.Id}");
        afterFailedReserve!.StockQuantity.Should().Be(6);

        var releaseReq = new ReleaseStockRequest();
        releaseReq.Items.Add(new StockItemQuantity { ItemId = item.Id.ToString(), Quantity = 4 });
        var releaseRes = await grpcClient.ReleaseStockAsync(releaseReq);
        releaseRes.Success.Should().BeTrue();

        var afterRelease = await managerClient.GetFromJsonAsync<ItemDto>($"/api/v1/catalog/items/{item.Id}");
        afterRelease!.StockQuantity.Should().Be(10);
    }
}
