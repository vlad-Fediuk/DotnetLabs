using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderingService.Domain;

namespace OrderingService.Data;

public class LegacyOrderImporter
{
    private readonly OrderingDbContext _dbContext;
    private readonly string _legacyConnectionString;
    private readonly ILogger<LegacyOrderImporter> _logger;

    public LegacyOrderImporter(OrderingDbContext dbContext, string legacyConnectionString, ILogger<LegacyOrderImporter> logger)
    {
        _dbContext = dbContext;
        _legacyConnectionString = legacyConnectionString;
        _logger = logger;
    }

    public async Task<int> ImportAsync(CancellationToken cancellationToken)
    {
        if (await _dbContext.Orders.AnyAsync(cancellationToken))
        {
            _logger.LogInformation("Legacy import skipped: orders already exist in the service database");
            return 0;
        }

        try
        {
            await using var connection = new NpgsqlConnection(_legacyConnectionString);
            await connection.OpenAsync(cancellationToken);

            var orders = await ReadOrdersAsync(connection, cancellationToken);
            var itemsByOrder = await ReadItemsAsync(connection, cancellationToken);

            foreach (var order in orders)
            {
                if (itemsByOrder.TryGetValue(order.Id, out var items))
                {
                    order.Items = items;
                }
            }

            _dbContext.Orders.AddRange(orders);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Imported {OrderCount} legacy orders", orders.Count);
            return orders.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Legacy orders import skipped or failed: {Message}", ex.Message);
            return 0;
        }
    }

    private static async Task<List<Order>> ReadOrdersAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = "SELECT \"Id\", \"OrderNumber\", \"UserId\", \"Status\", \"TotalAmount\", \"CreatedAtUtc\" FROM \"Orders\" ORDER BY \"CreatedAtUtc\"";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var orders = new List<Order>();
        while (await reader.ReadAsync(cancellationToken))
        {
            orders.Add(new Order
            {
                Id = reader.GetGuid(0),
                OrderNumber = reader.GetString(1),
                UserId = reader.GetGuid(2),
                Status = Enum.Parse<OrderStatus>(reader.GetString(3)),
                TotalAmount = reader.GetDecimal(4),
                CreatedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(5), DateTimeKind.Utc)
            });
        }

        return orders;
    }

    private static async Task<Dictionary<Guid, List<OrderItem>>> ReadItemsAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = "SELECT oi.\"Id\", oi.\"OrderId\", oi.\"ProductId\", COALESCE(NULLIF(oi.\"ProductName\", ''), p.\"Name\", ''), oi.\"UnitPrice\", oi.\"Quantity\" FROM \"OrderItems\" oi LEFT JOIN \"Products\" p ON p.\"Id\" = oi.\"ProductId\"";

        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var itemsByOrder = new Dictionary<Guid, List<OrderItem>>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var orderId = reader.GetGuid(1);
            if (!itemsByOrder.TryGetValue(orderId, out var items))
            {
                items = new List<OrderItem>();
                itemsByOrder[orderId] = items;
            }

            items.Add(new OrderItem
            {
                Id = reader.GetGuid(0),
                OrderId = orderId,
                ProductId = reader.GetGuid(2),
                ProductName = reader.GetString(3),
                UnitPrice = reader.GetDecimal(4),
                Quantity = reader.GetInt32(5)
            });
        }

        return itemsByOrder;
    }
}
