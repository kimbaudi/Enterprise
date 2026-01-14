using FluentValidation;

namespace Enterprise.Application.Features.Products.Commands.RestoreProduct;

public class RestoreProductCommandValidator : AbstractValidator<RestoreProductCommand>
{
    public RestoreProductCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Product ID is required");
    }
}
