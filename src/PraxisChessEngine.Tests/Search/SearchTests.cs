using Microsoft.VisualStudio.TestTools.UnitTesting;
using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Search;
using System.Threading;

namespace PraxisChessEngine.Tests.Search;

[TestClass]
public sealed class SearchTests
{
    [TestMethod]
    public void Search_FindsForcedMateInOne()
    {
        MoveGenerator generator = new();
        AlphaBetaSearchService search = new(generator, new PositionEvaluator(), new ZobristHasher());
        Position position = Position.FromFen("7k/5Q2/6K1/8/8/8/8/8 w - - 0 1");

        SearchResult result = search.Search(position, new SearchLimits(2), CancellationToken.None);

        Assert.IsNotNull(result.BestMove);
        Position after = position.Clone();
        after.MakeMove(result.BestMove.Value);
        Assert.IsEmpty(generator.GenerateLegalMoves(after));
        Assert.IsTrue(generator.IsInCheck(after, after.SideToMove));
    }
}
