using FluentValidation;

namespace Enterprise.Application.Features.Auth.Commands.Validate2FA;

public class Validate2FACommandValidator : AbstractValidator<Validate2FACommand>
{
    public Validate2FACommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Verification code is required");
    }
}
