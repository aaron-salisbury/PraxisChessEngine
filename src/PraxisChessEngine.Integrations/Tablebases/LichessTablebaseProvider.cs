using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Engine;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace PraxisChessEngine.Integrations.Tablebases;

internal sealed class LichessTablebaseProvider : ITablebaseProvider
{
    private const string CLIENT_NAME = "LichessTablebase";

    private readonly IHttpClientFactory _httpClientFactory;

    public LichessTablebaseProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public int MaximumPieceCount => 7;

    public async Task<TablebaseProbeResult> ProbeAsync(Position position, CancellationToken cancellationToken)
    {
        if (position.PieceCount > MaximumPieceCount)
        {
            return new TablebaseProbeResult(TablebaseProbeStatus.NotApplicable);
        }

        try
        {
            HttpClient client = _httpClientFactory.CreateClient(CLIENT_NAME);
            string fen = Uri.EscapeDataString(position.ToFen());
            TablebaseResponse? response = await client.GetFromJsonAsync<TablebaseResponse>($"standard?fen={fen}", cancellationToken);

            if (response?.Moves is null || response.Moves.Count == 0 || string.IsNullOrWhiteSpace(response.Moves[0].Uci))
            {
                return new TablebaseProbeResult(TablebaseProbeStatus.InvalidResponse);
            }

            return new TablebaseProbeResult(TablebaseProbeStatus.Success, Move.Parse(response.Moves[0].Uci));
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new TablebaseProbeResult(TablebaseProbeStatus.Unavailable);
        }
        catch (HttpRequestException)
        {
            return new TablebaseProbeResult(TablebaseProbeStatus.Unavailable);
        }
        catch (FormatException)
        {
            return new TablebaseProbeResult(TablebaseProbeStatus.InvalidResponse);
        }
        catch (JsonException)
        {
            return new TablebaseProbeResult(TablebaseProbeStatus.InvalidResponse);
        }
    }

    internal sealed record TablebaseResponse([property: JsonPropertyName("moves")] IReadOnlyList<TablebaseMove>? Moves);

    internal sealed record TablebaseMove([property: JsonPropertyName("uci")] string Uci);
}
