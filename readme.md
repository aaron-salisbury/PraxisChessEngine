# Praxis Chess Engine

Praxis is a hobbyist chess engine written in C# and built on .NET 10. It runs as a command-line engine for chess interfaces such as [Arena](http://www.playwitharena.de/) and communicates using the Universal Chess Interface (UCI) protocol. Published as self-contained and ahead-of-time (AOT) compiled to native code.

## Key Features

- Legal move generation with castling, promotion, en passant, check, checkmate, and stalemate handling.
- Iterative-deepening negamax search with alpha-beta pruning and quiescence search.
- Zobrist hashing, a transposition table, principal variation tracking, and killer/history move ordering.
- Basic material and piece-activity evaluation.
- Small built-in opening book with automatic fallback to normal search.
- Online Syzygy endgame tablebase probing through Lichess for positions with up to seven pieces, with bounded timeouts and automatic fallback.
- UCI support for use with chess GUIs, including clock, depth, node, and fixed-move-time search limits.
- Native AOT publishing validated on Windows x64, Linux x64, macOS x64, and macOS ARM64.
- Automated unit, canonical perft, UCI protocol, and Cute Chess self-play smoke testing.

## Running Praxis

Praxis does not provide its own chess board interface. Publish or obtain the executable for your platform, then configure it as a UCI engine in a compatible chess GUI.

The executable is self-contained and does not require a separate .NET installation. Internet access is optional; it is used only for online tablebase probing, and unavailable tablebases fall back to normal search.

## Development

The solution is under `src/PraxisChessEngine.slnx` and targets .NET 10. Normal validation is:

```text
dotnet build src/PraxisChessEngine.slnx --configuration Release
dotnet test src/PraxisChessEngine.slnx --configuration Release
```

See [docs/testing.md](docs/testing.md) for perft and Cute Chess smoke-test details. CI also validates Native AOT publishing on the supported desktop targets.

## Versioning

This project uses [Semantic Versioning](https://semver.org/).

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Backward-compatible functionality
- **PATCH** version: Backward-compatible bug fixes

## Future Enhancements

- Replace FEN-based opening-book lookup with an established opening-book format such as [Polyglot](https://hgm.nubati.net/book_format.html).
- Expand the opening repertoire to a substantial collection of known positions with multiple weighted candidate moves.
- Add repeatable benchmarks and tactical/position suites, then improve evaluation, pruning, extensions/reductions, time management, and search efficiency based on measured results.
- Add full draw handling for threefold repetition and insufficient material.
- Currently, evaluation is intentionally basic. Move search clones positions rather than make/unmake, and UCI search information is final-result rather than live iterative output
- Consider local Syzygy tablebase support as an offline alternative to the Lichess provider.
- Add another communication protocol, such as [CECP](https://www.chessprogramming.org/Chess_Engine_Communication_Protocol) (WinBoard), if compatibility with additional interfaces warrants it.
