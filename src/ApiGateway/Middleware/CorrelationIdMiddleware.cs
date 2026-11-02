namespace ApiGateway.Middleware;

public class CorrelationIdMiddleware
{
    public const string CorrelationHeaderName = "X-Correlation-Id";
    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate _nextDelegate)
    {
        _next = _nextDelegate;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[CorrelationHeaderName].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("D");
            context.Request.Headers[CorrelationHeaderName] = correlationId;
        }

        context.Response.Headers[CorrelationHeaderName] = correlationId;
        context.Items[CorrelationHeaderName] = correlationId;

        await _next(context);
    }
}
