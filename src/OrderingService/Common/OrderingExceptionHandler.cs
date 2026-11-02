using ECommerce.Shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OrderingService.Common;

public class OrderingExceptionHandler : IExceptionHandler
{
    private readonly ILogger<OrderingExceptionHandler> _logger;

    public OrderingExceptionHandler(ILogger<OrderingExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Ресурс не найден"),
            ConflictException => (StatusCodes.Status409Conflict, "Конфликт данных"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Некорректный запрос"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Ошибка авторизации"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Доступ запрещен"),
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "Ошибка валидации"),
            ServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Сервис временно недоступен"),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError && exception is not ServiceUnavailableException)
        {
            _logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            _logger.LogWarning("Request {Method} {Path} rejected with {StatusCode}: {Message}", httpContext.Request.Method, httpContext.Request.Path, statusCode, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "Произошла внутренняя ошибка сервера." : exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception is FluentValidation.ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        }

        if (exception is ServiceUnavailableException unavailableException)
        {
            httpContext.Response.Headers.RetryAfter = unavailableException.RetryAfterSeconds.ToString();
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
