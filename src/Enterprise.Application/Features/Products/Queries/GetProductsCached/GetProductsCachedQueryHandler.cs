using AutoMapper;
using Enterprise.Application.Common.Extensions;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Products.Queries.GetProductsCached;

public class GetProductsCachedQueryHandler : IRequestHandler<GetProductsCachedQuery, IEnumerable<ProductDto>>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IMapper _mapper;
    private readonly IDistributedCache _cache;
    private readonly ILogger<GetProductsCachedQueryHandler> _logger;
    private const string CacheKey = "all_products";

    public GetProductsCachedQueryHandler(
        IRepository<Product> productRepository,
        IMapper mapper,
        IDistributedCache cache,
        ILogger<GetProductsCachedQueryHandler> logger)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> Handle(GetProductsCachedQuery request, CancellationToken cancellationToken)
    {
        return await _cache.GetOrCreateAsync(
            CacheKey,
            async () =>
            {
                _logger.LogInformation("Cache miss - Loading products from database");
                var products = await _productRepository.GetAllAsync(cancellationToken);
                return _mapper.Map<IEnumerable<ProductDto>>(products);
            },
            TimeSpan.FromMinutes(10),
            cancellationToken);
    }
}
