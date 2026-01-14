using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.AuditLogs.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, PaginatedResult<AuditLogDto>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMapper _mapper;

    public GetAuditLogsQueryHandler(IAuditLogRepository auditLogRepository, IMapper mapper)
    {
        _auditLogRepository = auditLogRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<AuditLogDto>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Domain.Entities.AuditLog> auditLogs;
        int totalCount;

        // Filter by date range if provided
        if (request.StartDate.HasValue && request.EndDate.HasValue)
        {
            auditLogs = await _auditLogRepository.GetByDateRangeAsync(
                request.StartDate.Value,
                request.EndDate.Value,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            totalCount = await _auditLogRepository.GetCountAsync(cancellationToken);
        }
        // Filter by action if provided
        else if (!string.IsNullOrEmpty(request.Action))
        {
            auditLogs = await _auditLogRepository.GetByActionAsync(
                request.Action,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            totalCount = await _auditLogRepository.GetCountAsync(cancellationToken);
        }
        // Get all audit logs
        else
        {
            auditLogs = await _auditLogRepository.GetAllAsync(
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            totalCount = await _auditLogRepository.GetCountAsync(cancellationToken);
        }

        var auditLogDtos = _mapper.Map<List<AuditLogDto>>(auditLogs);

        return new PaginatedResult<AuditLogDto>(
            auditLogDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
