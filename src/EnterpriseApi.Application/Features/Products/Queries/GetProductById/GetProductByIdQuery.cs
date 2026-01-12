using EnterpriseApi.Application.DTOs;
using MediatR;

namespace EnterpriseApi.Application.Features.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto?>;
