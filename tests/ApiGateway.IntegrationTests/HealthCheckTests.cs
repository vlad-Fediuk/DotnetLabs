using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ApiGateway.IntegrationTests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace ApiGateway.IntegrationTests;

public class HealthCheckTests : IClassFixture<GatewayTestFactory>
{
    private readonly GatewayTestFactory _factory;

    public HealthCheckTests(GatewayTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task HealthCheck_КолиВсіСервісиДоступні_Повертає200ТаСтатусHealthy()
    {
        _factory.Reset();
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("service").GetString().Should().Be("ApiGateway");
        body.GetProperty("status").GetString().Should().Be("Healthy");
        body.GetProperty("dependencies").GetProperty("catalogService").GetString().Should().Be("Healthy");
        body.GetProperty("dependencies").GetProperty("orderingService").GetString().Should().Be("Healthy");
        body.GetProperty("dependencies").GetProperty("authService").GetString().Should().Be("Healthy");
    }

    [Fact]
    public async Task HealthCheck_КолиСервісНедоступний_Повертає503ТаДетальнийСтатус()
    {
        _factory.Reset();
        _factory.CatalogStub.IsHealthy = false;
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("service").GetString().Should().Be("ApiGateway");
        body.GetProperty("status").GetString().Should().Be("Degraded");
        body.GetProperty("dependencies").GetProperty("catalogService").GetString().Should().Be("Unreachable");
        body.GetProperty("dependencies").GetProperty("orderingService").GetString().Should().Be("Healthy");
    }
}
