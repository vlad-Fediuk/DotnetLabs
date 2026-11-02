using ECommerce.Shared.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ECommerce.Shared.Handlers;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
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
            NotFoundException => (StatusCodes.Status404NotFound, "Ресурс не знайдено"),
            ConflictException => (StatusCodes.Status409Conflict, "Конфлікт даних"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Некоректний запит"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Помилка авторизації"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Доступ заборонено"),
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "Помилка валідації"),
            ServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Сервіс тимчасово недоступний"),
            _ => (StatusCodes.Status500InternalServerError, "Внутрішня помилка сервера")
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
            Detail = statusCode == StatusCodes.Status500InternalServerError ? "Сталася внутрішня помилка сервера." : exception.Message,
            Instance = httpContext.Request.Path
        };

        if (exception is FluentValidation.ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
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
