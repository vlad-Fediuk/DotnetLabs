using FluentValidation;
using OrderingService.Features.Orders.DTOs;

namespace OrderingService.Features.Orders.Validators;

public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("Замовлення має містити хоча б один товар")
            .Must(items => items == null || items.Select(i => i.ProductId).Distinct().Count() == items.Count)
            .WithMessage("Замовлення не повинно містити дублікати позицій одного й того ж товару.");

        RuleForEach(x => x.Items).SetValidator(new OrderItemRequestValidator());
    }
}

public class OrderItemRequestValidator : AbstractValidator<OrderItemRequest>
{
    public OrderItemRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Ідентифікатор товару є обов'язковим");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Кількість товару має бути більше 0");
    }
}

public class UpdateOrderStatusRequestValidator : AbstractValidator<UpdateOrderStatusRequest>
{
    public UpdateOrderStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .IsInEnum().WithMessage("Вказано некоректний статус замовлення");
    }
}
