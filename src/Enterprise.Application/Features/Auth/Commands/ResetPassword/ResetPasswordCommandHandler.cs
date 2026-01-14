using Enterprise.Application.Common.Exceptions;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Enterprise.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Password reset attempted for non-existent email: {Email}", request.Email);
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Email", new[] { "Invalid password reset request" } }
            });
        }

        if (!user.HasValidPasswordResetToken(request.Token))
        {
            _logger.LogWarning("Invalid or expired password reset token for user: {Email}", request.Email);
            throw new ValidationException(new Dictionary<string, string[]>
            {
                { "Token", new[] { "Invalid or expired password reset token" } }
            });
        }

        // Hash the new password
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);

        // Clear the reset token
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;

        // Reset failed login attempts
        user.FailedLoginAttempts = 0;
        user.LockoutEnd = null;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password successfully reset for user: {Email}", request.Email);

        return new ResetPasswordResponse("Your password has been reset successfully.");
    }
}
