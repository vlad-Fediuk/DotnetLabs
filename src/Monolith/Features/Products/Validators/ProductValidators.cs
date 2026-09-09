using FluentValidation;
using LR0.Features.Products.DTOs;

namespace LR0.Features.Products.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название товара обязательно")
            .MaximumLength(255).WithMessage("Название товара не должно превышать 255 символов");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Артикул (SKU) обязателен")
            .MaximumLength(100).WithMessage("Артикул не должен превышать 100 символов");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Цена должна быть больше нуля");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Количество на складе не может быть отрицательным");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Необходимо указать идентификатор категории");
    }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название товара обязательно")
            .MaximumLength(255).WithMessage("Название товара не должно превышать 255 символов");

        RuleFor(x => x.Sku)
            .NotEmpty().WithMessage("Артикул (SKU) обязателен")
            .MaximumLength(100).WithMessage("Артикул не должен превышать 100 символов");

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Цена должна быть больше нуля");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Количество на складе не может быть отрицательным");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Необходимо указать идентификатор категории");
    }
}
