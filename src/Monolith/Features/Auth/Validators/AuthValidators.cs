using FluentValidation;
using LR0.Features.Auth.DTOs;

namespace LR0.Features.Auth.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен для заполнения")
            .EmailAddress().WithMessage("Некорректный формат Email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен")
            .MinimumLength(6).WithMessage("Длина пароля должна быть не менее 6 символов")
            .Must(p => !string.IsNullOrWhiteSpace(p)).WithMessage("Пароль не может состоять только из пробелов");

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Имя обязательно для заполнения")
            .MaximumLength(150).WithMessage("Имя не может превышать 150 символов");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email обязателен для заполнения")
            .EmailAddress().WithMessage("Некорректный формат Email");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Пароль обязателен");
    }
}
