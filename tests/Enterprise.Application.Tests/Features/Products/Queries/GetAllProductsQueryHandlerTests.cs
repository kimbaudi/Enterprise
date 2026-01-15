using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Products.Queries.GetAllProducts;
using Enterprise.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Enterprise.Application.Tests.Features.Products.Queries;

public class GetAllProductsQueryHandlerTests
{
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetAllProductsQueryHandler _handler;

    public GetAllProductsQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _mapperMock = new Mock<IMapper>();

        _handler = new GetAllProductsQueryHandler(
            _productRepositoryMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ProductsExist_ShouldReturnAllProducts()
    {
        // Arrange
        var query = new GetAllProductsQuery();

        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 99.99m },
            new() { Id = Guid.NewGuid(), Name = "Product 2", Price = 149.99m },
            new() { Id = Guid.NewGuid(), Name = "Product 3", Price = 199.99m }
        };

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price
        });

        _productRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mapperMock
            .Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(dto => dto.Name == "Product 1");
        result.Should().Contain(dto => dto.Name == "Product 2");
        result.Should().Contain(dto => dto.Name == "Product 3");

        _productRepositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _mapperMock.Verify(
            m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NoProducts_ShouldReturnEmptyCollection()
    {
        // Arrange
        var query = new GetAllProductsQuery();

        _productRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _mapperMock
            .Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>()))
            .Returns(new List<ProductDto>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();

        _productRepositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LargeDataset_ShouldReturnAllRecords()
    {
        // Arrange
        var query = new GetAllProductsQuery();

        var products = Enumerable.Range(1, 100)
            .Select(i => new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Product {i}",
                Price = i * 10
            })
            .ToList();

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price
        });

        _productRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mapperMock
            .Setup(m => m.Map<IEnumerable<ProductDto>>(It.IsAny<IEnumerable<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(100);
    }
}
