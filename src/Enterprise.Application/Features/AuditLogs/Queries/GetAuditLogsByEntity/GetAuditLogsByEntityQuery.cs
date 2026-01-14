using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.AuditLogs.Queries.GetAuditLogsByEntity;

public record GetAuditLogsByEntityQuery(
    string EntityName,
    string? EntityId = null,
    int PageNumber = 1,
    int PageSize = 10
) : IRequest<PaginatedResult<AuditLogDto>>;
