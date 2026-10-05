using PraxisChessEngine.Business.Diagnostics;
using PraxisChessEngine.Business.Engine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PraxisChessEngine.Business.Protocols;

public sealed record EngineIdentity(string Product, string Author, string InformationalVersion)
{
    public string DisplayName
    {
        get
        {
            int buildMetadataIndex = InformationalVersion.IndexOf('+');
            string version = buildMetadataIndex >= 0 ? InformationalVersion[..buildMetadataIndex] : InformationalVersion;
            return $"{Product} {version}";
        }
    }
}

public interface IEngineProtocol
{
    Task RunAsync(TextReader input, TextWriter output, CancellationToken cancellationToken);
}

public interface IEngineProtocolFactory
{
    IEngineProtocol Create(IEngineSession session, EngineIdentity identity, IDiagnosticLogger logger);
}
