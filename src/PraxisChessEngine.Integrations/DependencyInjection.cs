using Microsoft.Extensions.DependencyInjection;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Business.Protocols;
using PraxisChessEngine.Integrations.Protocols;
using PraxisChessEngine.Integrations.Tablebases;
using System;

namespace PraxisChessEngine.Integrations;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInternalIntegrationsServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddHttpClient("LichessTablebase", client =>
        {
            client.BaseAddress = new Uri("https://tablebase.lichess.ovh/");
            client.Timeout = TimeSpan.FromSeconds(2);
        });

        services.AddSingleton<IEngineProtocolFactory, UciProtocolFactory>();
        services.AddSingleton<ITablebaseProvider, LichessTablebaseProvider>();

        return services;
    }
}
