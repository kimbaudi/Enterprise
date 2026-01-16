using Enterprise.Application.Common.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Enterprise.Application.Common.Behaviors;

/// <summary>
/// Prevents duplicate command execution by tracking request IDs in Redis cache.
/// Only applies to commands (IRequest with void or non-query results).
/// </summary>
public class IdempotencyBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : class
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<IdempotencyBehavior<TRequest, TResponse>> _logger;
    private static readonly TimeSpan _idempotencyWindow = TimeSpan.FromMinutes(15);

    public IdempotencyBehavior(
        ICacheService cacheService,
        ILogger<IdempotencyBehavior<TRequest, TResponse>> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only apply to commands (not queries)
        var requestName = typeof(TRequest).Name;
        if (requestName.Contains("Query", StringComparison.OrdinalIgnoreCase))
        {
            return await next();
        }

        // Generate idempotency key from request content
        var idempotencyKey = GenerateIdempotencyKey(request);
        var cacheKey = $"idempotency:{requestName}:{idempotencyKey}";

        // Check if this exact request was already processed
        var cachedResult = await _cacheService.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cachedResult != null)
        {
            _logger.LogWarning(
                "Duplicate request detected for {RequestName}. Returning cached result. Key: {Key}",
                requestName,
                idempotencyKey);
            return cachedResult;
        }

        // Check if request is currently being processed (prevents concurrent duplicates)
        var processingKey = $"idempotency:processing:{requestName}:{idempotencyKey}";
        var isProcessing = await _cacheService.GetAsync<string>(processingKey, cancellationToken);
        if (isProcessing != null)
        {
            _logger.LogWarning(
                "Concurrent duplicate request detected for {RequestName}. Key: {Key}",
                requestName,
                idempotencyKey);
            throw new InvalidOperationException($"A request with the same content is currently being processed. Please wait for the first request to complete.");
        }

        try
        {
            // Mark as processing
            await _cacheService.SetAsync(processingKey, "processing", TimeSpan.FromMinutes(5), cancellationToken);

            // Execute the command
            var response = await next();

            // Cache the result
            await _cacheService.SetAsync(cacheKey, response, _idempotencyWindow, cancellationToken);

            _logger.LogInformation(
                "Request processed and cached for idempotency. {RequestName}, Key: {Key}",
                requestName,
                idempotencyKey);

            return response;
        }
        finally
        {
            // Remove processing lock
            await _cacheService.RemoveAsync(processingKey, cancellationToken);
        }
    }

    private string GenerateIdempotencyKey(TRequest request)
    {
        // Serialize request to JSON
        var json = JsonSerializer.Serialize(request, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Generate SHA256 hash for consistent key
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(json));
        return Convert.ToBase64String(hashBytes);
    }
}
