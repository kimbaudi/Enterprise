using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductsByCategory;

public record GetProductsByCategoryQuery(string Category) : IRequest<IEnumerable<ProductDto>>;
