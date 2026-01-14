using AutoMapper;
using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Features.Users.Queries;
using Enterprise.Domain.Entities;
using MediatR;

namespace Enterprise.Application.Features.Users.Commands.UpdateUser;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDto>
{
    private readonly IRepository<User> _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateUserCommandHandler(
        IRepository<User> userRepository,
        IUnitOfWork unitOfWork,
        IMapper mapper)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<UserDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var existingUser = await _userRepository.GetByIdAsync(request.Id, cancellationToken);

        if (existingUser == null)
        {
            throw new NotFoundException("User", request.Id);
        }

        existingUser.Email = request.Email;
        existingUser.FirstName = request.FirstName;
        existingUser.LastName = request.LastName;
        existingUser.IsActive = request.IsActive;
        existingUser.UpdatedAt = DateTime.UtcNow;

        await _userRepository.UpdateAsync(existingUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(existingUser);
    }
}
