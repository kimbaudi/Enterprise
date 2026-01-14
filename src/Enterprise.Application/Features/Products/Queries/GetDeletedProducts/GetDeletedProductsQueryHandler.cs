using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetDeletedProducts;

public class GetDeletedProductsQueryHandler : IRequestHandler<GetDeletedProductsQuery, PaginatedResult<ProductDto>>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IMapper _mapper;

    public GetDeletedProductsQueryHandler(
        IRepository<Product> productRepository,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<ProductDto>> Handle(GetDeletedProductsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _productRepository.GetDeletedPagedAsync(
            request.PageNumber,
            request.PageSize,
            p => p.DeletedAt ?? p.CreatedAt, // Order by deletion date, fallback to creation
            cancellationToken);

        var productDtos = _mapper.Map<List<ProductDto>>(items);

        return new PaginatedResult<ProductDto>(
            productDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
