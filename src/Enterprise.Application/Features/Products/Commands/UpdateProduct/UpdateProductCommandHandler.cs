using AutoMapper;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Entities;
using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateProductCommandHandler(
        IRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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

        return _mapper.Map<ProductDto>(existingProduct);
    }
}
