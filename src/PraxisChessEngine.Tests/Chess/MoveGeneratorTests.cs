using Microsoft.VisualStudio.TestTools.UnitTesting;
using PraxisChessEngine.Business.Chess;

namespace PraxisChessEngine.Tests.Chess;

[TestClass]
public sealed class MoveGeneratorTests
{
    private readonly MoveGenerator _generator = new();

    [TestMethod]
    public void StartingPosition_HasTwentyLegalMoves()
    {
        Position position = Position.FromFen(Position.START_FEN);

        Assert.AreEqual(20, _generator.GenerateLegalMoves(position).Count);
    }

    [TestMethod]
    public void StartingPosition_PerftDepthThree_Is8902()
    {
        Position position = Position.FromFen(Position.START_FEN);

        Assert.AreEqual(8902L, Perft(position, 3));
    }

    [TestMethod]
    public void CastlingThroughCheck_IsNotLegal()
    {
        Position position = Position.FromFen("4k3/8/8/8/2b5/8/8/4K2R w K - 0 1");

        IReadOnlyList<Move> moves = _generator.GenerateLegalMoves(position);

        Assert.IsFalse(moves.Contains(Move.Parse("e1g1")));
    }

    private long Perft(Position position, int depth)
    {
        if (depth == 0)
        {
            return 1;
        }

        long nodes = 0;
        foreach (Move move in _generator.GenerateLegalMoves(position))
        {
            Position child = position.Clone();
            child.MakeMove(move);
            nodes += Perft(child, depth - 1);
        }

        return nodes;
    }
}
