using PraxisChessEngine.Business.Chess;

namespace PraxisChessEngine.Business.Search;

internal sealed class PositionEvaluator
{
    internal int Evaluate(Position position)
    {
        int score = 0;

        for (int index = 0; index < 64; index++)
        {
            Square square = new(index);
            Piece piece = position[square];
            if (piece.IsEmpty)
            {
                continue;
            }

            int value = piece.Type switch
            {
                PieceType.Pawn => 100,
                PieceType.Knight => 320,
                PieceType.Bishop => 330,
                PieceType.Rook => 500,
                PieceType.Queen => 900,
                PieceType.King => 0,
                _ => 0
            };

            int center = 6 - Math.Abs(3 - square.File) - Math.Abs(3 - square.Rank);
            int activity = piece.Type is PieceType.Knight or PieceType.Bishop ? center * 2 : 0;
            score += (value + activity) * (piece.Color == PieceColor.White ? 1 : -1);
        }

        return position.SideToMove == PieceColor.White ? score : -score;
    }
}
