using Microsoft.Extensions.DependencyInjection;
using System;

namespace PraxisChessEngine.Data;

public static class DependencyInjection
{
    /// <summary>
    /// Register internal data-tier services.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalDataServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
