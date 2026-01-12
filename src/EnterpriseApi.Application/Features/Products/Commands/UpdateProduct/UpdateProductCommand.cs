using EnterpriseApi.Application.DTOs;
using MediatR;

namespace EnterpriseApi.Application.Features.Products.Commands.UpdateProduct;

public record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    string Category
) : IRequest<ProductDto>;
