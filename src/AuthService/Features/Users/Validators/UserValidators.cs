using FluentValidation;
using AuthService.Features.Users.DTOs;

namespace AuthService.Features.Users.Validators;

public class UpdateRolesRequestValidator : AbstractValidator<UpdateRolesRequest>
{
    public UpdateRolesRequestValidator()
    {
        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("At least one role must be specified.")
            .Must(roles => roles.All(r => !string.IsNullOrWhiteSpace(r)))
            .WithMessage("Role names must not be empty.");
    }
}
