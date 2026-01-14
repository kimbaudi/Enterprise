using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;

namespace Enterprise.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordCommandHandler> _logger;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IEmailService emailService,
        ILogger<ForgotPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Don't reveal if user exists or not (security best practice)
        if (user == null)
        {
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", request.Email);
            return new ForgotPasswordResponse("If the email exists, a password reset link has been sent.");
        }

        // Generate a secure random token
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Send password reset email
        await _emailService.SendPasswordResetEmailAsync(
            user.Email,
            $"{user.FirstName} {user.LastName}",
            token,
            cancellationToken);

        _logger.LogInformation("Password reset token generated for user: {Email}", request.Email);

        return new ForgotPasswordResponse("If the email exists, a password reset link has been sent.");
    }
}
