using EnterpriseApi.Application.Common.Models;
using EnterpriseApi.Application.DTOs;
using EnterpriseApi.Application.Features.Products.Commands.CreateProduct;
using EnterpriseApi.Application.Features.Products.Commands.DeleteProduct;
using EnterpriseApi.Application.Features.Products.Commands.UpdateProduct;
using EnterpriseApi.Application.Features.Products.Queries.GetAllProducts;
using EnterpriseApi.Application.Features.Products.Queries.GetProductById;
using EnterpriseApi.Application.Features.Products.Queries.GetProductsByCategory;
using EnterpriseApi.Application.Features.Products.Queries.GetProductsPaginated;
using EnterpriseApi.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IMediator mediator,
        ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all products
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetAllProducts(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting all products");
        var products = await _mediator.Send(new GetAllProductsQuery(), cancellationToken);
        return Ok(new ApiResponse<IEnumerable<ProductDto>>(products, "Products retrieved successfully"));
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> GetProductById(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting product with ID: {ProductId}", id);
        var product = await _mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        
        if (product == null)
        {
            return NotFound(new ApiResponse<ProductDto>($"Product with ID {id} not found"));
        }

        return Ok(new ApiResponse<ProductDto>(product, "Product retrieved successfully"));
    }

    /// <summary>
    /// Get products by category
    /// </summary>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ProductDto>>>> GetProductsByCategory(string category, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Getting products for category: {Category}", category);
        var products = await _mediator.Send(new GetProductsByCategoryQuery(category), cancellationToken);
        return Ok(new ApiResponse<IEnumerable<ProductDto>>(products, "Products retrieved successfully"));
    }

    /// <summary>
    /// Get products with pagination
    /// </summary>
    [HttpGet("paginated")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResult<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResult<ProductDto>>>> GetProductsPaginated(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting products page {PageNumber} with size {PageSize}", pageNumber, pageSize);
        var paginatedProducts = await _mediator.Send(new GetProductsPaginatedQuery(pageNumber, pageSize), cancellationToken);
        return Ok(new ApiResponse<PaginatedResult<ProductDto>>(paginatedProducts, "Products retrieved successfully"));
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> CreateProduct([FromBody] CreateProductDto createProductDto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating new product: {ProductName}", createProductDto.Name);
        
        var command = new CreateProductCommand(
            createProductDto.Name,
            createProductDto.Description,
            createProductDto.Price,
            createProductDto.Stock,
            createProductDto.Category,
            createProductDto.SKU
        );

        // Validation is handled by ValidationBehavior pipeline
        var product = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, 
            new ApiResponse<ProductDto>(product, "Product created successfully"));
    }

    /// <summary>
    /// Update an existing product
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ProductDto>>> UpdateProduct(Guid id, [FromBody] UpdateProductDto updateProductDto, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating product with ID: {ProductId}", id);
        
        var command = new UpdateProductCommand(
            id,
            updateProductDto.Name,
            updateProductDto.Description,
            updateProductDto.Price,
            updateProductDto.Stock,
            updateProductDto.Category
        );

        // Validation is handled by ValidationBehavior pipeline
        var product = await _mediator.Send(command, cancellationToken);
        return Ok(new ApiResponse<ProductDto>(product, "Product updated successfully"));
    }

    /// <summary>
    /// Delete a product
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting product with ID: {ProductId}", id);
        await _mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return Ok(new ApiResponse<object>(null!, "Product deleted successfully"));
    }
}
