using Microsoft.Extensions.DependencyInjection;
using PraxisChessEngine.Business;
using PraxisChessEngine.Business.Diagnostics;
using PraxisChessEngine.CLIApp.Diagnostics;
using PraxisChessEngine.Data;
using PraxisChessEngine.Integrations;
using System;
using System.IO;

namespace PraxisChessEngine.CLIApp;

internal static class DependencyInjection
{
    internal static IServiceCollection BuildServiceCollection()
    {
        IServiceCollection services = new ServiceCollection();

#if DEBUG
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string logPath = Path.Combine(appDataPath, "PraxisChessEngine", "Logs", "praxis.log");
        services.AddSingleton<IDiagnosticLogger>(_ => new FileDiagnosticLogger(logPath));
#else
        services.AddSingleton<IDiagnosticLogger, NullDiagnosticLogger>();
#endif

        services.RegisterInternalIntegrationsServices()
            .RegisterInternalDataServices()
            .RegisterInternalBusinessServices();

        return services;
    }
}
