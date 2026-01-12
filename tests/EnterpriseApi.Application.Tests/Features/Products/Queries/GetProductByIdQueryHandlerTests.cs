using AutoMapper;
using EnterpriseApi.Application.Common.Exceptions;
using EnterpriseApi.Application.DTOs;
using EnterpriseApi.Application.Features.Products.Queries.GetProductById;
using EnterpriseApi.Domain.Entities;
using EnterpriseApi.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace EnterpriseApi.Application.Tests.Features.Products.Queries;

public class GetProductByIdQueryHandlerTests
{
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetProductByIdQueryHandler _handler;

    public GetProductByIdQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _mapperMock = new Mock<IMapper>();
        
        _handler = new GetProductByIdQueryHandler(
            _productRepositoryMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingProduct_ShouldReturnProductDto()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Description = "Test Description",
            Price = 99.99m,
            Category = "Electronics",
            Stock = 10,
            SKU = "TEST-001",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var expectedDto = new ProductDto
        {
            Id = productId,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            Category = product.Category,
            Stock = product.Stock,
            SKU = product.SKU
        };

        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mapperMock
            .Setup(m => m.Map<ProductDto>(product))
            .Returns(expectedDto);

        var query = new GetProductByIdQuery(productId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(productId);
        result.Name.Should().Be(product.Name);
        result.Description.Should().Be(product.Description);
        result.Price.Should().Be(product.Price);
        result.Category.Should().Be(product.Category);
        result.Stock.Should().Be(product.Stock);
        result.SKU.Should().Be(product.SKU);
        
        _productRepositoryMock.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistingProduct_ShouldThrowNotFoundException()
    {
        // Arrange
        var productId = Guid.NewGuid();
        
        _productRepositoryMock
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var query = new GetProductByIdQuery(productId);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"Entity \"Product\" ({productId}) was not found.");
        
        _productRepositoryMock.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
