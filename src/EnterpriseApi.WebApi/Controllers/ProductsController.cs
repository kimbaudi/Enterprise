using EnterpriseApi.Application.DTOs;
using EnterpriseApi.Application.Interfaces;
using EnterpriseApi.WebApi.Common;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EnterpriseApi.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(
        IProductService productService,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator,
        ILogger<ProductsController> logger)
    {
        _productService = productService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
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
        var products = await _productService.GetAllProductsAsync(cancellationToken);
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
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);
        
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
        var products = await _productService.GetProductsByCategoryAsync(category, cancellationToken);
        return Ok(new ApiResponse<IEnumerable<ProductDto>>(products, "Products retrieved successfully"));
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
        
        var validationResult = await _createValidator.ValidateAsync(createProductDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<ProductDto>(errors));
        }

        var product = await _productService.CreateProductAsync(createProductDto, cancellationToken);
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
        
        var validationResult = await _updateValidator.ValidateAsync(updateProductDto, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(new ApiResponse<ProductDto>(errors));
        }

        var product = await _productService.UpdateProductAsync(id, updateProductDto, cancellationToken);
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
        await _productService.DeleteProductAsync(id, cancellationToken);
        return Ok(new ApiResponse<object>(null!, "Product deleted successfully"));
    }
}
