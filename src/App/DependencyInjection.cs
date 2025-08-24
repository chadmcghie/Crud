using App.Abstractions;
using App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace App;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IWallService, WallService>();
        return services;
    }
}
