using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using PraxisChessEngine.Business.Chess;

namespace PraxisChessEngine.Business.Search;

internal sealed class AlphaBetaSearchService : ISearchService
{
    private const int MATE_SCORE = 100000;

    private readonly MoveGenerator _moveGenerator;
    private readonly PositionEvaluator _evaluator;
    private readonly ZobristHasher _hasher;
    private readonly Dictionary<ulong, TranspositionEntry> _transpositions = [];
    private readonly Dictionary<Move, int> _history = [];
    private readonly Move?[,] _killers = new Move?[128, 2];

    private long _nodes;
    private Stopwatch? _stopwatch;
    private SearchLimits? _limits;
    private CancellationToken _cancellationToken;

    public AlphaBetaSearchService(MoveGenerator moveGenerator, PositionEvaluator evaluator, ZobristHasher hasher)
    {
        _moveGenerator = moveGenerator;
        _evaluator = evaluator;
        _hasher = hasher;
    }

    public SearchResult Search(Position position, SearchLimits limits, CancellationToken cancellationToken)
    {
        int maxDepth = limits.Depth ?? 64;
        _nodes = 0;
        _limits = limits;
        _cancellationToken = cancellationToken;
        _stopwatch = Stopwatch.StartNew();
        _transpositions.Clear();
        _history.Clear();
        Array.Clear(_killers);

        Move? bestMove = null;
        int bestScore = 0;
        int completedDepth = 0;
        List<Move> bestPv = [];

        for (int depth = 1; depth <= maxDepth; depth++)
        {
            try
            {
                List<Move> pv = [];
                int score = Negamax(position, depth, -MATE_SCORE, MATE_SCORE, 0, pv);
                if (pv.Count > 0)
                {
                    bestMove = pv[0];
                    bestPv = pv;
                }

                bestScore = score;
                completedDepth = depth;
            }
            catch (SearchStoppedException)
            {
                break;
            }

            if (limits.Depth.HasValue && depth >= limits.Depth.Value)
            {
                break;
            }
        }

        return new SearchResult(bestMove, bestScore, completedDepth, _nodes, bestPv);
    }

    private int Negamax(Position position, int depth, int alpha, int beta, int ply, List<Move> principalVariation)
    {
        CheckStop();
        _nodes++;

        IReadOnlyList<Move> legalMoves = _moveGenerator.GenerateLegalMoves(position);
        if (legalMoves.Count == 0)
        {
            return _moveGenerator.IsInCheck(position, position.SideToMove) ? -MATE_SCORE + ply : 0;
        }

        if (position.HalfmoveClock >= 100)
        {
            return 0;
        }

        if (depth <= 0)
        {
            return Quiescence(position, alpha, beta);
        }

        ulong hash = _hasher.Hash(position);
        if (_transpositions.TryGetValue(hash, out TranspositionEntry entry) && entry.Depth >= depth)
        {
            return entry.Score;
        }

        int best = -MATE_SCORE;
        Move? bestMove = null;
        List<Move> ordered = OrderMoves(position, legalMoves, entry?.BestMove, ply);

        foreach (Move move in ordered)
        {
            Position child = position.Clone();
            child.MakeMove(move);
            List<Move> childPv = [];
            int score = -Negamax(child, depth - 1, -beta, -alpha, ply + 1, childPv);

            if (score > best)
            {
                best = score;
                bestMove = move;
                principalVariation.Clear();
                principalVariation.Add(move);
                principalVariation.AddRange(childPv);
            }

            alpha = Math.Max(alpha, score);
            if (alpha >= beta)
            {
                if (position[move.To].IsEmpty && move.Promotion == PieceType.None && ply < _killers.GetLength(0))
                {
                    if (_killers[ply, 0] != move)
                    {
                        _killers[ply, 1] = _killers[ply, 0];
                        _killers[ply, 0] = move;
                    }

                    _history[move] = _history.GetValueOrDefault(move) + depth * depth;
                }

                break;
            }
        }

        _transpositions[hash] = new TranspositionEntry(depth, best, bestMove);
        return best;
    }

    private int Quiescence(Position position, int alpha, int beta)
    {
        CheckStop();
        int standPat = _evaluator.Evaluate(position);
        if (standPat >= beta)
        {
            return beta;
        }

        alpha = Math.Max(alpha, standPat);

        foreach (Move move in _moveGenerator.GenerateLegalMoves(position))
        {
            if (position[move.To].IsEmpty && position.EnPassantSquare != move.To && move.Promotion == PieceType.None)
            {
                continue;
            }

            Position child = position.Clone();
            child.MakeMove(move);
            _nodes++;
            int score = -Quiescence(child, -beta, -alpha);
            if (score >= beta)
            {
                return beta;
            }

            alpha = Math.Max(alpha, score);
        }

        return alpha;
    }

    private List<Move> OrderMoves(Position position, IReadOnlyList<Move> moves, Move? preferred, int ply)
    {
        Move? firstKiller = ply < _killers.GetLength(0) ? _killers[ply, 0] : null;
        Move? secondKiller = ply < _killers.GetLength(0) ? _killers[ply, 1] : null;

        return moves
            .OrderByDescending(move => preferred.HasValue && move == preferred.Value)
            .ThenByDescending(move => !position[move.To].IsEmpty)
            .ThenByDescending(move => move.Promotion != PieceType.None)
            .ThenByDescending(move => firstKiller.HasValue && move == firstKiller.Value)
            .ThenByDescending(move => secondKiller.HasValue && move == secondKiller.Value)
            .ThenByDescending(move => _history.GetValueOrDefault(move))
            .ToList();
    }

    private void CheckStop()
    {
        if (_cancellationToken.IsCancellationRequested
            || (_limits?.Nodes is long nodes && _nodes >= nodes)
            || (_limits?.MoveTime is TimeSpan time && _stopwatch?.Elapsed >= time))
        {
            throw new SearchStoppedException();
        }
    }

    private sealed record TranspositionEntry(int Depth, int Score, Move? BestMove);

    private sealed class SearchStoppedException : Exception;
}
