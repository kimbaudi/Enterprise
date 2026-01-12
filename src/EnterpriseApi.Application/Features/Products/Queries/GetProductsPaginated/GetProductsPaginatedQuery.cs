using EnterpriseApi.Application.Common.Models;
using EnterpriseApi.Application.DTOs;
using MediatR;

namespace EnterpriseApi.Application.Features.Products.Queries.GetProductsPaginated;

public record GetProductsPaginatedQuery(int PageNumber, int PageSize) : IRequest<PaginatedResult<ProductDto>>;
