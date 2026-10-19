using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using OrderingService.Domain;
using OrderingService.Features.Orders.DTOs;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderingService.Common;

public class OrderExamplesSchemaFilter : ISchemaFilter
{
    private const string ExampleProductId = "11111111-2222-3333-4444-555555555553";
    private const string ExampleUserId = "720c0c72-a29a-4d41-8726-51555434e0c5";
    private const string ExampleOrderId = "9865c956-196e-45f2-a8bf-8fddd1fea683";

    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema concrete)
        {
            return;
        }

        var example = BuildExample(context.Type);
        if (example is not null)
        {
            concrete.Examples = new List<JsonNode> { example };
        }
    }

    private static JsonNode? BuildExample(Type type)
    {
        if (type == typeof(CreateOrderRequest))
        {
            return new JsonObject
            {
                ["items"] = new JsonArray(new JsonObject
                {
                    ["productId"] = ExampleProductId,
                    ["quantity"] = 2
                })
            };
        }

        if (type == typeof(UpdateOrderStatusRequest))
        {
            return new JsonObject { ["status"] = (int)OrderStatus.Shipped };
        }

        if (type == typeof(OrderResponse))
        {
            return new JsonObject
            {
                ["id"] = ExampleOrderId,
                ["orderNumber"] = "ORD-20261015-7D89D356",
                ["userId"] = ExampleUserId,
                ["status"] = (int)OrderStatus.Pending,
                ["totalAmount"] = 99.98,
                ["createdAtUtc"] = "2026-10-15T17:21:15.518Z",
                ["items"] = new JsonArray(new JsonObject
                {
                    ["productId"] = ExampleProductId,
                    ["productName"] = "Domain-Driven Design Book",
                    ["quantity"] = 2,
                    ["unitPrice"] = 49.99,
                    ["totalPrice"] = 99.98
                })
            };
        }

        return null;
    }
}
