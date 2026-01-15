using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Products.Commands.UploadProductImage;
using Enterprise.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Enterprise.Application.Tests.Features.Products.Commands;

public class UploadProductImageCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IFileStorageService> _fileStorageServiceMock;
    private readonly Mock<ILogger<UploadProductImageCommandHandler>> _loggerMock;
    private readonly UploadProductImageCommandHandler _handler;

    public UploadProductImageCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _fileStorageServiceMock = new Mock<IFileStorageService>();
        _loggerMock = new Mock<ILogger<UploadProductImageCommandHandler>>();

        _handler = new UploadProductImageCommandHandler(
            _productRepositoryMock.Object,
            _fileStorageServiceMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidImageFile_ShouldUploadImageAndUpdateProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var fileName = "test-image.jpg";
        var contentType = "image/jpeg";
        var fileContent = "fake image content"u8.ToArray();
        var stream = new MemoryStream(fileContent);

        var command = new UploadProductImageCommand(
            productId,
            stream,
            fileName,
            contentType,
            fileContent.Length);

        var existingProduct = new Product
        {
            Id = productId,
            Name = "Test Product",
            ImagePath = null
        };

        var uploadedImagePath = $"products/{productId}/{fileName}";
        var uploadedImageUrl = $"/uploads/{uploadedImagePath}";

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _fileStorageServiceMock
            .Setup(f => f.UploadFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(uploadedImagePath);

        _fileStorageServiceMock
            .Setup(f => f.GetFileUrl(It.IsAny<string>()))
            .Returns(uploadedImageUrl);

        _productRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.ImageUrl.Should().Be(uploadedImageUrl);
        result.ImagePath.Should().Be(uploadedImagePath);
        existingProduct.ImagePath.Should().Be(uploadedImagePath);

        _productRepositoryMock.Verify(
            r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);

        _fileStorageServiceMock.Verify(
            f => f.UploadFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentProduct_ShouldThrowNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var stream = new MemoryStream();
        var command = new UploadProductImageCommand(
            productId,
            stream,
            "test.jpg",
            "image/jpeg",
            1000);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity \"Product\" ({productId}) was not found.");

        _fileStorageServiceMock.Verify(
            f => f.UploadFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_ReplacesExistingImage_ShouldDeleteOldImage()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var stream = new MemoryStream();
        var command = new UploadProductImageCommand(
            productId,
            stream,
            "new-image.jpg",
            "image/jpeg",
            1000);

        var oldImagePath = "products/old-image.jpg";
        var existingProduct = new Product
        {
            Id = productId,
            Name = "Test Product",
            ImagePath = oldImagePath
        };

        var newImagePath = "products/new-image.jpg";
        var newImageUrl = "/uploads/products/new-image.jpg";

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _fileStorageServiceMock
            .Setup(f => f.UploadFileAsync(
                It.IsAny<Stream>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(newImagePath);

        _fileStorageServiceMock
            .Setup(f => f.GetFileUrl(It.IsAny<string>()))
            .Returns(newImageUrl);

        _fileStorageServiceMock
            .Setup(f => f.DeleteFileAsync(oldImagePath, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ImagePath.Should().Be(newImagePath);

        _fileStorageServiceMock.Verify(
            f => f.DeleteFileAsync(oldImagePath, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
