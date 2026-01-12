using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using EnterpriseApi.Application.Interfaces;
using EnterpriseApi.Application.Services;
using EnterpriseApi.Application.Mappings;

namespace EnterpriseApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(typeof(MappingProfile));

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<MappingProfile>();

        // Services
        services.AddScoped<IProductService, ProductService>();

        return services;
    }
}
