using AutoMapper;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Common.Models;
using Enterprise.Application.DTOs;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Users.Queries.GetUsersPaginated;

public class GetUsersPaginatedQueryHandler : IRequestHandler<GetUsersPaginatedQuery, PaginatedResult<UserDto>>
{
    private readonly IRepository<User> _userRepository;
    private readonly IMapper _mapper;

    public GetUsersPaginatedQueryHandler(
        IRepository<User> userRepository,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<UserDto>> Handle(GetUsersPaginatedQuery request, CancellationToken cancellationToken)
    {
        var allUsers = await _userRepository.GetAllAsync(cancellationToken);

        // Apply filters
        var filteredUsers = allUsers.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            filteredUsers = filteredUsers.Where(u =>
                u.Username.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                u.Email.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase));
        }

        if (request.IsActive.HasValue)
        {
            filteredUsers = filteredUsers.Where(u => u.IsActive == request.IsActive.Value);
        }

        var totalCount = filteredUsers.Count();

        var paginatedUsers = filteredUsers
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        var userDtos = _mapper.Map<List<UserDto>>(paginatedUsers);

        return PaginatedResult<UserDto>.Create(userDtos, totalCount, request.PageNumber, request.PageSize);
    }
}
