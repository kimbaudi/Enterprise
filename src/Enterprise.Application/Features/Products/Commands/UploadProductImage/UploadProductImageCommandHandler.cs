using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Entities;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Products.Commands.UploadProductImage;

public class UploadProductImageCommandHandler : IRequestHandler<UploadProductImageCommand, UploadProductImageResponse>
{
    private readonly IRepository<Product> _productRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UploadProductImageCommandHandler> _logger;

    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public UploadProductImageCommandHandler(
        IRepository<Product> productRepository,
        IFileStorageService fileStorageService,
        IUnitOfWork unitOfWork,
        ILogger<UploadProductImageCommandHandler> logger)
    {
        _productRepository = productRepository;
        _fileStorageService = fileStorageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UploadProductImageResponse> Handle(UploadProductImageCommand request, CancellationToken cancellationToken)
    {
        // Validate file
        ValidateFile(request.FileName, request.FileSize);

        // Get product
        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        // Delete old image if exists
        if (!string.IsNullOrEmpty(product.ImagePath))
        {
            try
            {
                await _fileStorageService.DeleteFileAsync(product.ImagePath, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete old image: {ImagePath}", product.ImagePath);
            }
        }

        // Upload new image
        var imagePath = await _fileStorageService.UploadFileAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            cancellationToken);

        var imageUrl = _fileStorageService.GetFileUrl(imagePath);

        // Update product
        product.ImagePath = imagePath;
        product.ImageUrl = imageUrl;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Product image uploaded successfully for product: {ProductId}", request.ProductId);

        return new UploadProductImageResponse(imageUrl, imagePath);
    }

    private static void ValidateFile(string fileName, long fileSize)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!AllowedExtensions.Contains(extension))
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "File", new[] { $"File type not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}" } }
            });
        }

        if (fileSize > MaxFileSize)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "File", new[] { $"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024} MB" } }
            });
        }

        if (fileSize == 0)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "File", new[] { "File is empty" } }
            });
        }
    }
}
