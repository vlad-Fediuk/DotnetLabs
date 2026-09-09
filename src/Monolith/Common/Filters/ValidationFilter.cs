using FluentValidation;

namespace LR0.Common.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var validator = context.HttpContext.RequestServices.GetService<IValidator<T>>();
        if (validator is not null)
        {
            var argument = context.Arguments.OfType<T>().FirstOrDefault();
            if (argument is not null)
            {
                var validationResult = await validator.ValidateAsync(argument, context.HttpContext.RequestAborted);
                if (!validationResult.IsValid)
                {
                    return Results.ValidationProblem(validationResult.ToDictionary());
                }
            }
        }

        return await next(context);
    }
}
