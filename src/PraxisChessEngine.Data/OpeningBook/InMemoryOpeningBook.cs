using PraxisChessEngine.Business.Chess;
using PraxisChessEngine.Business.Engine;
using System;
using System.Collections.Generic;

namespace PraxisChessEngine.Data.OpeningBook;

/// <inheritdoc cref="IOpeningBook"/>
/// <remarks>
/// This is currently a proof-of-capability, in-memory, implementation of an opening-book abstraction.
/// </remarks>
internal sealed class InMemoryOpeningBook : IOpeningBook
{
    private static readonly IReadOnlyDictionary<string, string> MOVES = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Position.START_FEN] = "e2e4",
        ["rnbqkbnr/pppppppp/8/8/4P3/8/PPPP1PPP/RNBQKBNR b KQkq e3 0 1"] = "e7e5",
        ["rnbqkbnr/pppp1ppp/8/4p3/4P3/8/PPPP1PPP/RNBQKBNR w KQkq e6 0 2"] = "g1f3"
    };

    public Move? FindMove(Position position)
    {
        return MOVES.TryGetValue(position.ToFen(), out string? move) ? Move.Parse(move) : null;
    }
}
