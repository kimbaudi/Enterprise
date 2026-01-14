using Asp.Versioning;
using Enterprise.Application.Common.Models;
using Enterprise.Application.Features.Products.Commands.CreateProduct;
using Enterprise.Application.Features.Products.Commands.DeleteProduct;
using Enterprise.Application.Features.Products.Commands.UpdateProduct;
using Enterprise.Application.Features.Products.Queries.GetProductById;
using Enterprise.Application.Features.Products.Queries.GetProductsPaginated;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Enterprise.WebApi.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[EnableRateLimiting("perUser")] // Per-user token bucket
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Get all products
    /// </summary>
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "pageNumber", "pageSize", "searchTerm", "sortBy" })]
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
    /// Get product by ID
    /// </summary>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 120, Location = ResponseCacheLocation.Any, VaryByQueryKeys = new[] { "id" })]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        var query = new GetProductByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(new ApiResponse<ProductDto>(result));
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [EnableRateLimiting("expensive")] // Override: Limit bulk creates
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
    [EnableRateLimiting("expensive")] // Limit delete operations
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var command = new DeleteProductCommand(id);
        var result = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<bool>(result));
    }
}
