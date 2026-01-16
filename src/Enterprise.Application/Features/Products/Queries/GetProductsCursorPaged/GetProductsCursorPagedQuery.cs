using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductsCursorPaged;

/// <summary>
/// Query to get products using cursor-based pagination for optimal performance with large datasets
/// </summary>
public record GetProductsCursorPagedQuery(
    string? Cursor = null,
    int PageSize = 20,
    string? SearchTerm = null
) : IRequest<CursorPaginatedResult<ProductDto>>;

public class GetProductsCursorPagedQueryHandler : IRequestHandler<GetProductsCursorPagedQuery, CursorPaginatedResult<ProductDto>>
{
    private readonly IRepository<Product> _productRepository;

    public GetProductsCursorPagedQueryHandler(IRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<CursorPaginatedResult<ProductDto>> Handle(
        GetProductsCursorPagedQuery request,
        CancellationToken cancellationToken)
    {
        // Build predicate for search
        System.Linq.Expressions.Expression<Func<Product, bool>>? predicate = null;
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            predicate = p => p.Name.ToLower().Contains(searchLower) ||
                           (p.Description != null && p.Description.ToLower().Contains(searchLower));
        }

        // Get cursor-based paginated results
        var result = await _productRepository.GetCursorPagedAsync(
            request.Cursor,
            request.PageSize,
            predicate,
            orderBy: p => p.CreatedAt,
            ascending: false, // Most recent first
            cancellationToken);

        // Map to DTOs
        var dtos = result.Items.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            SKU = p.SKU,
            Price = p.Price,
            Stock = p.Stock,
            Category = p.Category,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        });

        return new CursorPaginatedResult<ProductDto>(
            dtos,
            result.NextCursor,
            result.PreviousCursor,
            result.PageSize);
    }
}
