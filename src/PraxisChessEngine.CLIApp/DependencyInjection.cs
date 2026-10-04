using Microsoft.Extensions.DependencyInjection;
using PraxisChessEngine.Business;
using PraxisChessEngine.Data;
using PraxisChessEngine.Integrations;

namespace PraxisChessEngine.CLIApp;

internal static class DependencyInjection
{
    internal static IServiceCollection BuildServiceCollection()
    {
        IServiceCollection services = new ServiceCollection();

        services.RegisterInternalIntegrationsServices()
            .RegisterInternalDataServices()
            .RegisterInternalBusinessServices();

        return services;
    }
}
