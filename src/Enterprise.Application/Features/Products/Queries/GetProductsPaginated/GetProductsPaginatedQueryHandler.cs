using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductsPaginated;

public class GetProductsPaginatedQueryHandler : IRequestHandler<GetProductsPaginatedQuery, PaginatedResult<ProductDto>>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public GetProductsPaginatedQueryHandler(
        IRepository<Product> productRepository,
        IMapper mapper,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<PaginatedResult<ProductDto>> Handle(GetProductsPaginatedQuery request, CancellationToken cancellationToken)
    {
        // Generate cache key based on query parameters
        var cacheKey = $"products:paginated:{request.PageNumber}:{request.PageSize}:{request.SearchTerm}:{request.SortBy}";

        // Try to get from cache first
        var cachedResult = await _cacheService.GetAsync<PaginatedResult<ProductDto>>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            return cachedResult;
        }

        // If not in cache, query database
        var totalCount = await _productRepository.CountAsync(null, cancellationToken);

        var allProducts = await _productRepository.GetAllAsync(cancellationToken);

        var paginatedProducts = allProducts
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var productDtos = _mapper.Map<List<ProductDto>>(paginatedProducts);

        var result = PaginatedResult<ProductDto>.Create(productDtos, totalCount, request.PageNumber, request.PageSize);

        // Cache the result for 5 minutes
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return result;
    }
}
