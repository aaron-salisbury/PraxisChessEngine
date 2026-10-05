using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Diagnostics;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Business.Protocols;
using PraxisChessEngine.Business.Search;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PraxisChessEngine.Integrations.Protocols;

internal sealed class UciProtocol : IEngineProtocol
{
    private readonly IEngineSession _session;
    private readonly EngineIdentity _identity;
    private readonly IDiagnosticLogger _logger;
    private CancellationTokenSource? _searchCancellation;
    private Task? _searchTask;
    private bool _debugEnabled;

    public UciProtocol(IEngineSession session, EngineIdentity identity, IDiagnosticLogger logger)
    {
        _session = session;
        _identity = identity;
        _logger = logger;
    }

    public async Task RunAsync(TextReader input, TextWriter output, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            string? line = await input.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                break;
            }

            string command = line.Trim();
            _logger.Log($"GUI -> {command}");
            if (command == "uci")
            {
                await WriteAsync(output, $"id name {_identity.DisplayName}");
                await WriteAsync(output, $"id author {_identity.Author}");
                await WriteAsync(output, "uciok");
            }
            else if (command.StartsWith("debug ", StringComparison.Ordinal))
            {
                _debugEnabled = command == "debug on";
                _logger.Log($"UCI debug {(_debugEnabled ? "enabled" : "disabled")}");
            }
            else if (command.StartsWith("setoption ", StringComparison.Ordinal))
            {
                if (_debugEnabled)
                {
                    _logger.Log($"Ignored unsupported UCI option: {command}");
                }
            }
            else if (command == "isready")
            {
                await WriteAsync(output, "readyok");
            }
            else if (command == "ucinewgame")
            {
                await StopSearchAsync();
                _session.NewGame();
            }
            else if (command.StartsWith("position ", StringComparison.Ordinal))
            {
                await StopSearchAsync();
                HandlePosition(command);
            }
            else if (command.StartsWith("go", StringComparison.Ordinal))
            {
                await StopSearchAsync();
                StartSearch(command, output, cancellationToken);
            }
            else if (command == "stop")
            {
                await StopSearchAsync();
            }
            else if (command == "quit")
            {
                await StopSearchAsync();
                break;
            }
        }
    }

    private void HandlePosition(string command)
    {
        string payload = command["position ".Length..];
        string? fen = null;
        string moveText = string.Empty;

        if (payload.StartsWith("startpos", StringComparison.Ordinal))
        {
            payload = payload["startpos".Length..].Trim();
        }
        else if (payload.StartsWith("fen ", StringComparison.Ordinal))
        {
            string fenAndMoves = payload["fen ".Length..];
            int movesIndex = fenAndMoves.IndexOf(" moves ", StringComparison.Ordinal);
            if (movesIndex >= 0)
            {
                fen = fenAndMoves[..movesIndex];
                moveText = fenAndMoves[(movesIndex + " moves ".Length)..];
            }
            else
            {
                fen = fenAndMoves;
            }

            payload = string.Empty;
        }

        if (payload.StartsWith("moves ", StringComparison.Ordinal))
        {
            moveText = payload["moves ".Length..];
        }

        IEnumerable<Move> moves = string.IsNullOrWhiteSpace(moveText)
            ? []
            : moveText.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(Move.Parse);

        _session.SetPosition(fen, moves);
        _logger.Log($"Position FEN: {_session.Position.ToFen()}");
    }

    private void StartSearch(string command, TextWriter output, CancellationToken applicationCancellation)
    {
        SearchLimits limits = ParseLimits(command);
        _searchCancellation = CancellationTokenSource.CreateLinkedTokenSource(applicationCancellation);
        CancellationToken searchToken = _searchCancellation.Token;

        _searchTask = Task.Run(async () =>
        {
            try
            {
                string searchFen = _session.Position.ToFen();
                _logger.Log($"Search FEN: {searchFen}");
                SearchResult result = await _session.SearchAsync(limits, searchToken);
                Move? bestMove = result.BestMove;
                IReadOnlyList<Move> legalMoves = new MoveGenerator().GenerateLegalMoves(Position.FromFen(searchFen));

                if (bestMove.HasValue && !legalMoves.Contains(bestMove.Value))
                {
                    _logger.Log($"Illegal search result '{bestMove.Value}'. FEN: {searchFen}. Legal moves: {string.Join(' ', legalMoves)}");
                    return;
                }

                string bestMoveText = bestMove?.ToString() ?? "0000";
                _logger.Log($"Search result: bestmove {bestMoveText}; depth {result.Depth}; score {result.Score}; nodes {result.Nodes}");
                await WriteSearchInfoAsync(output, result);
                await WriteAsync(output, $"bestmove {bestMoveText}");
            }
            catch (OperationCanceledException)
            {
            }
        }, CancellationToken.None);
    }

    private async Task WriteSearchInfoAsync(TextWriter output, SearchResult result)
    {
        if (result.Depth <= 0 && result.Nodes <= 0)
        {
            return;
        }

        string principalVariation = result.PrincipalVariation.Count == 0
            ? string.Empty
            : $" pv {string.Join(' ', result.PrincipalVariation)}";

        await WriteAsync(output, $"info depth {result.Depth} score cp {result.Score} nodes {result.Nodes}{principalVariation}");
    }

    private SearchLimits ParseLimits(string command)
    {
        string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int? depth = null;
        TimeSpan? moveTime = null;
        long? nodes = null;
        int? whiteTime = null;
        int? blackTime = null;
        int whiteIncrement = 0;
        int blackIncrement = 0;
        int movesToGo = 30;

        for (int i = 1; i + 1 < parts.Length; i++)
        {
            string name = parts[i];
            string value = parts[i + 1];

            if (name == "depth" && int.TryParse(value, out int parsedDepth))
            {
                depth = parsedDepth;
                i++;
            }
            else if (name == "movetime" && int.TryParse(value, out int milliseconds))
            {
                moveTime = TimeSpan.FromMilliseconds(Math.Max(1, milliseconds));
                i++;
            }
            else if (name == "nodes" && long.TryParse(value, out long parsedNodes))
            {
                nodes = parsedNodes;
                i++;
            }
            else if (name == "wtime" && int.TryParse(value, out int parsedWhiteTime))
            {
                whiteTime = parsedWhiteTime;
                i++;
            }
            else if (name == "btime" && int.TryParse(value, out int parsedBlackTime))
            {
                blackTime = parsedBlackTime;
                i++;
            }
            else if (name == "winc" && int.TryParse(value, out int parsedWhiteIncrement))
            {
                whiteIncrement = parsedWhiteIncrement;
                i++;
            }
            else if (name == "binc" && int.TryParse(value, out int parsedBlackIncrement))
            {
                blackIncrement = parsedBlackIncrement;
                i++;
            }
            else if (name == "movestogo" && int.TryParse(value, out int parsedMovesToGo))
            {
                movesToGo = Math.Max(1, parsedMovesToGo);
                i++;
            }
        }

        if (!moveTime.HasValue)
        {
            int? remaining = _session.Position.SideToMove == PieceColor.White ? whiteTime : blackTime;
            int increment = _session.Position.SideToMove == PieceColor.White ? whiteIncrement : blackIncrement;

            if (remaining.HasValue)
            {
                int budget = Math.Max(1, remaining.Value / movesToGo + increment / 2);
                int reserve = Math.Max(10, remaining.Value / 50);
                moveTime = TimeSpan.FromMilliseconds(Math.Min(budget, Math.Max(1, remaining.Value - reserve)));
            }
        }

        if (!depth.HasValue && !moveTime.HasValue && !nodes.HasValue)
        {
            depth = 5;
        }

        return new SearchLimits(depth, moveTime, nodes);
    }

    private async Task WriteAsync(TextWriter output, string message)
    {
        _logger.Log($"Praxis -> {message}");
        await output.WriteLineAsync(message);
    }

    private async Task StopSearchAsync()
    {
        if (_searchCancellation is null)
        {
            return;
        }

        await _searchCancellation.CancelAsync();

        if (_searchTask is not null)
        {
            await _searchTask;
        }

        _searchCancellation.Dispose();
        _searchCancellation = null;
        _searchTask = null;
    }
}

internal sealed class UciProtocolFactory : IEngineProtocolFactory
{
    public IEngineProtocol Create(IEngineSession session, EngineIdentity identity, IDiagnosticLogger logger)
    {
        return new UciProtocol(session, identity, logger);
    }
}
