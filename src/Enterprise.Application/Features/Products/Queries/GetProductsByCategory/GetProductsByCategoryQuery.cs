using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductsByCategory;

public record GetProductsByCategoryQuery(
    string Category,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedResult<ProductDto>>;
