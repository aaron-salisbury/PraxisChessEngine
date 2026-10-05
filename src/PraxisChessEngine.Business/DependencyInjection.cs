using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Diagnostics;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Business.Search;
using System;

namespace PraxisChessEngine.Business;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInternalBusinessServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDiagnosticLogger, NullDiagnosticLogger>();
        services.AddSingleton<MoveGenerator>();
        services.AddSingleton<PositionEvaluator>();
        services.AddSingleton<ZobristHasher>();
        services.AddTransient<ISearchService, AlphaBetaSearchService>();
        services.AddTransient<IEngineSession, EngineSession>();

        return services;
    }
}
