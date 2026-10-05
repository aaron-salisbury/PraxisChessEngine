using Microsoft.VisualStudio.TestTools.UnitTesting;
using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Diagnostics;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Business.Search;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PraxisChessEngine.Tests.Engine;

[TestClass]
public sealed class EngineSessionTests
{
    [TestMethod]
    public async Task SearchAsync_UsesOpeningBookBeforeSearch()
    {
        Move bookMove = Move.Parse("e2e4");
        FakeSearch search = new();
        FakeTablebase tablebase = new(7, Move.Parse("d2d4"));
        EngineSession session = new(search, new MoveGenerator(), new FakeOpeningBook(bookMove), [tablebase], new NullDiagnosticLogger());

        SearchResult result = await session.SearchAsync(new SearchLimits(2), CancellationToken.None);

        Assert.AreEqual(bookMove, result.BestMove);
        Assert.IsFalse(search.WasCalled);
        Assert.IsFalse(tablebase.WasCalled);
    }

    [TestMethod]
    public async Task SearchAsync_ReturnsForcedMoveBeforeCapabilities()
    {
        FakeSearch search = new();
        FakeTablebase tablebase = new(7, Move.Parse("a1a2"));
        EngineSession session = new(search, new MoveGenerator(), new FakeOpeningBook(null), [tablebase], new NullDiagnosticLogger());
        session.SetPosition("8/8/8/8/8/8/Q7/k1K5 b - - 0 1", []);

        SearchResult result = await session.SearchAsync(new SearchLimits(null, TimeSpan.FromSeconds(1), null), CancellationToken.None);

        Assert.AreEqual(Move.Parse("a1a2"), result.BestMove);
        Assert.IsFalse(tablebase.WasCalled);
        Assert.IsFalse(search.WasCalled);
    }

    [TestMethod]
    public async Task SearchAsync_UsesTablebaseWhenPositionIsInScope()
    {
        Move tablebaseMove = Move.Parse("e1e2");
        FakeSearch search = new();
        FakeTablebase tablebase = new(7, tablebaseMove);
        EngineSession session = new(search, new MoveGenerator(), new FakeOpeningBook(null), [tablebase], new NullDiagnosticLogger());
        session.SetPosition("7k/8/8/8/8/8/8/4K3 w - - 0 1", []);

        SearchResult result = await session.SearchAsync(new SearchLimits(2), CancellationToken.None);

        Assert.AreEqual(tablebaseMove, result.BestMove);
        Assert.IsTrue(tablebase.WasCalled);
        Assert.IsFalse(search.WasCalled);
    }


    [TestMethod]
    public async Task SearchAsync_TablebaseTimeoutFallsBackToSearchAndLogs()
    {
        FakeSearch search = new();
        SlowTablebase tablebase = new();
        RecordingLogger logger = new();
        EngineSession session = new(search, new MoveGenerator(), new FakeOpeningBook(null), [tablebase], logger);
        session.SetPosition("7k/8/8/8/8/8/8/4K3 w - - 0 1", []);

        SearchResult result = await session.SearchAsync(new SearchLimits(null, TimeSpan.FromSeconds(2), null), CancellationToken.None);

        Assert.AreEqual(Move.Parse("e1e2"), result.BestMove);
        Assert.IsTrue(tablebase.WasCanceled);
        Assert.IsTrue(search.WasCalled);
        Assert.IsTrue(logger.Messages.Exists(message => message.Contains("Tablebase probe started", StringComparison.Ordinal)));
        Assert.IsTrue(logger.Messages.Exists(message => message.Contains("Tablebase probe timed out", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task SearchAsync_MoveDeadlineIncludesTablebaseProbe()
    {
        FakeSearch search = new();
        SlowTablebase tablebase = new();
        RecordingLogger logger = new();
        EngineSession session = new(search, new MoveGenerator(), new FakeOpeningBook(null), [tablebase], logger);
        session.SetPosition("7k/8/8/8/8/8/8/4K3 w - - 0 1", []);

        SearchResult result = await session.SearchAsync(new SearchLimits(null, TimeSpan.FromMilliseconds(50), null), CancellationToken.None);

        Assert.IsNotNull(result.BestMove);
        Assert.IsTrue(tablebase.WasCanceled);
        Assert.IsFalse(search.WasCalled);
        Assert.IsTrue(logger.Messages.Exists(message => message.Contains("Move deadline reached", StringComparison.Ordinal)));
    }

    private sealed class FakeOpeningBook : IOpeningBook
    {
        private readonly Move? _move;

        public FakeOpeningBook(Move? move)
        {
            _move = move;
        }

        public Move? FindMove(Position position)
        {
            return _move;
        }
    }

    private sealed class FakeTablebase : ITablebaseProvider
    {
        private readonly Move _move;

        public FakeTablebase(int maximumPieceCount, Move move)
        {
            MaximumPieceCount = maximumPieceCount;
            _move = move;
        }

        public int MaximumPieceCount { get; }

        public bool WasCalled { get; private set; }

        public Task<TablebaseProbeResult> ProbeAsync(Position position, CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(new TablebaseProbeResult(TablebaseProbeStatus.Success, _move));
        }
    }


    private sealed class SlowTablebase : ITablebaseProvider
    {
        public int MaximumPieceCount => 7;

        public bool WasCanceled { get; private set; }

        public async Task<TablebaseProbeResult> ProbeAsync(Position position, CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                return new TablebaseProbeResult(TablebaseProbeStatus.Unavailable);
            }
            catch (OperationCanceledException)
            {
                WasCanceled = true;
                throw;
            }
        }
    }

    private sealed class RecordingLogger : IDiagnosticLogger
    {
        public List<string> Messages { get; } = [];

        public void Log(string message)
        {
            Messages.Add(message);
        }

        public void Log(Exception exception, string message)
        {
            Messages.Add(message);
        }
    }

    private sealed class FakeSearch : ISearchService
    {
        public bool WasCalled { get; private set; }

        public SearchResult Search(Position position, SearchLimits limits, CancellationToken cancellationToken)
        {
            WasCalled = true;
            Move move = Move.Parse("e1e2");
            return new SearchResult(move, 0, 1, 1, [move]);
        }
    }
}
