using Microsoft.VisualStudio.TestTools.UnitTesting;
using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Business.Search;
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
        EngineSession session = new(search, new MoveGenerator(), new FakeOpeningBook(bookMove), [tablebase]);

        SearchResult result = await session.SearchAsync(new SearchLimits(2), CancellationToken.None);

        Assert.AreEqual(bookMove, result.BestMove);
        Assert.IsFalse(search.WasCalled);
        Assert.IsFalse(tablebase.WasCalled);
    }

    [TestMethod]
    public async Task SearchAsync_UsesTablebaseWhenPositionIsInScope()
    {
        Move tablebaseMove = Move.Parse("e1e2");
        FakeSearch search = new();
        FakeTablebase tablebase = new(7, tablebaseMove);
        EngineSession session = new(search, new MoveGenerator(), new FakeOpeningBook(null), [tablebase]);
        session.SetPosition("7k/8/8/8/8/8/8/4K3 w - - 0 1", []);

        SearchResult result = await session.SearchAsync(new SearchLimits(2), CancellationToken.None);

        Assert.AreEqual(tablebaseMove, result.BestMove);
        Assert.IsTrue(tablebase.WasCalled);
        Assert.IsFalse(search.WasCalled);
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

    private sealed class FakeSearch : ISearchService
    {
        public bool WasCalled { get; private set; }

        public SearchResult Search(Position position, SearchLimits limits, CancellationToken cancellationToken)
        {
            WasCalled = true;
            Move move = Move.Parse("g1f3");
            return new SearchResult(move, 0, 1, 1, [move]);
        }
    }
}
