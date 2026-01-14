using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Products.Commands.RestoreProduct;
using Enterprise.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Enterprise.Application.Tests.Features.Products.Commands;

public class RestoreProductCommandHandlerTests
{
    private readonly Mock<IRepository<Product>> _repositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly RestoreProductCommandHandler _handler;

    public RestoreProductCommandHandlerTests()
    {
        _repositoryMock = new Mock<IRepository<Product>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _handler = new RestoreProductCommandHandler(
            _repositoryMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task Handle_ValidDeletedProduct_ShouldRestoreProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var deletedProduct = new Product
        {
            Id = productId,
            Name = "Deleted Product",
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-1),
            DeletedBy = "admin"
        };

        var command = new RestoreProductCommand(productId);

        _repositoryMock.Setup(x => x.GetDeletedByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(deletedProduct);

        _repositoryMock.Setup(x => x.RestoreAsync(productId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(x => x.RestoreAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new RestoreProductCommand(productId);

        _repositoryMock.Setup(x => x.GetDeletedByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage("*Deleted Product*not found*");
    }
}
