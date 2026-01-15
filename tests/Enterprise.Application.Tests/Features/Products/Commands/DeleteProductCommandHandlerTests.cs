using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Products.Commands.DeleteProduct;
using Enterprise.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Enterprise.Application.Tests.Features.Products.Commands;

public class DeleteProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly DeleteProductCommandHandler _handler;

    public DeleteProductCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _productRepositoryMock = new Mock<IRepository<Product>>();

        _handler = new DeleteProductCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidProductId_ShouldDeleteProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new DeleteProductCommand(productId);

        var existingProduct = new Product
        {
            Id = productId,
            Name = "Test Product",
            Price = 99.99m,
            Stock = 10
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _productRepositoryMock
            .Setup(r => r.DeleteAsync(productId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        _productRepositoryMock.Verify(
            r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);

        _productRepositoryMock.Verify(
            r => r.DeleteAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentProductId_ShouldThrowNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new DeleteProductCommand(productId);

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity \"Product\" ({productId}) was not found.");

        _productRepositoryMock.Verify(
            r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);

        _productRepositoryMock.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
