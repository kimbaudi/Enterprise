using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Products.Queries.GetProductsPaginated;
using Enterprise.Domain.Entities;
using FluentAssertions;
using Moq;

namespace Enterprise.Application.Tests.Features.Products.Queries;

public class GetProductsPaginatedQueryHandlerTests
{
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly GetProductsPaginatedQueryHandler _handler;

    public GetProductsPaginatedQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _mapperMock = new Mock<IMapper>();
        _cacheServiceMock = new Mock<ICacheService>();

        _handler = new GetProductsPaginatedQueryHandler(
            _productRepositoryMock.Object,
            _mapperMock.Object,
            _cacheServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_ShouldReturnPaginatedProducts()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 10;
        var query = new GetProductsPaginatedQuery(pageNumber, pageSize);

        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 99.99m },
            new() { Id = Guid.NewGuid(), Name = "Product 2", Price = 149.99m },
            new() { Id = Guid.NewGuid(), Name = "Product 3", Price = 199.99m }
        };

        var productDtos = new List<ProductDto>
        {
            new() { Id = products[0].Id, Name = "Product 1", Price = 99.99m },
            new() { Id = products[1].Id, Name = "Product 2", Price = 149.99m },
            new() { Id = products[2].Id, Name = "Product 3", Price = 199.99m }
        };

        _productRepositoryMock
            .Setup(r => r.CountAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(products.Count);

        _productRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<IEnumerable<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalPages.Should().Be(1);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeFalse();

        _productRepositoryMock.Verify(
            r => r.CountAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);

        _productRepositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        _mapperMock.Verify(
            m => m.Map<List<ProductDto>>(It.IsAny<IEnumerable<Product>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SecondPage_ShouldReturnCorrectPagedResults()
    {
        // Arrange
        var pageNumber = 2;
        var pageSize = 2;
        var query = new GetProductsPaginatedQuery(pageNumber, pageSize);

        var allProducts = Enumerable.Range(1, 5)
            .Select(i => new Product { Id = Guid.NewGuid(), Name = $"Product {i}", Price = i * 10 })
            .ToList();

        var expectedDtos = allProducts
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductDto { Id = p.Id, Name = p.Name, Price = p.Price })
            .ToList();

        _productRepositoryMock
            .Setup(r => r.CountAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(allProducts.Count);

        _productRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(allProducts);

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<IEnumerable<Product>>()))
            .Returns(expectedDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.PageNumber.Should().Be(pageNumber);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmptyResult_ShouldReturnEmptyPaginatedResult()
    {
        // Arrange
        var query = new GetProductsPaginatedQuery(1, 10);

        _productRepositoryMock
            .Setup(r => r.CountAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _productRepositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<IEnumerable<Product>>()))
            .Returns(new List<ProductDto>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.TotalPages.Should().Be(0);
    }
}
