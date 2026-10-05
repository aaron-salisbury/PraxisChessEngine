using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Diagnostics;
using PraxisChessEngine.Business.Search;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PraxisChessEngine.Business.Engine;

internal sealed class EngineSession : IEngineSession
{
    private static readonly TimeSpan TABLEBASE_TIMEOUT = TimeSpan.FromMilliseconds(500);

    private readonly ISearchService _searchService;
    private readonly MoveGenerator _moveGenerator;
    private readonly IOpeningBook _openingBook;
    private readonly IEnumerable<ITablebaseProvider> _tablebases;
    private readonly IDiagnosticLogger _logger;

    public EngineSession(ISearchService searchService, MoveGenerator moveGenerator, IOpeningBook openingBook, IEnumerable<ITablebaseProvider> tablebases, IDiagnosticLogger logger)
    {
        _searchService = searchService;
        _moveGenerator = moveGenerator;
        _openingBook = openingBook;
        _tablebases = tablebases;
        _logger = logger;
        Position = Position.FromFen(Position.START_FEN);
    }

    public Position Position { get; private set; }

    public void NewGame()
    {
        Position = Position.FromFen(Position.START_FEN);
    }

    public void SetPosition(string? fen, IEnumerable<Move> moves)
    {
        Position position = Position.FromFen(fen ?? Position.START_FEN);

        foreach (Move move in moves)
        {
            if (!_moveGenerator.GenerateLegalMoves(position).Contains(move))
            {
                throw new InvalidOperationException($"Illegal move '{move}'.");
            }

            position.MakeMove(move);
        }

        Position = position;
    }

    public async Task<SearchResult> SearchAsync(SearchLimits limits, CancellationToken cancellationToken)
    {
        Position snapshot = Position.Clone();
        IReadOnlyList<Move> legalMoves = _moveGenerator.GenerateLegalMoves(snapshot);
        if (legalMoves.Count == 0)
        {
            return new SearchResult(null, 0, 0, 0, []);
        }

        if (legalMoves.Count == 1)
        {
            Move forcedMove = legalMoves[0];
            _logger.Log($"Forced move: {forcedMove}");
            return new SearchResult(forcedMove, 0, 0, 0, [forcedMove]);
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        using CancellationTokenSource? deadlineCancellation = CreateDeadlineCancellation(limits.MoveTime, cancellationToken);
        CancellationToken effectiveCancellation = deadlineCancellation?.Token ?? cancellationToken;

        Move? bookMove = _openingBook.FindMove(snapshot);
        if (bookMove.HasValue && legalMoves.Contains(bookMove.Value))
        {
            _logger.Log($"Opening book move: {bookMove.Value}");
            return new SearchResult(bookMove, 0, 0, 0, [bookMove.Value]);
        }

        foreach (ITablebaseProvider provider in _tablebases.OrderByDescending(provider => provider.MaximumPieceCount))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (snapshot.PieceCount > provider.MaximumPieceCount)
            {
                continue;
            }

            TimeSpan? remaining = GetRemainingTime(limits.MoveTime, stopwatch.Elapsed);
            if (remaining <= TimeSpan.Zero)
            {
                return DeadlineFallback(legalMoves);
            }

            TimeSpan probeTimeout = remaining.HasValue && remaining.Value < TABLEBASE_TIMEOUT
                ? remaining.Value
                : TABLEBASE_TIMEOUT;

            using CancellationTokenSource probeCancellation = CancellationTokenSource.CreateLinkedTokenSource(effectiveCancellation);
            probeCancellation.CancelAfter(probeTimeout);

            string providerName = provider.GetType().Name;
            _logger.Log($"Tablebase probe started: {providerName}; timeout {probeTimeout.TotalMilliseconds:0} ms");

            try
            {
                TablebaseProbeResult result = await provider.ProbeAsync(snapshot, probeCancellation.Token);
                _logger.Log($"Tablebase probe completed: {providerName}; status {result.Status}");

                if (result.Status == TablebaseProbeStatus.Success
                    && result.BestMove.HasValue
                    && legalMoves.Contains(result.BestMove.Value))
                {
                    _logger.Log($"Tablebase move: {result.BestMove.Value}");
                    return new SearchResult(result.BestMove, 0, 0, 0, [result.BestMove.Value]);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (effectiveCancellation.IsCancellationRequested)
                {
                    _logger.Log($"Move deadline reached during tablebase probe: {providerName}");
                    return DeadlineFallback(legalMoves);
                }

                _logger.Log($"Tablebase probe timed out: {providerName}; falling back");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        TimeSpan? searchTime = GetRemainingTime(limits.MoveTime, stopwatch.Elapsed);
        if (searchTime <= TimeSpan.Zero)
        {
            return DeadlineFallback(legalMoves);
        }

        SearchLimits searchLimits = new(limits.Depth, searchTime, limits.Nodes);
        SearchResult result = await Task.Run(() => _searchService.Search(snapshot, searchLimits, effectiveCancellation), CancellationToken.None);
        if (result.BestMove.HasValue && legalMoves.Contains(result.BestMove.Value))
        {
            return result;
        }

        Move fallback = legalMoves[0];
        _logger.Log($"Search completed without a legal best move; returning legal fallback {fallback}");
        return new SearchResult(fallback, result.Score, result.Depth, result.Nodes, [fallback]);
    }

    private SearchResult DeadlineFallback(IReadOnlyList<Move> legalMoves)
    {
        Move move = legalMoves[0];
        _logger.Log($"Move deadline exhausted before search completed; returning legal fallback {move}");
        return new SearchResult(move, 0, 0, 0, [move]);
    }

    private static CancellationTokenSource? CreateDeadlineCancellation(TimeSpan? moveTime, CancellationToken cancellationToken)
    {
        if (!moveTime.HasValue)
        {
            return null;
        }

        CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cancellation.CancelAfter(moveTime.Value);
        return cancellation;
    }

    private static TimeSpan? GetRemainingTime(TimeSpan? moveTime, TimeSpan elapsed)
    {
        if (!moveTime.HasValue)
        {
            return null;
        }

        return moveTime.Value - elapsed;
    }
}
