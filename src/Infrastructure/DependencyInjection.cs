using App.Abstractions;
using Infrastructure.Repositories.InMemory;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureInMemory(this IServiceCollection services)
    {
        services.AddSingleton<IPersonRepository, InMemoryPersonRepository>();
        services.AddSingleton<IRoleRepository, InMemoryRoleRepository>();
        return services;
    }
}
