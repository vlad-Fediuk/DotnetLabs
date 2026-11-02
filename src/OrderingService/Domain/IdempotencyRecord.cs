namespace OrderingService.Domain;

public class IdempotencyRecord
{
    public string Key { get; set; } = string.Empty;
    public Guid OrderId { get; set; }
    public int ResponseStatusCode { get; set; }
    public string ResponseBody { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
