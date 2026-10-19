using ECommerce.Shared.Exceptions;
using ECommerce.Shared.Protos;
using Grpc.Core;

namespace OrderingService.Features.Orders;

public class CatalogLookup : ICatalogLookup
{
    private static readonly TimeSpan CallTimeout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(1);

    private readonly CatalogGrpcService.CatalogGrpcServiceClient _client;
    private readonly ILogger<CatalogLookup> _logger;

    public CatalogLookup(CatalogGrpcService.CatalogGrpcServiceClient client, ILogger<CatalogLookup> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<Guid, CatalogItemResponse>> GetItemsAsync(IEnumerable<Guid> itemIds, CancellationToken cancellationToken)
    {
        var request = new GetItemsRequest();
        request.Ids.AddRange(itemIds.Select(id => id.ToString()));

        try
        {
            var response = await _client.GetItemsAsync(
                request,
                deadline: DateTime.UtcNow.Add(CallTimeout),
                cancellationToken: cancellationToken);

            return response.Items.ToDictionary(i => Guid.Parse(i.Id));
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            throw new NotFoundException("Товар не найден.");
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.DeadlineExceeded or StatusCode.Unavailable)
        {
            _logger.LogWarning("Catalog gRPC call failed with {StatusCode}", ex.StatusCode);
            throw new ServiceUnavailableException("Каталог товаров временно недоступен. Повторите запрос позже.");
        }
    }

    public async Task<bool> IsReachableAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _client.CheckStockAsync(
                new CheckStockRequest { ItemId = Guid.Empty.ToString(), Quantity = 1 },
                deadline: DateTime.UtcNow.Add(ProbeTimeout),
                cancellationToken: cancellationToken);

            return true;
        }
        catch (RpcException ex) when (ex.StatusCode is StatusCode.NotFound or StatusCode.InvalidArgument)
        {
            return true;
        }
        catch (RpcException)
        {
            return false;
        }
    }
}
