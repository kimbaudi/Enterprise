using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.DTOs;
using MediatR;
using System.Text.Json;

namespace Enterprise.Application.Features.Auth.Commands.Verify2FA;

public class Verify2FACommandHandler : IRequestHandler<Verify2FACommand, Verify2FAResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IUnitOfWork _unitOfWork;

    public Verify2FACommandHandler(
        IUserRepository userRepository,
        ITwoFactorService twoFactorService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _twoFactorService = twoFactorService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Verify2FAResponse> Handle(Verify2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        if (string.IsNullOrEmpty(user.TwoFactorSecret))
        {
            throw new InvalidOperationException("Two-factor authentication has not been initialized. Please enable 2FA first.");
        }

        // Validate the code
        if (!_twoFactorService.ValidateCode(user.TwoFactorSecret, request.Code))
        {
            return new Verify2FAResponse
            {
                IsVerified = false,
                RecoveryCodes = new List<string>()
            };
        }

        // Generate recovery codes
        var recoveryCodes = _twoFactorService.GenerateRecoveryCodes();

        // Hash and store recovery codes
        var hashedCodes = recoveryCodes.Select(code => _twoFactorService.HashRecoveryCode(code)).ToList();
        user.RecoveryCodes = JsonSerializer.Serialize(hashedCodes);

        // Enable 2FA
        user.TwoFactorEnabled = true;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new Verify2FAResponse
        {
            IsVerified = true,
            RecoveryCodes = recoveryCodes
        };
    }
}
