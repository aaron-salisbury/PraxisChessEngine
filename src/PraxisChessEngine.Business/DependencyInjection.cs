using System;
using Microsoft.Extensions.DependencyInjection;
using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Business.Protocols;
using PraxisChessEngine.Business.Search;

namespace PraxisChessEngine.Business;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInternalBusinessServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<MoveGenerator>();
        services.AddSingleton<PositionEvaluator>();
        services.AddSingleton<ZobristHasher>();
        services.AddSingleton<ISearchService, AlphaBetaSearchService>();
        services.AddTransient<IEngineSession, EngineSession>();
        services.AddSingleton<IEngineProtocolFactory, UciProtocolFactory>();

        return services;
    }
}
