using Enterprise.Application.Common.Behaviors;
using Enterprise.Application.Common.Interfaces;
using Enterprise.Application.Mappings;
using Enterprise.Application.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Enterprise.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR for CQRS
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // MediatR Pipeline Behaviors (order matters!)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuditLoggingBehavior<,>));

        // Audit Log Queue (Singleton - shared across all requests)
        services.AddSingleton<IAuditLogQueue, AuditLogQueue>();

        // AutoMapper
        services.AddAutoMapper(config =>
        {
            config.AddProfile<MappingProfile>();
        }, typeof(MappingProfile).Assembly);

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<MappingProfile>();

        return services;
    }
}
