using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Users.Queries.GetUsersPaginated;

public record GetUsersPaginatedQuery(
    int PageNumber,
    int PageSize,
    string? SearchTerm = null,
    bool? IsActive = null
) : IRequest<PaginatedResult<UserDto>>;
