using Microsoft.Extensions.DependencyInjection;
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
            Assembly assembly = typeof(Program).Assembly;
            string product = assembly.GetCustomAttribute<AssemblyProductAttribute>()?.Product
                ?? assembly.GetName().Name
                ?? "Praxis Chess Engine";
            string author = assembly.GetCustomAttribute<AssemblyCompanyAttribute>()?.Company ?? string.Empty;
            string informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()
                ?? string.Empty;
            EngineIdentity identity = new(product, author, informationalVersion);
            IEngineProtocol protocol = protocolFactory.Create(session, identity);

            await protocol.RunAsync(Console.In, Console.Out, CancellationToken.None);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Application terminated unexpectedly: {ex}");
            return 1;
        }
    }
}
