using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Search;

namespace PraxisChessEngine.Business.Engine;

internal sealed class EngineSession : IEngineSession
{
    private readonly ISearchService _searchService;
    private readonly MoveGenerator _moveGenerator;
    private readonly IOpeningBook _openingBook;
    private readonly IEnumerable<ITablebaseProvider> _tablebases;

    public EngineSession(ISearchService searchService, MoveGenerator moveGenerator, IOpeningBook openingBook, IEnumerable<ITablebaseProvider> tablebases)
    {
        _searchService = searchService;
        _moveGenerator = moveGenerator;
        _openingBook = openingBook;
        _tablebases = tablebases;
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
        IReadOnlyList<Move> legalMoves = _moveGenerator.GenerateLegalMoves(Position);

        Move? bookMove = _openingBook.FindMove(Position);
        if (bookMove.HasValue && legalMoves.Contains(bookMove.Value))
        {
            return new SearchResult(bookMove, 0, 0, 0, [bookMove.Value]);
        }

        foreach (ITablebaseProvider provider in _tablebases.OrderByDescending(provider => provider.MaximumPieceCount))
        {
            if (Position.PieceCount > provider.MaximumPieceCount)
            {
                continue;
            }

            TablebaseProbeResult result = await provider.ProbeAsync(Position, cancellationToken);
            if (result.Status == TablebaseProbeStatus.Success
                && result.BestMove.HasValue
                && legalMoves.Contains(result.BestMove.Value))
            {
                return new SearchResult(result.BestMove, 0, 0, 0, [result.BestMove.Value]);
            }
        }

        Position snapshot = Position.Clone();
        return await Task.Run(() => _searchService.Search(snapshot, limits, cancellationToken), cancellationToken);
    }
}
