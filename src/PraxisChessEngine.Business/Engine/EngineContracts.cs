using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Search;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PraxisChessEngine.Business.Engine;

/// <summary>
/// An opening-book store used to eliminate the need to calculate the best lines during approximately the
/// first ten moves of the game, where the positions are extremely open-ended and thus computationally
/// expensive to evaluate.
/// </summary>
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
