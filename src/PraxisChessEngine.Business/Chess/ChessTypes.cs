namespace PraxisChessEngine.Business.Chess;

public enum PieceColor
{
    White,
    Black
}

public enum PieceType
{
    None,
    Pawn,
    Knight,
    Bishop,
    Rook,
    Queen,
    King
}

public readonly record struct Piece(PieceType Type, PieceColor Color)
{
    public bool IsEmpty => Type == PieceType.None;

    public static Piece Empty => new(PieceType.None, PieceColor.White);
}

public readonly record struct Square(int Index)
{
    public int File => Index % 8;
    public int Rank => Index / 8;

    public static Square Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length != 2 || value[0] < 'a' || value[0] > 'h' || value[1] < '1' || value[1] > '8')
        {
            throw new FormatException($"Invalid square '{value}'.");
        }

        return new Square((value[1] - '1') * 8 + value[0] - 'a');
    }

    public override string ToString()
    {
        return $"{(char)('a' + File)}{(char)('1' + Rank)}";
    }
}

public readonly record struct Move(Square From, Square To, PieceType Promotion = PieceType.None)
{
    public override string ToString()
    {
        string promotion = Promotion switch
        {
            PieceType.Queen => "q",
            PieceType.Rook => "r",
            PieceType.Bishop => "b",
            PieceType.Knight => "n",
            _ => string.Empty
        };

        return $"{From}{To}{promotion}";
    }

    public static Move Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (value.Length is < 4 or > 5)
        {
            throw new FormatException($"Invalid UCI move '{value}'.");
        }

        PieceType promotion = value.Length == 5 ? value[4] switch
        {
            'q' => PieceType.Queen,
            'r' => PieceType.Rook,
            'b' => PieceType.Bishop,
            'n' => PieceType.Knight,
            _ => throw new FormatException($"Invalid promotion piece '{value[4]}'.")
        } : PieceType.None;

        return new Move(Square.Parse(value[..2]), Square.Parse(value.Substring(2, 2)), promotion);
    }
}
