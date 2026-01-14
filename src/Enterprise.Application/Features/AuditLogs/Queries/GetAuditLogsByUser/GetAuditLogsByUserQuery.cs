using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.AuditLogs.Queries.GetAuditLogsByUser;

public record GetAuditLogsByUserQuery(
    string UserId,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedResult<AuditLogDto>>;
