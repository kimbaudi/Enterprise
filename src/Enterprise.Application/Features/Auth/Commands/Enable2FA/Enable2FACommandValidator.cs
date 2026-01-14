using FluentValidation;

namespace Enterprise.Application.Features.Auth.Commands.Enable2FA;

public class Enable2FACommandValidator : AbstractValidator<Enable2FACommand>
{
    public Enable2FACommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
}
