using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductById;

public record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
