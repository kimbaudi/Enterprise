using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.SearchProducts;

public record SearchProductsQuery(
    string? SearchTerm = null,
    string? Category = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MinStockLevel = null,
    int? MaxStockLevel = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedResult<ProductDto>>;
