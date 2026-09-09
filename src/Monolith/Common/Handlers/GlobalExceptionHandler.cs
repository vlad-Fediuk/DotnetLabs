using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LR0.Common.Exceptions;

namespace LR0.Common.Handlers;

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
        _logger.LogError(exception, "Произошла необработанная ошибка: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Ресурс не найден"),
            ConflictException => (StatusCodes.Status409Conflict, "Конфликт данных"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Некорректный запрос"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Ошибка авторизации"),
            FluentValidation.ValidationException => (StatusCodes.Status400BadRequest, "Ошибка валидации"),
            _ => (StatusCodes.Status500InternalServerError, "Внутренняя ошибка сервера")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
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

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
