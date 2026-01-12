using EnterpriseApi.Application.DTOs;
using MediatR;

namespace EnterpriseApi.Application.Features.Products.Commands.CreateProduct;

public record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category,
    string SKU
) : IRequest<ProductDto>;
