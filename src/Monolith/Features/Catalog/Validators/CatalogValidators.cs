using FluentValidation;
using LR0.Features.Catalog.DTOs;

namespace LR0.Features.Catalog.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название категории обязательно")
            .MaximumLength(150).WithMessage("Название не должно превышать 150 символов");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug обязателен")
            .MaximumLength(150).WithMessage("Slug не должен превышать 150 символов")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug может содержать только строчные латинские буквы, цифры и дефис");
    }
}

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название категории обязательно")
            .MaximumLength(150).WithMessage("Название не должно превышать 150 символов");

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug обязателен")
            .MaximumLength(150).WithMessage("Slug не должен превышать 150 символов")
            .Matches("^[a-z0-9-]+$").WithMessage("Slug может содержать только строчные латинские буквы, цифры и дефис");
    }
}
