using System;
using System.Collections.Generic;
namespace PraxisChessEngine.Business.Chess;

public sealed class MoveGenerator
{
    private static readonly (int File, int Rank)[] KNIGHT_OFFSETS =
    [
        (1, 2), (2, 1), (2, -1), (1, -2), (-1, -2), (-2, -1), (-2, 1), (-1, 2)
    ];

    private static readonly (int File, int Rank)[] KING_OFFSETS =
    [
        (-1, -1), (0, -1), (1, -1), (-1, 0), (1, 0), (-1, 1), (0, 1), (1, 1)
    ];

    public IReadOnlyList<Move> GenerateLegalMoves(Position position)
    {
        List<Move> legal = [];
        PieceColor mover = position.SideToMove;

        foreach (Move move in GeneratePseudoLegalMoves(position))
        {
            Position next = position.Clone();
            next.MakeMove(move);
            if (!IsInCheck(next, mover))
            {
                legal.Add(move);
            }
        }

        return legal;
    }

    public bool IsInCheck(Position position, PieceColor color)
    {
        for (int index = 0; index < 64; index++)
        {
            Piece piece = position[new Square(index)];
            if (piece.Type == PieceType.King && piece.Color == color)
            {
                return IsSquareAttacked(position, new Square(index), Position.Opposite(color));
            }
        }

        throw new InvalidOperationException($"Position contains no {color} king.");
    }

    public bool IsSquareAttacked(Position position, Square square, PieceColor attacker)
    {
        int pawnSourceRank = square.Rank + (attacker == PieceColor.White ? -1 : 1);
        foreach (int fileDelta in new[] { -1, 1 })
        {
            if (TrySquare(square.File + fileDelta, pawnSourceRank, out Square source))
            {
                Piece piece = position[source];
                if (piece.Color == attacker && piece.Type == PieceType.Pawn)
                {
                    return true;
                }
            }
        }

        foreach ((int file, int rank) in KNIGHT_OFFSETS)
        {
            if (TrySquare(square.File + file, square.Rank + rank, out Square source))
            {
                Piece piece = position[source];
                if (piece.Color == attacker && piece.Type == PieceType.Knight)
                {
                    return true;
                }
            }
        }

        foreach ((int file, int rank) in KING_OFFSETS)
        {
            if (TrySquare(square.File + file, square.Rank + rank, out Square source))
            {
                Piece piece = position[source];
                if (piece.Color == attacker && piece.Type == PieceType.King)
                {
                    return true;
                }
            }
        }

        if (RayAttacked(position, square, attacker, [(1, 0), (-1, 0), (0, 1), (0, -1)], PieceType.Rook))
        {
            return true;
        }

        return RayAttacked(position, square, attacker, [(1, 1), (1, -1), (-1, 1), (-1, -1)], PieceType.Bishop);
    }

    private IEnumerable<Move> GeneratePseudoLegalMoves(Position position)
    {
        for (int index = 0; index < 64; index++)
        {
            Square from = new(index);
            Piece piece = position[from];
            if (piece.IsEmpty || piece.Color != position.SideToMove)
            {
                continue;
            }

            IEnumerable<Move> moves = piece.Type switch
            {
                PieceType.Pawn => PawnMoves(position, from, piece.Color),
                PieceType.Knight => StepMoves(position, from, piece.Color, KNIGHT_OFFSETS),
                PieceType.Bishop => RayMoves(position, from, piece.Color, [(1, 1), (1, -1), (-1, 1), (-1, -1)]),
                PieceType.Rook => RayMoves(position, from, piece.Color, [(1, 0), (-1, 0), (0, 1), (0, -1)]),
                PieceType.Queen => RayMoves(position, from, piece.Color, [(1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1)]),
                PieceType.King => KingMoves(position, from, piece.Color),
                _ => []
            };

            foreach (Move move in moves)
            {
                yield return move;
            }
        }
    }

    private IEnumerable<Move> PawnMoves(Position position, Square from, PieceColor color)
    {
        int direction = color == PieceColor.White ? 1 : -1;
        int startRank = color == PieceColor.White ? 1 : 6;

        if (TrySquare(from.File, from.Rank + direction, out Square one) && position[one].IsEmpty)
        {
            foreach (Move move in Promote(from, one))
            {
                yield return move;
            }

            if (from.Rank == startRank && TrySquare(from.File, from.Rank + 2 * direction, out Square two) && position[two].IsEmpty)
            {
                yield return new Move(from, two);
            }
        }

        foreach (int fileDelta in new[] { -1, 1 })
        {
            if (!TrySquare(from.File + fileDelta, from.Rank + direction, out Square target))
            {
                continue;
            }

            Piece targetPiece = position[target];
            if ((!targetPiece.IsEmpty && targetPiece.Color != color) || position.EnPassantSquare == target)
            {
                foreach (Move move in Promote(from, target))
                {
                    yield return move;
                }
            }
        }
    }

    private static IEnumerable<Move> Promote(Square from, Square to)
    {
        if (to.Rank is 0 or 7)
        {
            yield return new Move(from, to, PieceType.Queen);
            yield return new Move(from, to, PieceType.Rook);
            yield return new Move(from, to, PieceType.Bishop);
            yield return new Move(from, to, PieceType.Knight);
        }
        else
        {
            yield return new Move(from, to);
        }
    }

    private IEnumerable<Move> KingMoves(Position position, Square from, PieceColor color)
    {
        foreach (Move move in StepMoves(position, from, color, KING_OFFSETS))
        {
            yield return move;
        }

        PieceColor enemy = Position.Opposite(color);
        int rank = color == PieceColor.White ? 0 : 7;
        if (from.Index != rank * 8 + 4 || IsSquareAttacked(position, from, enemy))
        {
            yield break;
        }

        char kingSide = color == PieceColor.White ? 'K' : 'k';
        if (position.CastlingRights.Contains(kingSide)
            && position[new Square(rank * 8 + 5)].IsEmpty
            && position[new Square(rank * 8 + 6)].IsEmpty
            && !IsSquareAttacked(position, new Square(rank * 8 + 5), enemy)
            && !IsSquareAttacked(position, new Square(rank * 8 + 6), enemy))
        {
            yield return new Move(from, new Square(rank * 8 + 6));
        }

        char queenSide = color == PieceColor.White ? 'Q' : 'q';
        if (position.CastlingRights.Contains(queenSide)
            && position[new Square(rank * 8 + 1)].IsEmpty
            && position[new Square(rank * 8 + 2)].IsEmpty
            && position[new Square(rank * 8 + 3)].IsEmpty
            && !IsSquareAttacked(position, new Square(rank * 8 + 3), enemy)
            && !IsSquareAttacked(position, new Square(rank * 8 + 2), enemy))
        {
            yield return new Move(from, new Square(rank * 8 + 2));
        }
    }

    private static IEnumerable<Move> StepMoves(Position position, Square from, PieceColor color, IEnumerable<(int File, int Rank)> offsets)
    {
        foreach ((int file, int rank) in offsets)
        {
            if (TrySquare(from.File + file, from.Rank + rank, out Square target))
            {
                Piece targetPiece = position[target];
                if (targetPiece.IsEmpty || targetPiece.Color != color)
                {
                    yield return new Move(from, target);
                }
            }
        }
    }

    private static IEnumerable<Move> RayMoves(Position position, Square from, PieceColor color, IEnumerable<(int File, int Rank)> directions)
    {
        foreach ((int fileDirection, int rankDirection) in directions)
        {
            int file = from.File + fileDirection;
            int rank = from.Rank + rankDirection;

            while (TrySquare(file, rank, out Square target))
            {
                Piece targetPiece = position[target];
                if (targetPiece.IsEmpty)
                {
                    yield return new Move(from, target);
                }
                else
                {
                    if (targetPiece.Color != color)
                    {
                        yield return new Move(from, target);
                    }

                    break;
                }

                file += fileDirection;
                rank += rankDirection;
            }
        }
    }

    private static bool RayAttacked(Position position, Square square, PieceColor attacker, IEnumerable<(int File, int Rank)> directions, PieceType slider)
    {
        foreach ((int fileDirection, int rankDirection) in directions)
        {
            int file = square.File + fileDirection;
            int rank = square.Rank + rankDirection;

            while (TrySquare(file, rank, out Square target))
            {
                Piece piece = position[target];
                if (!piece.IsEmpty)
                {
                    if (piece.Color == attacker && (piece.Type == slider || piece.Type == PieceType.Queen))
                    {
                        return true;
                    }

                    break;
                }

                file += fileDirection;
                rank += rankDirection;
            }
        }

        return false;
    }

    private static bool TrySquare(int file, int rank, out Square square)
    {
        if (file is >= 0 and < 8 && rank is >= 0 and < 8)
        {
            square = new Square(rank * 8 + file);
            return true;
        }

        square = default;
        return false;
    }
}
