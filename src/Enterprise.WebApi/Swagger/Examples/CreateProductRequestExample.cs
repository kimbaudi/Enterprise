using Enterprise.Application.Features.Products.Commands.CreateProduct;
using Swashbuckle.AspNetCore.Filters;

namespace Enterprise.WebApi.Swagger.Examples;

public class CreateProductRequestExample : IExamplesProvider<CreateProductCommand>
{
    public CreateProductCommand GetExamples()
    {
        return new CreateProductCommand(
            Name: "Premium Wireless Headphones",
            Description: "High-quality noise-cancelling wireless headphones with 30-hour battery life",
            Price: 299.99m,
            Stock: 150,
            Category: "Electronics",
            SKU: "WH-XB910N-BLK"
        );
    }
}
