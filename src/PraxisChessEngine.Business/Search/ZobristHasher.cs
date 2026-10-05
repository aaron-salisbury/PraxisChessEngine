using PraxisChessEngine.Business.Chess;

namespace PraxisChessEngine.Business.Search;

internal sealed class ZobristHasher
{
    private static readonly ulong[,] PIECES = CreateValues();
    private static readonly ulong SIDE = 0x9E3779B97F4A7C15UL;

    internal ulong Hash(Position position)
    {
        ulong hash = 0;

        for (int index = 0; index < 64; index++)
        {
            Piece piece = position[new Square(index)];
            if (!piece.IsEmpty)
            {
                int pieceIndex = ((int)piece.Color * 6) + (int)piece.Type - 1;
                hash ^= PIECES[pieceIndex, index];
            }
        }

        if (position.SideToMove == PieceColor.Black)
        {
            hash ^= SIDE;
        }

        foreach (char right in position.CastlingRights)
        {
            hash ^= Mix((ulong)right * 0x100000001B3UL);
        }

        if (position.EnPassantSquare is Square square)
        {
            hash ^= Mix((ulong)(square.Index + 1) * 0x517CC1B727220A95UL);
        }

        return hash;
    }

    private static ulong[,] CreateValues()
    {
        ulong[,] values = new ulong[12, 64];
        ulong state = 0xD1B54A32D192ED03UL;

        for (int piece = 0; piece < 12; piece++)
        {
            for (int square = 0; square < 64; square++)
            {
                state += 0x9E3779B97F4A7C15UL;
                values[piece, square] = Mix(state);
            }
        }

        return values;
    }

    private static ulong Mix(ulong value)
    {
        value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
        value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
        return value ^ (value >> 31);
    }
}
