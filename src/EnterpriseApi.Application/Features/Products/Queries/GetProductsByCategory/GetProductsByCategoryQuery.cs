using EnterpriseApi.Application.DTOs;
using MediatR;

namespace EnterpriseApi.Application.Features.Products.Queries.GetProductsByCategory;

public record GetProductsByCategoryQuery(string Category) : IRequest<IEnumerable<ProductDto>>;
