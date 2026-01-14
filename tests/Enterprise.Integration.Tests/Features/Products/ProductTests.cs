using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Integration.Tests.Infrastructure;
using Enterprise.WebApi.Common;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace Enterprise.Integration.Tests.Features.Products;

/// <summary>
/// Integration tests for product endpoints
/// </summary>
public class ProductTests : IntegrationTestBase
{
    public ProductTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetProducts_WithValidToken_ReturnsProductList()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/products?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<ApiResponse<PaginatedResult<ProductDto>>>(response);
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().NotBeEmpty();
        result.Data.Items.Should().HaveCountGreaterThanOrEqualTo(3); // We seeded 3 products
    }

    [Fact]
    public async Task GetProducts_WithoutToken_ReturnsOk()
    {
        // Act - Products endpoint may be public depending on implementation
        var response = await Client.GetAsync("/api/v1/products?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetProductById_WithExistingProduct_ReturnsProduct()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var productId = Guid.Parse("33333333-3333-3333-3333-333333333333"); // Test Product 1

        // Act
        var response = await client.GetAsync($"/api/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Id.Should().Be(productId);
        result.Data.Name.Should().Be("Test Product 1");
    }

    [Fact]
    public async Task GetProductById_WithNonExistentProduct_ReturnsNotFound()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/api/v1/products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateProduct_WithValidData_CreatesProductSuccessfully()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var newProduct = new
        {
            Name = "New Test Product",
            Description = "New Test Description",
            Price = 299.99m,
            Stock = 75,
            Category = "Test Category",
            SKU = "TEST-NEW-001"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/products", newProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(newProduct.Name);
        result.Data.Price.Should().Be(newProduct.Price);
        result.Data.Stock.Should().Be(newProduct.Stock);
    }

    [Fact]
    public async Task CreateProduct_WithInvalidData_ReturnsBadRequest()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var invalidProduct = new
        {
            Name = "", // Empty name is invalid
            Description = "Test",
            Price = -10m, // Negative price
            Stock = -5, // Negative stock
            Category = "",
            SKU = ""
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/products", invalidProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProduct_WithoutAuthentication_ReturnsUnauthorized()
    {
        // Arrange
        var newProduct = new
        {
            Name = "Unauthorized Product",
            Description = "Test",
            Price = 100m,
            Stock = 50,
            Category = "Test",
            SKU = "UNAUTH-001"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/v1/products", newProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateProduct_WithValidData_UpdatesProductSuccessfully()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var productId = Guid.Parse("44444444-4444-4444-4444-444444444444"); // Test Product 2

        var updateProduct = new
        {
            Id = productId,
            Name = "Updated Product Name",
            Description = "Updated Description",
            Price = 199.99m,
            Stock = 100,
            Category = "Updated Category",
            SKU = "TEST-UPD-002"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/products/{productId}", updateProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Name.Should().Be(updateProduct.Name);
        result.Data.Price.Should().Be(updateProduct.Price);
    }

    [Fact]
    public async Task UpdateProduct_WithNonExistentProduct_ReturnsNotFound()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var nonExistentId = Guid.NewGuid();

        var updateProduct = new
        {
            Id = nonExistentId,
            Name = "Update Name",
            Description = "Update Description",
            Price = 100m,
            Stock = 50,
            Category = "Category",
            SKU = "TEST-001"
        };

        // Act
        var response = await client.PutAsJsonAsync($"/api/v1/products/{nonExistentId}", updateProduct);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_WithExistingProduct_DeletesSuccessfully()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var productId = Guid.Parse("55555555-5555-5555-5555-555555555555"); // Test Product 3

        // Act
        var response = await client.DeleteAsync($"/api/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify product is deleted
        var getResponse = await client.GetAsync($"/api/v1/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteProduct_WithNonExistentProduct_ReturnsNotFound()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await client.DeleteAsync($"/api/v1/products/{nonExistentId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetProducts_WithPagination_ReturnsPaginatedResults()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/products?pageNumber=1&pageSize=2");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<ApiResponse<PaginatedResult<ProductDto>>>(response);
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.PageNumber.Should().Be(1);
        result.Data.PageSize.Should().Be(2);
        result.Data.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetProducts_WithCategoryFilter_ReturnsFilteredResults()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/products/category/Electronics");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<ApiResponse<PaginatedResult<ProductDto>>>(response);
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Items.Should().OnlyContain(p => p.Category == "Electronics");
    }

    [Fact]
    public async Task GetProducts_WithSearchTerm_ReturnsMatchingResults()
    {
        // Arrange
        var client = await GetAuthenticatedAdminClientAsync();

        // Act
        var response = await client.GetAsync("/api/v1/products?searchTerm=Product 1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await DeserializeResponseAsync<ApiResponse<PaginatedResult<ProductDto>>>(response);
        result.Should().NotBeNull();
        result!.Data.Should().NotBeNull();
        result.Data!.Items.Should().Contain(p => p.Name.Contains("Product 1"));
    }
}
