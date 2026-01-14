using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductsCached;

public record GetProductsCachedQuery : IRequest<IEnumerable<ProductDto>>;
