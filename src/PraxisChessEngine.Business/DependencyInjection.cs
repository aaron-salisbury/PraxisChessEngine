using Microsoft.Extensions.DependencyInjection;
using System;

namespace PraxisChessEngine.Business;

public static class DependencyInjection
{
    /// <summary>
    /// Register internal business-tier services.
    /// </summary>
    /// <returns>A reference to this instance after the operation has completed.</returns>
    public static IServiceCollection RegisterInternalBusinessServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
