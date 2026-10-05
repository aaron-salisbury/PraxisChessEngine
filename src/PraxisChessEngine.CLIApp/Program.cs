using Microsoft.Extensions.DependencyInjection;
using PraxisChessEngine.Business.Diagnostics;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Business.Protocols;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace PraxisChessEngine.CLIApp;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        await using ServiceProvider serviceProvider = DependencyInjection.BuildServiceCollection().BuildServiceProvider();

        try
        {
            IEngineSession session = serviceProvider.GetRequiredService<IEngineSession>();
            IEngineProtocolFactory protocolFactory = serviceProvider.GetRequiredService<IEngineProtocolFactory>();
            IDiagnosticLogger logger = serviceProvider.GetRequiredService<IDiagnosticLogger>();
            IEngineProtocol protocol = protocolFactory.Create(session, GetEngineIdentity(), logger);

            await protocol.RunAsync(Console.In, Console.Out, CancellationToken.None);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Application terminated unexpectedly: {ex}");
            return 1;
        }
    }

    private static EngineIdentity GetEngineIdentity()
    {
        Assembly assembly = typeof(Program).Assembly;

        return new EngineIdentity(
            assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                ?? throw new InvalidOperationException("AssemblyProductAttribute is missing."),
            assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company
                ?? throw new InvalidOperationException("AssemblyCompanyAttribute is missing."),
            assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? throw new InvalidOperationException("AssemblyInformationalVersionAttribute is missing."));
    }
}
