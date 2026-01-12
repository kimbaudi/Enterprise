using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using EnterpriseApi.Application.Mappings;

namespace EnterpriseApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // MediatR for CQRS
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

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
