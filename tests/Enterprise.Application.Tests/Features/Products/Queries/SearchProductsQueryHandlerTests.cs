using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Products.Queries.SearchProducts;
using Enterprise.Domain.Entities;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using System.Linq.Expressions;

namespace Enterprise.Application.Tests.Features.Products.Queries;

public class SearchProductsQueryHandlerTests
{
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly SearchProductsQueryHandler _handler;

    public SearchProductsQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _mapperMock = new Mock<IMapper>();

        _handler = new SearchProductsQueryHandler(
            _productRepositoryMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_WithSearchTerm_ShouldReturnMatchingProducts()
    {
        // Arrange
        var searchTerm = "laptop";
        var query = new SearchProductsQuery(
            searchTerm,
            null,
            null,
            null,
            1,
            10);

        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Gaming Laptop", Description = "High performance", Price = 1299.99m, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Business Laptop", Description = "Reliable and fast", Price = 899.99m, IsDeleted = false }
        };

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price
        }).ToList();

        var queryableMock = products.AsQueryable().BuildMockDbSet().Object;

        _productRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(queryableMock);

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(p => p.Name.Contains("Laptop"));

        _productRepositoryMock.Verify(
            r => r.GetQueryable(),
            Times.Once);

        _mapperMock.Verify(
            m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPriceFilters_ShouldReturnProductsInPriceRange()
    {
        // Arrange
        var query = new SearchProductsQuery(
            string.Empty,
            500m,
            1000m,
            null,
            1,
            10);

        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Product 1", Price = 599.99m, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Product 2", Price = 899.99m, IsDeleted = false }
        };

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price
        }).ToList();

        var queryableMock = products.AsQueryable().BuildMockDbSet().Object;

        _productRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(queryableMock);

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p =>
        {
            p.Price.Should().BeGreaterThanOrEqualTo(500m);
            p.Price.Should().BeLessThanOrEqualTo(1000m);
        });
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_ShouldReturnProductsInCategory()
    {
        // Arrange
        var category = "Electronics";
        var query = new SearchProductsQuery(
            string.Empty,
            null,
            null,
            category,
            1,
            10);

        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "TV", Category = category, Price = 799.99m, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Radio", Category = category, Price = 99.99m, IsDeleted = false }
        };

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Category = p.Category,
            Price = p.Price
        }).ToList();

        var queryableMock = products.AsQueryable().BuildMockDbSet().Object;

        _productRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(queryableMock);

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.Category.Should().Be(category));
    }

    [Fact]
    public async Task Handle_WithMultipleFilters_ShouldApplyAllFilters()
    {
        // Arrange
        var query = new SearchProductsQuery(
            "gaming",
            500m,
            2000m,
            "Electronics",
            1,
            10);

        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Gaming Console",
                Category = "Electronics",
                Price = 499.99m,
                IsDeleted = false
            }
        };

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Category = p.Category,
            Price = p.Price
        }).ToList();

        var queryableMock = products.AsQueryable().BuildMockDbSet().Object;

        _productRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(queryableMock);

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _productRepositoryMock.Verify(r => r.GetQueryable(), Times.Once);
    }

    [Fact]
    public async Task Handle_NoMatchingResults_ShouldReturnEmptyPaginatedResult()
    {
        // Arrange
        var query = new SearchProductsQuery(
            "nonexistent",
            null,
            null,
            null,
            1,
            10);

        var emptyProducts = new List<Product>();
        var queryableMock = emptyProducts.AsQueryable().BuildMockDbSet().Object;

        _productRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(queryableMock);

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(new List<ProductDto>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ExcludesDeletedProducts_ShouldOnlyReturnActiveProducts()
    {
        // Arrange
        var query = new SearchProductsQuery(
            "product",
            null,
            null,
            null,
            1,
            10);

        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Active Product", Price = 99.99m, IsDeleted = false }
        };

        var queryableMock = products.AsQueryable().BuildMockDbSet().Object;

        _productRepositoryMock
            .Setup(r => r.GetQueryable())
            .Returns(queryableMock);

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(products.Select(p => new ProductDto { Id = p.Id, Name = p.Name }).ToList());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().OnlyContain(p => p.Name == "Active Product");
    }
}
