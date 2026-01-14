using AutoMapper;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Products.Queries.SearchProducts;

public class SearchProductsQueryHandler : IRequestHandler<SearchProductsQuery, PaginatedResult<ProductDto>>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<SearchProductsQueryHandler> _logger;

    public SearchProductsQueryHandler(
        IRepository<Product> productRepository,
        IMapper mapper,
        ILogger<SearchProductsQueryHandler> logger)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResult<ProductDto>> Handle(SearchProductsQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Searching products with filters: SearchTerm={SearchTerm}, Category={Category}, " +
                             "PriceRange={MinPrice}-{MaxPrice}, StockRange={MinStock}-{MaxStock}",
            request.SearchTerm, request.Category, request.MinPrice, request.MaxPrice,
            request.MinStockLevel, request.MaxStockLevel);

        // Start with all products
        var query = _productRepository.GetQueryable();

        // Apply search term filter (search in Name, Description, SKU)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(searchTerm) ||
                p.Description.ToLower().Contains(searchTerm) ||
                p.SKU.ToLower().Contains(searchTerm));
        }

        // Apply category filter
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(p => p.Category == request.Category);
        }

        // Apply price range filter
        if (request.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= request.MinPrice.Value);
        }
        if (request.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= request.MaxPrice.Value);
        }

        // Apply stock level filter
        if (request.MinStockLevel.HasValue)
        {
            query = query.Where(p => p.Stock >= request.MinStockLevel.Value);
        }
        if (request.MaxStockLevel.HasValue)
        {
            query = query.Where(p => p.Stock <= request.MaxStockLevel.Value);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var products = await query
            .OrderBy(p => p.Name)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var productDtos = _mapper.Map<List<ProductDto>>(products);

        _logger.LogInformation("Found {Count} products matching search criteria (Page {PageNumber}/{TotalPages})",
            totalCount, request.PageNumber, (int)Math.Ceiling(totalCount / (double)request.PageSize));

        return new PaginatedResult<ProductDto>(
            productDtos,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }
}
