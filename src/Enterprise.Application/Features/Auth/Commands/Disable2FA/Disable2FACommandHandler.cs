using Enterprise.Application.Common.Interfaces;
using MediatR;

namespace Enterprise.Application.Features.Auth.Commands.Disable2FA;

public class Disable2FACommandHandler : IRequestHandler<Disable2FACommand, bool>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IUnitOfWork _unitOfWork;

    public Disable2FACommandHandler(
        IUserRepository userRepository,
        ITwoFactorService twoFactorService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _twoFactorService = twoFactorService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(Disable2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        if (!user.TwoFactorEnabled)
        {
            throw new InvalidOperationException("Two-factor authentication is not enabled");
        }

        // Verify the code before disabling
        if (!_twoFactorService.ValidateCode(user.TwoFactorSecret!, request.Code))
        {
            throw new UnauthorizedAccessException("Invalid verification code");
        }

        // Disable 2FA and clear secrets
        user.TwoFactorEnabled = false;
        user.TwoFactorSecret = null;
        user.RecoveryCodes = null;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
