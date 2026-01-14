using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Products.Queries.GetProductsPaginated;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductsByCategory;

public record GetProductsByCategoryQuery(
    string Category,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedResult<ProductDto>>;
