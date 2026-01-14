using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.DTOs;
using MediatR;

namespace Enterprise.Application.Features.Auth.Commands.Enable2FA;

public class Enable2FACommandHandler : IRequestHandler<Enable2FACommand, Enable2FAResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IUnitOfWork _unitOfWork;

    public Enable2FACommandHandler(
        IUserRepository userRepository,
        ITwoFactorService twoFactorService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _twoFactorService = twoFactorService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Enable2FAResponse> Handle(Enable2FACommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        // Generate a new secret
        var secret = _twoFactorService.GenerateSecret();

        // Store the secret (but don't enable 2FA yet - user must verify first)
        user.TwoFactorSecret = secret;
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate QR code URL
        var qrCodeUrl = _twoFactorService.GenerateQrCodeUrl(user.Email, secret);

        return new Enable2FAResponse
        {
            Secret = secret,
            QrCodeUrl = qrCodeUrl,
            ManualEntryKey = FormatSecretForManualEntry(secret)
        };
    }

    private string FormatSecretForManualEntry(string secret)
    {
        // Format as groups of 4 characters for easier manual entry
        var formatted = "";
        for (int i = 0; i < secret.Length; i += 4)
        {
            if (i > 0) formatted += " ";
            formatted += secret.Substring(i, Math.Min(4, secret.Length - i));
        }
        return formatted;
    }
}
