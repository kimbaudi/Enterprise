using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Entities;
using MediatR;
using System.Runtime.CompilerServices;

namespace Enterprise.Application.Features.Products.Queries.GetProductsStreaming;

public class GetProductsStreamingQueryHandler : IStreamRequestHandler<GetProductsStreamingQuery, ProductDto>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IMapper _mapper;

    public GetProductsStreamingQueryHandler(
        IRepository<Product> productRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async IAsyncEnumerable<ProductDto> Handle(
        GetProductsStreamingQuery request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        // Get all products efficiently without loading everything into memory
        var allProducts = await _productRepository.GetAllAsync(cancellationToken);

        // Apply filtering if search term is provided
        var filteredProducts = allProducts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            filteredProducts = filteredProducts.Where(p =>
                p.Name.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                (p.Description != null && p.Description.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase)));
        }

        // Apply sorting
        filteredProducts = request.SortBy?.ToLower() switch
        {
            "name" => filteredProducts.OrderBy(p => p.Name),
            "price" => filteredProducts.OrderBy(p => p.Price),
            "createdat" => filteredProducts.OrderByDescending(p => p.CreatedAt),
            _ => filteredProducts.OrderBy(p => p.Name)
        };

        // Apply pagination
        var paginatedProducts = filteredProducts
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize);

        // Stream results one by one
        foreach (var product in paginatedProducts)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return _mapper.Map<ProductDto>(product);

            // Small delay to demonstrate streaming (remove in production)
            await Task.Delay(10, cancellationToken);
        }
    }
}
