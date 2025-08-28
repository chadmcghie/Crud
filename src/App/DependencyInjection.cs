using App.Abstractions;
using App.Behaviors;
using App.Services;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace App;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IWallService, WallService>();
        services.AddScoped<IWindowService, WindowService>();
        
        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));
        
        // Register Validation Pipeline Behavior
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        
        // Register Validators
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        
        // Register AutoMapper
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        
        return services;
    }
}
