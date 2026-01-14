using FluentValidation;

namespace Enterprise.Application.Features.Auth.Commands.Disable2FA;

public class Disable2FACommandValidator : AbstractValidator<Disable2FACommand>
{
    public Disable2FACommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Verification code is required")
            .Length(6)
            .WithMessage("Verification code must be 6 digits")
            .Matches("^[0-9]+$")
            .WithMessage("Verification code must contain only digits");
    }
}
