using PraxisChessEngine.Business.Protocols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Engine;
using PraxisChessEngine.Business.Search;

namespace PraxisChessEngine.Integrations.Protocols;

internal sealed class UciProtocol : IEngineProtocol
{
    private readonly IEngineSession _session;
    private CancellationTokenSource? _searchCancellation;
    private Task? _searchTask;

    public UciProtocol(IEngineSession session)
    {
        _session = session;
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
                await output.WriteLineAsync("id name Praxis Chess Engine");
                await output.WriteLineAsync("id author Aaron Salisbury");
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

    private static SearchLimits ParseLimits(string command)
    {
        string[] parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int? depth = null;
        TimeSpan? moveTime = null;
        long? nodes = null;

        for (int i = 1; i + 1 < parts.Length; i++)
        {
            if (parts[i] == "depth" && int.TryParse(parts[++i], out int parsedDepth))
            {
                depth = parsedDepth;
            }
            else if (parts[i] == "movetime" && int.TryParse(parts[++i], out int milliseconds))
            {
                moveTime = TimeSpan.FromMilliseconds(milliseconds);
            }
            else if (parts[i] == "nodes" && long.TryParse(parts[++i], out long parsedNodes))
            {
                nodes = parsedNodes;
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
    public IEngineProtocol Create(IEngineSession session)
    {
        return new UciProtocol(session);
    }
}
