using CnabStore.Api.Application.Interfaces;

namespace CnabStore.Api.Application;

/// <summary>
/// Extension methods to register application-layer services in the DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers application services and abstractions.
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICnabLineParser, CnabLineParser>();
        services.AddScoped<ICnabImportService, CnabImportService>();

        return services;
    }
}
