using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Products.Queries.GetProductsByCategory;
using Enterprise.Domain.Entities;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;

namespace Enterprise.Application.Tests.Features.Products.Queries;

public class GetProductsByCategoryQueryHandlerTests
{
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetProductsByCategoryQueryHandler _handler;

    public GetProductsByCategoryQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _mapperMock = new Mock<IMapper>();

        _handler = new GetProductsByCategoryQueryHandler(
            _productRepositoryMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCategory_ShouldReturnFilteredProducts()
    {
        // Arrange
        var category = "Electronics";
        var pageNumber = 1;
        var pageSize = 10;
        var query = new GetProductsByCategoryQuery(category, pageNumber, pageSize);

        var products = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Laptop", Category = category, Price = 999.99m, IsDeleted = false },
            new() { Id = Guid.NewGuid(), Name = "Phone", Category = category, Price = 599.99m, IsDeleted = false }
        };

        var productDtos = products.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name,
            Category = p.Category,
            Price = p.Price
        }).ToList();

        _productRepositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                pageNumber,
                pageSize,
                It.IsAny<Expression<Func<Product, object>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((products, products.Count));

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.Category.Should().Be(category));
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);

        _productRepositoryMock.Verify(
            r => r.GetPagedAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                pageNumber,
                pageSize,
                It.IsAny<Expression<Func<Product, object>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _mapperMock.Verify(
            m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NonExistentCategory_ShouldReturnEmptyResult()
    {
        // Arrange
        var category = "NonExistent";
        var query = new GetProductsByCategoryQuery(category, 1, 10);

        _productRepositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Product, object>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Product>(), 0));

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
    public async Task Handle_PaginatedRequest_ShouldRespectPagination()
    {
        // Arrange
        var category = "Books";
        var pageNumber = 2;
        var pageSize = 5;
        var query = new GetProductsByCategoryQuery(category, pageNumber, pageSize);

        var products = Enumerable.Range(1, 5)
            .Select(i => new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Book {i + 5}",
                Category = category,
                Price = i * 20
            })
            .ToList();

        _productRepositoryMock
            .Setup(r => r.GetPagedAsync(
                It.IsAny<Expression<Func<Product, bool>>>(),
                pageNumber,
                pageSize,
                It.IsAny<Expression<Func<Product, object>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IEnumerable<Product>)products, 15));

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<IEnumerable<Product>>()))
            .Returns(products.Select(p => new ProductDto { Id = p.Id, Name = p.Name }).ToList());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }
}
