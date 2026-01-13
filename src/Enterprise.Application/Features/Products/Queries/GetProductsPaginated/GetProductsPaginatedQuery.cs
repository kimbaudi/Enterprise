using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Products.Queries.GetProductsPaginated;

public record GetProductsPaginatedQuery(int PageNumber, int PageSize) : IRequest<PaginatedResult<ProductDto>>;
