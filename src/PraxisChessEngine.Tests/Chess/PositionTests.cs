using PraxisChessEngine.Business.Chess;

namespace PraxisChessEngine.Tests.Chess;

[TestClass]
public sealed class PositionTests
{
    [TestMethod]
    public void StartingPosition_RoundTripsFen()
    {
        Position position = Position.FromFen(Position.START_FEN);

        Assert.AreEqual(Position.START_FEN, position.ToFen());
        Assert.AreEqual(32, position.PieceCount);
    }

    [TestMethod]
    public void DoublePawnMove_SetsEnPassantAndCounters()
    {
        Position position = Position.FromFen(Position.START_FEN);

        position.MakeMove(Move.Parse("e2e4"));

        Assert.AreEqual("e3", position.EnPassantSquare?.ToString());
        Assert.AreEqual(PieceColor.Black, position.SideToMove);
        Assert.AreEqual(0, position.HalfmoveClock);
        Assert.AreEqual(1, position.FullmoveNumber);
    }

    [TestMethod]
    public void Castling_MovesKingAndRook()
    {
        Position position = Position.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");

        position.MakeMove(Move.Parse("e1g1"));

        Assert.AreEqual(PieceType.King, position[Square.Parse("g1")].Type);
        Assert.AreEqual(PieceType.Rook, position[Square.Parse("f1")].Type);
        Assert.IsTrue(position[Square.Parse("h1")].IsEmpty);
    }
}
