using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;

    public DeleteProductCommandHandler(
        IRepository<Product> productRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.Id);
        }

        await _productRepository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate product list cache
        await _cacheService.RemoveByPatternAsync("products:paginated:*", cancellationToken);

        return true;
    }
}
