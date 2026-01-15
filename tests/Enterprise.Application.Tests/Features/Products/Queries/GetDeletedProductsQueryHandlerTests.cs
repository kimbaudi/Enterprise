using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Products.Queries.GetDeletedProducts;
using Enterprise.Domain.Entities;
using FluentAssertions;
using Moq;
using System.Linq.Expressions;

namespace Enterprise.Application.Tests.Features.Products.Queries;

public class GetDeletedProductsQueryHandlerTests
{
    private readonly Mock<IRepository<Product>> _productRepositoryMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly GetDeletedProductsQueryHandler _handler;

    public GetDeletedProductsQueryHandlerTests()
    {
        _productRepositoryMock = new Mock<IRepository<Product>>();
        _mapperMock = new Mock<IMapper>();

        _handler = new GetDeletedProductsQueryHandler(
            _productRepositoryMock.Object,
            _mapperMock.Object);
    }

    [Fact]
    public async Task Handle_DeletedProductsExist_ShouldReturnPaginatedDeletedProducts()
    {
        // Arrange
        var pageNumber = 1;
        var pageSize = 10;
        var query = new GetDeletedProductsQuery(pageNumber, pageSize);

        var deletedProducts = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Deleted Product 1", IsDeleted = true, DeletedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Name = "Deleted Product 2", IsDeleted = true, DeletedAt = DateTime.UtcNow.AddDays(-2) }
        };

        var productDtos = deletedProducts.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name
        }).ToList();

        _productRepositoryMock
            .Setup(r => r.GetDeletedPagedAsync(
                pageNumber,
                pageSize,
                It.IsAny<Expression<Func<Product, object>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((deletedProducts, deletedProducts.Count));

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);

        _productRepositoryMock.Verify(
            r => r.GetDeletedPagedAsync(
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
    public async Task Handle_NoDeletedProducts_ShouldReturnEmptyPaginatedResult()
    {
        // Arrange
        var query = new GetDeletedProductsQuery(1, 10);

        _productRepositoryMock
            .Setup(r => r.GetDeletedPagedAsync(
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
        result.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_PaginatedRequest_ShouldRespectPagination()
    {
        // Arrange
        var pageNumber = 2;
        var pageSize = 5;
        var query = new GetDeletedProductsQuery(pageNumber, pageSize);

        var deletedProducts = Enumerable.Range(6, 5)
            .Select(i => new Product
            {
                Id = Guid.NewGuid(),
                Name = $"Deleted Product {i}",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow.AddDays(-i)
            })
            .ToList();

        var productDtos = deletedProducts.Select(p => new ProductDto
        {
            Id = p.Id,
            Name = p.Name
        }).ToList();

        _productRepositoryMock
            .Setup(r => r.GetDeletedPagedAsync(
                pageNumber,
                pageSize,
                It.IsAny<Expression<Func<Product, object>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((deletedProducts, 15)); // Total count = 15

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(productDtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.TotalCount.Should().Be(15);
        result.PageNumber.Should().Be(pageNumber);
        result.PageSize.Should().Be(pageSize);
        result.TotalPages.Should().Be(3);
        result.HasPreviousPage.Should().BeTrue();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_OrdersByDeletionDate_ShouldVerifyCorrectOrdering()
    {
        // Arrange
        var query = new GetDeletedProductsQuery(1, 10);

        var deletedProducts = new List<Product>
        {
            new() { Id = Guid.NewGuid(), Name = "Product A", DeletedAt = DateTime.UtcNow.AddDays(-1) },
            new() { Id = Guid.NewGuid(), Name = "Product B", DeletedAt = DateTime.UtcNow.AddDays(-3) },
            new() { Id = Guid.NewGuid(), Name = "Product C", DeletedAt = DateTime.UtcNow.AddDays(-2) }
        };

        _productRepositoryMock
            .Setup(r => r.GetDeletedPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Product, object>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((deletedProducts, deletedProducts.Count));

        _mapperMock
            .Setup(m => m.Map<List<ProductDto>>(It.IsAny<List<Product>>()))
            .Returns(deletedProducts.Select(p => new ProductDto { Id = p.Id, Name = p.Name }).ToList());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        _productRepositoryMock.Verify(
            r => r.GetDeletedPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<Expression<Func<Product, object>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "Should call GetDeletedPagedAsync with ordering expression");
    }
}
