using PraxisChessEngine.Business.Engine;

namespace PraxisChessEngine.Business.Protocols;

public interface IEngineProtocol
{
    Task RunAsync(TextReader input, TextWriter output, CancellationToken cancellationToken);
}

public interface IEngineProtocolFactory
{
    IEngineProtocol Create(IEngineSession session);
}
