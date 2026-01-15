using AutoMapper;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICacheService _cacheService;

    public UpdateProductCommandHandler(
        IRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cacheService = cacheService;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var existingProduct = await _productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (existingProduct == null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        existingProduct.Name = request.Name;
        existingProduct.Description = request.Description;
        existingProduct.Price = request.Price;
        existingProduct.Stock = request.Stock;
        existingProduct.Category = request.Category;
        existingProduct.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(existingProduct, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate product list cache
        await _cacheService.RemoveByPatternAsync("products:paginated:*", cancellationToken);

        return _mapper.Map<ProductDto>(existingProduct);
    }
}
