using MediatR;

namespace Enterprise.Application.Features.Products.Commands.RestoreProduct;

public record RestoreProductCommand(Guid Id) : IRequest<bool>;
