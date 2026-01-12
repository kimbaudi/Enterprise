using EnterpriseApi.Application.DTOs;
using MediatR;

namespace EnterpriseApi.Application.Features.Products.Queries.GetAllProducts;

public record GetAllProductsQuery : IRequest<IEnumerable<ProductDto>>;
