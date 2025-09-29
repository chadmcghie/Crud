using App.Abstractions;
using App.Behaviors;
using App.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace App;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register example service demonstrating generic repository with specifications
        services.AddScoped<IPersonQueryService, PersonQueryService>();


        // Register MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(DependencyInjection).Assembly));

        // Register Pipeline Behaviors (order matters - validation first, then caching)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(DataAnnotationsValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(CacheInvalidationBehavior<,>));

        // Register Cache Key Generator
        services.AddSingleton<ICacheKeyGenerator, CacheKeyGenerator>();

        return services;
    }
}
