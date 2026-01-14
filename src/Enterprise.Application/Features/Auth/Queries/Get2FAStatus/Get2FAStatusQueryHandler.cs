using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.DTOs;
using MediatR;
using System.Text.Json;

namespace Enterprise.Application.Features.Auth.Queries.Get2FAStatus;

public class Get2FAStatusQueryHandler : IRequestHandler<Get2FAStatusQuery, TwoFactorStatusResponse>
{
    private readonly IUserRepository _userRepository;

    public Get2FAStatusQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<TwoFactorStatusResponse> Handle(Get2FAStatusQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user == null)
        {
            throw new UnauthorizedAccessException("User not found");
        }

        bool hasRecoveryCodes = false;
        if (!string.IsNullOrEmpty(user.RecoveryCodes))
        {
            try
            {
                var codes = JsonSerializer.Deserialize<List<string>>(user.RecoveryCodes);
                hasRecoveryCodes = codes != null && codes.Count > 0;
            }
            catch
            {
                // Invalid JSON
            }
        }

        return new TwoFactorStatusResponse
        {
            IsEnabled = user.TwoFactorEnabled,
            HasRecoveryCodes = hasRecoveryCodes
        };
    }
}
