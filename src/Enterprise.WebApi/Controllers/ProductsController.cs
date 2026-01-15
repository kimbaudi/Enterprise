using Asp.Versioning;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Application.Features.Products.Commands.CreateProduct;
using Enterprise.Application.Features.Products.Commands.DeleteProduct;
using Enterprise.Application.Features.Products.Commands.RestoreProduct;
using Enterprise.Application.Features.Products.Commands.UpdateProduct;
using Enterprise.Application.Features.Products.Commands.UploadProductImage;
using Enterprise.Application.Features.Products.Queries.GetDeletedProducts;
using Enterprise.Application.Features.Products.Queries.GetProductById;
using Enterprise.Application.Features.Products.Queries.GetProductsByCategory;
using Enterprise.Application.Features.Products.Queries.GetProductsPaginated;
using Enterprise.Application.Features.Products.Queries.GetProductsStreaming;
using Enterprise.Application.Features.Products.Queries.SearchProducts;
using Enterprise.WebApi.Common;
using Enterprise.WebApi.FeatureFlags;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.FeatureManagement.Mvc;

namespace Enterprise.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[EnableRateLimiting("perUser")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all products with pagination
    /// </summary>
    [HttpGet]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "products-list")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsPaginatedQuery(pageNumber, pageSize, searchTerm, sortBy);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<ProductDto>>(result));
    }

    /// <summary>
    /// Stream products with pagination (memory-efficient for large datasets)
    /// </summary>
    [HttpGet("stream")]
    [FeatureGate("StreamingResponses")]
    [ProducesResponseType(typeof(IAsyncEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async IAsyncEnumerable<ProductDto> StreamProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var query = new GetProductsStreamingQuery(pageNumber, pageSize, searchTerm, sortBy);
        await foreach (var product in _mediator.CreateStream(query, cancellationToken))
        {
            yield return product;
        }
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "product-details")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<ProductDto>(result));
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{category}")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "products-category")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> GetProductsByCategory(
        string category,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetProductsByCategoryQuery(category, pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<ProductDto>>(result));
    }

    /// <summary>
    /// Search products with filters
    /// </summary>
    [HttpGet("search")]
    [Microsoft.AspNetCore.OutputCaching.OutputCache(PolicyName = "products-search")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> SearchProducts(
        [FromQuery] string searchTerm,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? category = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new SearchProductsQuery(searchTerm, minPrice, maxPrice, category, pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<ProductDto>>(result));
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id = result.Id }, new ApiResponse<ProductDto>(result));
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(
        Guid id,
        [FromBody] UpdateProductCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.Id)
        {
            return BadRequest(new ApiResponse<ProductDto>("ID mismatch"));
        }

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<ProductDto>(result));
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<bool>(result));
    }

    /// <summary>
    /// Get all deleted products with pagination (Admin only)
    /// </summary>
    [HttpGet("deleted")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> GetDeletedProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = new GetDeletedProductsQuery(pageNumber, pageSize);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<ProductDto>>(result));
    }

    /// <summary>
    /// Restore a soft-deleted product (Admin only)
    /// </summary>
    [HttpPost("{id}/restore")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("expensive")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> RestoreProduct(Guid id, CancellationToken cancellationToken)
    {
        var command = new RestoreProductCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<bool>(result));
    }

    /// <summary>
    /// Upload product image
    /// </summary>
    [HttpPost("{id}/image")]
    [Authorize(Roles = "Admin,Manager")]
    [EnableRateLimiting("expensive")]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    [ProducesResponseType(typeof(ApiResponse<UploadProductImageResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UploadProductImageResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UploadProductImageResponse>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UploadProductImageResponse>>> UploadProductImage(
        Guid id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var command = new UploadProductImageCommand(
            id,
            file.OpenReadStream(),
            file.FileName,
            file.ContentType,
            file.Length);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<UploadProductImageResponse>(result));
    }
}
