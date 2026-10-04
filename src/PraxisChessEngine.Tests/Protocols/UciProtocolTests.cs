using Microsoft.VisualStudio.TestTools.UnitTesting;
using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Diagnostics;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Business.Protocols;
using PraxisChessEngine.Business.Search;
using PraxisChessEngine.Integrations.Protocols;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PraxisChessEngine.Tests.Protocols;

[TestClass]
public sealed class UciProtocolTests
{
    [TestMethod]
    public async Task Handshake_ReturnsIdentityAndReady()
    {
        FakeSession session = new();
        EngineIdentity identity = new("Praxis Chess Engine", "Aaron Salisbury", "0.2.0+abcdef");
        UciProtocol protocol = new(session, identity, new NullDiagnosticLogger());
        using StringReader input = new("uci\nisready\nquit\n");
        using StringWriter output = new();

        await protocol.RunAsync(input, output, CancellationToken.None);

        string text = output.ToString();
        StringAssert.Contains(text, "id name Praxis Chess Engine 0.2.0");
        StringAssert.Contains(text, "id author Aaron Salisbury");
        StringAssert.Contains(text, "uciok");
        StringAssert.Contains(text, "readyok");
    }

    [TestMethod]
    public async Task Position_AppliesMovesToSession()
    {
        FakeSession session = new();
        EngineIdentity identity = new("Praxis Chess Engine", "Aaron Salisbury", "0.2.0+abcdef");
        UciProtocol protocol = new(session, identity, new NullDiagnosticLogger());
        using StringReader input = new("position startpos moves e2e4 e7e5\nquit\n");
        using StringWriter output = new();

        await protocol.RunAsync(input, output, CancellationToken.None);

        CollectionAssert.AreEqual(new[] { "e2e4", "e7e5" }, session.Moves.Select(move => move.ToString()).ToArray());
    }

    private sealed class FakeSession : IEngineSession
    {
        public Position Position { get; } = Position.FromFen(Position.START_FEN);

        public List<Move> Moves { get; } = [];

        public void NewGame()
        {
        }

        public void SetPosition(string? fen, IEnumerable<Move> moves)
        {
            Moves.Clear();
            Moves.AddRange(moves);
        }

        public Task<SearchResult> SearchAsync(SearchLimits limits, CancellationToken cancellationToken)
        {
            return Task.FromResult(new SearchResult(Move.Parse("e2e4"), 0, 1, 1, [Move.Parse("e2e4")]));
        }
    }
}
