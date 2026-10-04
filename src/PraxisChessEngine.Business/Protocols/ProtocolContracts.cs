using PraxisChessEngine.Business.Engine;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PraxisChessEngine.Business.Protocols;

public interface IEngineProtocol
{
    Task RunAsync(TextReader input, TextWriter output, CancellationToken cancellationToken);
}

public interface IEngineProtocolFactory
{
    IEngineProtocol Create(IEngineSession session);
}
