using Microsoft.Extensions.DependencyInjection;
using System;

namespace PraxisChessEngine.Integrations;

public static class DependencyInjection
{
    /// <summary>
    /// Register internal integrations-tier services.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalIntegrationsServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
