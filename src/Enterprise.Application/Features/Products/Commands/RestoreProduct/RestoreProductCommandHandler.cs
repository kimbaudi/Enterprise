using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Products.Commands.RestoreProduct;

public class RestoreProductCommandHandler : IRequestHandler<RestoreProductCommand, bool>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RestoreProductCommandHandler(
        IRepository<Product> productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RestoreProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetDeletedByIdAsync(request.Id, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Deleted Product", request.Id);
        }

        await _productRepository.RestoreAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
