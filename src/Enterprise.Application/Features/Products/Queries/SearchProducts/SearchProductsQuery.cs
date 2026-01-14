using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Products.Queries.GetProductsPaginated;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.SearchProducts;

public record SearchProductsQuery(
    string SearchTerm,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Category = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedResult<ProductDto>>;
