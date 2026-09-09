using FluentValidation;
using LR0.Features.Orders.DTOs;

namespace LR0.Features.Orders.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Заказ должен содержать хотя бы один товар")
            .Must(items => items == null || items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .WithMessage("Заказ не должен содержать дублирующиеся позиции одного и того же товара.");

        RuleForEach(x => x.Items).SetValidator(new OrderItemRequestValidator());
    }
}

public class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
{
    public OrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Идентификатор товара обязателен");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Количество товара должно быть больше 0");
    }
}

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Указан некорректный статус заказа");
    }
}
