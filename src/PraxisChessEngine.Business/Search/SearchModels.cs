using System;
using System.Collections.Generic;
using System.Threading;
using PraxisChessEngine.Business.Chess;

namespace PraxisChessEngine.Business.Search;

public sealed record SearchLimits(int? Depth = null, TimeSpan? MoveTime = null, long? Nodes = null);

public sealed record SearchResult(Move? BestMove, int Score, int Depth, long Nodes, IReadOnlyList<Move> PrincipalVariation);

public interface ISearchService
{
    SearchResult Search(Position position, SearchLimits limits, CancellationToken cancellationToken);
}
