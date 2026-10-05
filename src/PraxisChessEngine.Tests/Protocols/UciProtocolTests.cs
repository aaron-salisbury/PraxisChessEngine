using Microsoft.VisualStudio.TestTools.UnitTesting;
using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Diagnostics;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Business.Protocols;
using PraxisChessEngine.Business.Search;
using PraxisChessEngine.Integrations.Protocols;
using System;
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
        Assert.Contains("id name Praxis Chess Engine 0.2.0", text);
        Assert.Contains("id author Aaron Salisbury", text);
        Assert.Contains("uciok", text);
        Assert.Contains("readyok", text);
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


    [TestMethod]
    public async Task Go_WithClockLimits_ReturnsBestMove()
    {
        FakeSession session = new();
        EngineIdentity identity = new("Praxis Chess Engine", "Aaron Salisbury", "0.2.0");
        UciProtocol protocol = new(session, identity, new NullDiagnosticLogger());
        using StringReader input = new("position startpos\ngo wtime 1000 btime 1000 winc 0 binc 0\nquit\n");
        using StringWriter output = new();

        await protocol.RunAsync(input, output, CancellationToken.None);

        Assert.Contains("bestmove e2e4", output.ToString());
        Assert.IsNotNull(session.LastLimits);
        Assert.IsNotNull(session.LastLimits.MoveTime);
    }

    [TestMethod]
    public async Task NewGame_ResetsSession()
    {
        FakeSession session = new();
        EngineIdentity identity = new("Praxis Chess Engine", "Aaron Salisbury", "0.2.0");
        UciProtocol protocol = new(session, identity, new NullDiagnosticLogger());
        using StringReader input = new("ucinewgame\nquit\n");
        using StringWriter output = new();

        await protocol.RunAsync(input, output, CancellationToken.None);

        Assert.IsTrue(session.NewGameWasCalled);
    }

    [TestMethod]
    public async Task UnknownAndUnsupportedCommands_DoNotPolluteProtocolOutput()
    {
        FakeSession session = new();
        UciProtocol protocol = new(session, new EngineIdentity("Praxis Chess Engine", "Aaron Salisbury", "0.2.0"), new NullDiagnosticLogger());
        using StringReader input = new("nonsense\nsetoption name Unknown value 1\nisready\nquit\n");
        using StringWriter output = new();

        await protocol.RunAsync(input, output, CancellationToken.None);

        Assert.AreEqual($"readyok{Environment.NewLine}", output.ToString());
    }

    [TestMethod]
    public async Task Go_EmitsSearchInfoBeforeBestMove()
    {
        FakeSession session = new();
        UciProtocol protocol = new(session, new EngineIdentity("Praxis Chess Engine", "Aaron Salisbury", "0.2.0"), new NullDiagnosticLogger());
        using StringReader input = new("go depth 2\nquit\n");
        using StringWriter output = new();

        await protocol.RunAsync(input, output, CancellationToken.None);

        string text = output.ToString();
        Assert.Contains("info depth 1 score cp 0 nodes 1 pv e2e4", text);
        Assert.IsLessThan(text.IndexOf("bestmove e2e4", StringComparison.Ordinal), text.IndexOf("info depth", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task DebugCommands_AreAcceptedWithoutProtocolOutput()
    {
        FakeSession session = new();
        UciProtocol protocol = new(session, new EngineIdentity("Praxis Chess Engine", "Aaron Salisbury", "0.2.0"), new NullDiagnosticLogger());
        using StringReader input = new("debug on\ndebug off\nquit\n");
        using StringWriter output = new();

        await protocol.RunAsync(input, output, CancellationToken.None);

        Assert.AreEqual(string.Empty, output.ToString());
    }

    private sealed class FakeSession : IEngineSession
    {
        public Position Position { get; } = Position.FromFen(Position.START_FEN);

        public List<Move> Moves { get; } = [];

        public SearchLimits? LastLimits { get; private set; }

        public bool NewGameWasCalled { get; private set; }

        public void NewGame()
        {
            NewGameWasCalled = true;
        }

        public void SetPosition(string? fen, IEnumerable<Move> moves)
        {
            Moves.Clear();
            Moves.AddRange(moves);
        }

        public Task<SearchResult> SearchAsync(SearchLimits limits, CancellationToken cancellationToken)
        {
            LastLimits = limits;
            return Task.FromResult(new SearchResult(Move.Parse("e2e4"), 0, 1, 1, [Move.Parse("e2e4")]));
        }
    }
}
