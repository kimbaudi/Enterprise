using AutoMapper;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductsPaginated;

public class GetProductsPaginatedQueryHandler : IRequestHandler<GetProductsPaginatedQuery, PaginatedResult<ProductDto>>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IMapper _mapper;

    public GetProductsPaginatedQueryHandler(
        IRepository<Product> productRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ProductDto>> Handle(GetProductsPaginatedQuery request, CancellationToken cancellationToken)
    {
        var totalCount = await _productRepository.CountAsync(null, cancellationToken);
        
        var allProducts = await _productRepository.GetAllAsync(cancellationToken);
        
        var paginatedProducts = allProducts
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var productDtos = _mapper.Map<List<ProductDto>>(paginatedProducts);

        return PaginatedResult<ProductDto>.Create(productDtos, totalCount, request.PageNumber, request.PageSize);
    }
}
