using PraxisChessEngine.Business.Chess;
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
    private CancellationTokenSource? _searchCancellation;
    private Task? _searchTask;

    public UciProtocol(IEngineSession session, EngineIdentity identity)
    {
        _session = session;
        _identity = identity;
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
            if (command == "uci")
            {
                await output.WriteLineAsync($"id name {_identity.DisplayName}");
                await output.WriteLineAsync($"id author {_identity.Author}");
                await output.WriteLineAsync("uciok");
            }
            else if (command == "isready")
            {
                await output.WriteLineAsync("readyok");
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
                SearchResult result = await _session.SearchAsync(limits, searchToken);
                string bestMove = result.BestMove?.ToString() ?? "0000";
                await output.WriteLineAsync($"bestmove {bestMove}");
            }
            catch (OperationCanceledException)
            {
            }
        }, CancellationToken.None);
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
    public IEngineProtocol Create(IEngineSession session, EngineIdentity identity)
    {
        return new UciProtocol(session, identity);
    }
}
