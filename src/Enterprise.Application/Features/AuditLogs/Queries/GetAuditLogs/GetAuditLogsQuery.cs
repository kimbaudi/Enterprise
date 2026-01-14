using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.AuditLogs.Queries.GetAuditLogs;

public record GetAuditLogsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? Action = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
) : IRequest<PaginatedResult<AuditLogDto>>;
