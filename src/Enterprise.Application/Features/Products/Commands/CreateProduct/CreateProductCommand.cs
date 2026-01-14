using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Products.Queries;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    string SKU
) : IRequest<ProductDto>;
