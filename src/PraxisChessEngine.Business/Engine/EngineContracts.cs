using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Search;

namespace PraxisChessEngine.Business.Engine;

public interface IOpeningBook
{
    Move? FindMove(Position position);
}

public enum TablebaseProbeStatus
{
    Success,
    NotApplicable,
    Unavailable,
    InvalidResponse
}

public sealed record TablebaseProbeResult(TablebaseProbeStatus Status, Move? BestMove = null);

public interface ITablebaseProvider
{
    int MaximumPieceCount { get; }

    Task<TablebaseProbeResult> ProbeAsync(Position position, CancellationToken cancellationToken);
}

public interface IEngineSession
{
    Position Position { get; }

    void NewGame();

    void SetPosition(string? fen, IEnumerable<Move> moves);

    Task<SearchResult> SearchAsync(SearchLimits limits, CancellationToken cancellationToken);
}
