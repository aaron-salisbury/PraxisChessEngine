# Praxis Chess Engine

Praxis is a chess engine written in C#. The engine is a command line application that you would select when using a chess interface, such as [Arena](http://www.playwitharena.de/).

## Versioning
This project uses [Semantic Versioning](https://semver.org/).

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Backward-compatible functionality
- **PATCH** version: Backward-compatible bug fixes

## Future Enhancements

  - Replace opening-book lookup by FEN string an established opening-book format such as [Polyglot](https://hgm.nubati.net/book_format.html).
  - Expand opening repertoire to thousands, or millions of known positions. With positions having several candidate moves, perhaps with weights.
  - Implement additional communication protocol. Right now, only UCI has been started, but developing a [CECP](https://www.chessprogramming.org/Chess_Engine_Communication_Protocol) (Winboard) protocol would make the engine compatible with more interfaces.
