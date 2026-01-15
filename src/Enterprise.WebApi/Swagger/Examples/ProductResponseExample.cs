using Enterprise.Application.DTOs;
using Enterprise.WebApi.Common;
using Swashbuckle.AspNetCore.Filters;

namespace Enterprise.WebApi.Swagger.Examples;

public class ProductResponseExample : IExamplesProvider<ApiResponse<ProductDto>>
{
    public ApiResponse<ProductDto> GetExamples()
    {
        return new ApiResponse<ProductDto>(
            new ProductDto
            {
                Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                Name = "Premium Wireless Headphones",
                Description = "High-quality noise-cancelling wireless headphones with 30-hour battery life",
                Price = 299.99m,
                Stock = 150,
                Category = "Electronics",
                SKU = "WH-XB910N-BLK",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            }
        );
    }
}
