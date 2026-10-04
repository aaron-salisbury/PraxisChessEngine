using System.Text;

namespace PraxisChessEngine.Business.Chess;

public sealed class Position
{
    public const string START_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    private readonly Piece[] _board;

    public Position()
    {
        _board = new Piece[64];
        SideToMove = PieceColor.White;
        CastlingRights = "KQkq";
        EnPassantSquare = null;
        FullmoveNumber = 1;
    }

    private Position(Position source)
    {
        _board = (Piece[])source._board.Clone();
        SideToMove = source.SideToMove;
        CastlingRights = source.CastlingRights;
        EnPassantSquare = source.EnPassantSquare;
        HalfmoveClock = source.HalfmoveClock;
        FullmoveNumber = source.FullmoveNumber;
    }

    public PieceColor SideToMove { get; private set; }

    public string CastlingRights { get; private set; }

    public Square? EnPassantSquare { get; private set; }

    public int HalfmoveClock { get; private set; }

    public int FullmoveNumber { get; private set; }

    public int PieceCount => _board.Count(piece => !piece.IsEmpty);

    public Piece this[Square square] => _board[square.Index];

    public Position Clone()
    {
        return new Position(this);
    }

    public static Position FromFen(string fen)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fen);

        string[] fields = fen.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 6)
        {
            throw new FormatException("FEN must contain six fields.");
        }

        Position position = new();
        Array.Fill(position._board, Piece.Empty);

        string[] ranks = fields[0].Split('/');
        if (ranks.Length != 8)
        {
            throw new FormatException("FEN piece placement must contain eight ranks.");
        }

        for (int fenRank = 0; fenRank < 8; fenRank++)
        {
            int file = 0;
            foreach (char token in ranks[fenRank])
            {
                if (char.IsDigit(token))
                {
                    file += token - '0';
                    continue;
                }

                if (file >= 8)
                {
                    throw new FormatException("Invalid FEN rank.");
                }

                PieceColor color = char.IsUpper(token) ? PieceColor.White : PieceColor.Black;
                PieceType type = char.ToLowerInvariant(token) switch
                {
                    'p' => PieceType.Pawn,
                    'n' => PieceType.Knight,
                    'b' => PieceType.Bishop,
                    'r' => PieceType.Rook,
                    'q' => PieceType.Queen,
                    'k' => PieceType.King,
                    _ => throw new FormatException($"Invalid FEN piece '{token}'.")
                };

                int rank = 7 - fenRank;
                position._board[rank * 8 + file] = new Piece(type, color);
                file++;
            }

            if (file != 8)
            {
                throw new FormatException("Each FEN rank must describe eight squares.");
            }
        }

        position.SideToMove = fields[1] switch
        {
            "w" => PieceColor.White,
            "b" => PieceColor.Black,
            _ => throw new FormatException("Invalid FEN active color.")
        };
        position.CastlingRights = fields[2] == "-" ? string.Empty : fields[2];
        position.EnPassantSquare = fields[3] == "-" ? null : Square.Parse(fields[3]);
        position.HalfmoveClock = int.Parse(fields[4]);
        position.FullmoveNumber = int.Parse(fields[5]);

        return position;
    }

    public string ToFen()
    {
        StringBuilder placement = new();

        for (int rank = 7; rank >= 0; rank--)
        {
            int empty = 0;
            for (int file = 0; file < 8; file++)
            {
                Piece piece = _board[rank * 8 + file];
                if (piece.IsEmpty)
                {
                    empty++;
                    continue;
                }

                if (empty > 0)
                {
                    placement.Append(empty);
                    empty = 0;
                }

                char symbol = piece.Type switch
                {
                    PieceType.Pawn => 'p',
                    PieceType.Knight => 'n',
                    PieceType.Bishop => 'b',
                    PieceType.Rook => 'r',
                    PieceType.Queen => 'q',
                    PieceType.King => 'k',
                    _ => throw new InvalidOperationException()
                };
                placement.Append(piece.Color == PieceColor.White ? char.ToUpperInvariant(symbol) : symbol);
            }

            if (empty > 0)
            {
                placement.Append(empty);
            }

            if (rank > 0)
            {
                placement.Append('/');
            }
        }

        string castling = string.IsNullOrEmpty(CastlingRights) ? "-" : CastlingRights;
        string enPassant = EnPassantSquare?.ToString() ?? "-";
        return $"{placement} {(SideToMove == PieceColor.White ? "w" : "b")} {castling} {enPassant} {HalfmoveClock} {FullmoveNumber}";
    }

    public void MakeMove(Move move)
    {
        Piece moving = _board[move.From.Index];
        if (moving.IsEmpty || moving.Color != SideToMove)
        {
            throw new InvalidOperationException("Move does not start from a piece belonging to the side to move.");
        }

        Piece captured = _board[move.To.Index];
        bool pawnMove = moving.Type == PieceType.Pawn;

        if (pawnMove && EnPassantSquare == move.To && captured.IsEmpty && move.From.File != move.To.File)
        {
            int capturedIndex = move.To.Index + (moving.Color == PieceColor.White ? -8 : 8);
            _board[capturedIndex] = Piece.Empty;
            captured = new Piece(PieceType.Pawn, Opposite(moving.Color));
        }

        _board[move.To.Index] = move.Promotion == PieceType.None ? moving : new Piece(move.Promotion, moving.Color);
        _board[move.From.Index] = Piece.Empty;

        if (moving.Type == PieceType.King && Math.Abs(move.To.File - move.From.File) == 2)
        {
            int rank = move.From.Rank;
            if (move.To.File == 6)
            {
                _board[rank * 8 + 5] = _board[rank * 8 + 7];
                _board[rank * 8 + 7] = Piece.Empty;
            }
            else
            {
                _board[rank * 8 + 3] = _board[rank * 8];
                _board[rank * 8] = Piece.Empty;
            }
        }

        UpdateCastlingRights(move, moving, captured);

        EnPassantSquare = null;
        if (pawnMove && Math.Abs(move.To.Rank - move.From.Rank) == 2)
        {
            EnPassantSquare = new Square((move.From.Index + move.To.Index) / 2);
        }

        HalfmoveClock = pawnMove || !captured.IsEmpty ? 0 : HalfmoveClock + 1;
        if (SideToMove == PieceColor.Black)
        {
            FullmoveNumber++;
        }

        SideToMove = Opposite(SideToMove);
    }

    private void UpdateCastlingRights(Move move, Piece moving, Piece captured)
    {
        if (moving.Type == PieceType.King)
        {
            CastlingRights = moving.Color == PieceColor.White
                ? CastlingRights.Replace("K", string.Empty).Replace("Q", string.Empty)
                : CastlingRights.Replace("k", string.Empty).Replace("q", string.Empty);
        }

        if (moving.Type == PieceType.Rook)
        {
            RemoveRookRight(move.From);
        }

        if (captured.Type == PieceType.Rook)
        {
            RemoveRookRight(move.To);
        }
    }

    private void RemoveRookRight(Square square)
    {
        CastlingRights = square.Index switch
        {
            0 => CastlingRights.Replace("Q", string.Empty),
            7 => CastlingRights.Replace("K", string.Empty),
            56 => CastlingRights.Replace("q", string.Empty),
            63 => CastlingRights.Replace("k", string.Empty),
            _ => CastlingRights
        };
    }

    internal static PieceColor Opposite(PieceColor color)
    {
        return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
    }
}
