using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.AuditLogs.Queries.GetAuditLogsByUser;

public class GetAuditLogsByUserQueryHandler : IRequestHandler<GetAuditLogsByUserQuery, PaginatedResult<AuditLogDto>>
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly IMapper _mapper;

    public GetAuditLogsByUserQueryHandler(IAuditLogRepository auditLogRepository, IMapper mapper)
    {
        _auditLogRepository = auditLogRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<AuditLogDto>> Handle(GetAuditLogsByUserQuery request, CancellationToken cancellationToken)
    {
        var auditLogs = await _auditLogRepository.GetByUserAsync(
            request.UserId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var totalCount = await _auditLogRepository.GetCountByUserAsync(
            request.UserId,
            cancellationToken);

        var auditLogDtos = _mapper.Map<List<AuditLogDto>>(auditLogs);

        return new PaginatedResult<AuditLogDto>(
            auditLogDtos,
            totalCount,
            request.PageNumber,
            request.PageSize);
    }
}
