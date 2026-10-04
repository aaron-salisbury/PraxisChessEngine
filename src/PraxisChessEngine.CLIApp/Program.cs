using Microsoft.Extensions.DependencyInjection;
using System;

namespace PraxisChessEngine.CLIApp;

public class Program
{
    public static int Main(string[] args)
    {
        ServiceProvider? serviceProvider = null;

        try
        {
            IServiceCollection services = DependencyInjection.BuildServiceCollection();
            serviceProvider = services.BuildServiceProvider();

            Console.WriteLine("Welcome to Praxis Chess Engine!");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Application terminated unexpectedly: {ex}");
            return 1;
        }
        finally
        {
            serviceProvider?.Dispose();
        }
    }
}
