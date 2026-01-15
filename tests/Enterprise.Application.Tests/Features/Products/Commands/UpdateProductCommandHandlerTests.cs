using AutoMapper;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Products.Commands.UpdateProduct;
using Enterprise.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Enterprise.Application.Tests.Features.Products.Commands;

public class UpdateProductCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly UpdateProductCommandHandler _handler;

    public UpdateProductCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _mapperMock = new Mock<IMapper>();
        _cacheServiceMock = new Mock<ICacheService>();

        _handler = new UpdateProductCommandHandler(
            _productRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _mapperMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ShouldUpdateProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new UpdateProductCommand(
            productId,
            "Updated Product",
            "Updated Description",
            149.99m,
            20,
            "Updated Category");

        var existingProduct = new Product
        {
            Id = productId,
            Name = "Original Product",
            Description = "Original Description",
            Price = 99.99m,
            Stock = 10,
            Category = "Original Category",
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var expectedDto = new ProductDto
        {
            Id = productId,
            Name = command.Name,
            Description = command.Description,
            Price = command.Price,
            Stock = command.Stock,
            Category = command.Category
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _productRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock
            .Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns(expectedDto);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Name.Should().Be(command.Name);
        result.Description.Should().Be(command.Description);
        result.Price.Should().Be(command.Price);
        result.Stock.Should().Be(command.Stock);
        result.Category.Should().Be(command.Category);

        existingProduct.Name.Should().Be(command.Name);
        existingProduct.Description.Should().Be(command.Description);
        existingProduct.Price.Should().Be(command.Price);
        existingProduct.Stock.Should().Be(command.Stock);
        existingProduct.Category.Should().Be(command.Category);
        existingProduct.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

        _productRepositoryMock.Verify(
            r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()),
            Times.Once);

        _productRepositoryMock.Verify(
            r => r.UpdateAsync(It.Is<Product>(p => p.Id == productId), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _mapperMock.Verify(
            m => m.Map<ProductDto>(It.IsAny<Product>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentProductId_ShouldThrowNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new UpdateProductCommand(
            productId,
            "Updated Product",
            "Updated Description",
            149.99m,
            20,
            "Updated Category");

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
            r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()),
            Times.Never);

        _unitOfWorkMock.Verify(
            u => u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_PartialUpdate_ShouldUpdateOnlyProvidedFields()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var command = new UpdateProductCommand(
            productId,
            "Updated Name Only",
            "Original Description",
            99.99m,
            10,
            "Original Category");

        var existingProduct = new Product
        {
            Id = productId,
            Name = "Original Name",
            Description = "Original Description",
            Price = 99.99m,
            Stock = 10,
            Category = "Original Category"
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _productRepositoryMock
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _mapperMock
            .Setup(m => m.Map<ProductDto>(It.IsAny<Product>()))
            .Returns(new ProductDto { Id = productId, Name = command.Name });

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        existingProduct.Name.Should().Be("Updated Name Only");
        existingProduct.Description.Should().Be("Original Description");
    }
}
