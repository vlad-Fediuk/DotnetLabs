using ECommerce.Shared.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.Middleware;

public class GatewayProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GatewayProblemDetailsMiddleware> _logger;

    public GatewayProblemDetailsMiddleware(RequestDelegate next, ILogger<GatewayProblemDetailsMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            if (!context.Response.HasStarted && context.Response.StatusCode == StatusCodes.Status401Unauthorized)
            {
                context.Response.ContentType = "application/problem+json";
                var problem = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.2",
                    Title = "Не авторизовано",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "Токен автентифікації відсутній або недійсний.",
                    Instance = context.Request.Path
                };
                await context.Response.WriteAsJsonAsync(problem);
            }
            else if (!context.Response.HasStarted && context.Response.StatusCode == StatusCodes.Status403Forbidden)
            {
                context.Response.ContentType = "application/problem+json";
                var problem = new ProblemDetails
                {
                    Type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                    Title = "Доступ заборонено",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = "Недостатньо прав для виконання цієї операції.",
                    Instance = context.Request.Path
                };
                await context.Response.WriteAsJsonAsync(problem);
            }
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex, "Response has already started, cannot write ProblemDetails");
                throw;
            }

            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail, type) = exception switch
        {
            NotFoundException notFound => (
                StatusCodes.Status404NotFound,
                "Ресурс не знайдено",
                notFound.Message,
                "https://tools.ietf.org/html/rfc9110#section-15.5.5"),

            ServiceUnavailableException unavailable => (
                StatusCodes.Status503ServiceUnavailable,
                "Сервіс тимчасово недоступний",
                unavailable.Message,
                "https://tools.ietf.org/html/rfc9110#section-15.6.4"),

            UnauthorizedException unauthorized => (
                StatusCodes.Status401Unauthorized,
                "Не авторизовано",
                unauthorized.Message,
                "https://tools.ietf.org/html/rfc9110#section-15.5.2"),

            ForbiddenException forbidden => (
                StatusCodes.Status403Forbidden,
                "Доступ заборонено",
                forbidden.Message,
                "https://tools.ietf.org/html/rfc9110#section-15.5.4"),

            TimeoutException or TaskCanceledException => (
                StatusCodes.Status504GatewayTimeout,
                "Таймаут шлюзу",
                "Час очікування відповіді від залежного сервісу вичерпано.",
                "https://tools.ietf.org/html/rfc9110#section-15.6.5"),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Внутрішня помилка сервера",
                "Сталася непередбачена помилка на API Gateway.",
                "https://tools.ietf.org/html/rfc9110#section-15.6.1")
        };

        if (statusCode >= 500)
        {
            _logger.LogError(exception, "Gateway error on {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        if (exception is ServiceUnavailableException unavailableException)
        {
            context.Response.Headers.RetryAfter = unavailableException.RetryAfterSeconds.ToString();
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Type = type,
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problem);
    }
}
