using Microsoft.VisualStudio.TestTools.UnitTesting;
using PraxisChessEngine.Business.Chess;
using System.Collections.Generic;
using System.Linq;

namespace PraxisChessEngine.Tests.Chess;

[TestClass]
public sealed class MoveGeneratorTests
{
    private readonly MoveGenerator _generator = new();

    [TestMethod]
    public void StartingPosition_HasTwentyLegalMoves()
    {
        Position position = Position.FromFen(Position.START_FEN);

        Assert.HasCount(20, _generator.GenerateLegalMoves(position));
    }

    [DataTestMethod]
    [DataRow(Position.START_FEN, 4, 197281L, DisplayName = "Starting position depth 4")]
    [DataRow("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1", 3, 97862L, DisplayName = "Kiwipete depth 3")]
    [DataRow("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1", 4, 43238L, DisplayName = "Position 3 depth 4")]
    [DataRow("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1", 3, 9467L, DisplayName = "Position 4 depth 3")]
    [DataRow("rnbq1k1r/pp1Pbppp/2p5/8/2B5/8/PPP1NnPP/RNBQK2R w KQ - 1 8", 3, 62379L, DisplayName = "Position 5 depth 3")]
    [DataRow("r4rk1/1pp1qppp/p1np1n2/2b1p1B1/2B1P1b1/P1NP1N2/1PP1QPPP/R4RK1 w - - 0 10", 3, 89890L, DisplayName = "Position 6 depth 3")]
    public void CanonicalPosition_PerftMatchesKnownNodeCount(string fen, int depth, long expectedNodes)
    {
        Position position = Position.FromFen(fen);

        Assert.AreEqual(expectedNodes, Perft(position, depth));
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
