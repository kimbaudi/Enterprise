using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using EnterpriseApi.Application.Common.Behaviors;
using EnterpriseApi.Application.Mappings;

namespace EnterpriseApi.Application;

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
