using AutoMapper;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.AuditLogs.Queries.GetAuditLogsByEntity;

public class GetAuditLogsByEntityQueryHandler : IRequestHandler<GetAuditLogsByEntityQuery, PaginatedResult<AuditLogDto>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMapper _mapper;

    public GetAuditLogsByEntityQueryHandler(IAuditLogRepository auditLogRepository, IMapper mapper)
    {
        _auditLogRepository = auditLogRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<AuditLogDto>> Handle(GetAuditLogsByEntityQuery request, CancellationToken cancellationToken)
    {
        var auditLogs = await _auditLogRepository.GetByEntityAsync(
            request.EntityName,
            request.EntityId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var totalCount = await _auditLogRepository.GetCountByEntityAsync(
            request.EntityName,
            request.EntityId,
            cancellationToken);

        var auditLogDtos = _mapper.Map<List<AuditLogDto>>(auditLogs);

        return new PaginatedResult<AuditLogDto>(
            auditLogDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
