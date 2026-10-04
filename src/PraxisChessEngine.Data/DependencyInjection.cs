using Microsoft.Extensions.DependencyInjection;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Data.OpeningBook;

namespace PraxisChessEngine.Data;

public static class DependencyInjection
{
    public static IServiceCollection RegisterInternalDataServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IOpeningBook, InMemoryOpeningBook>();

        return services;
    }
}
