using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Products.Queries;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category
) : IRequest<ProductDto>;
