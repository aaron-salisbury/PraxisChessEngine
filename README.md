# Praxis Chess Engine

Praxis is a chess engine written in C#. The engine is a command line application that you would select when using a chess interface, such as [Arena](http://www.playwitharena.de/).

## Versioning
This project uses [Semantic Versioning](https://semver.org/).

- **MAJOR** version: Incompatible API changes
- **MINOR** version: Backward-compatible functionality
- **PATCH** version: Backward-compatible bug fixes

## Roadmap

The engine does function in its current state but is not complete.

  - Enhance valid move discovery, such as not attempting to castle if a space in-between can be attacked.
  - Expand promotion logic. Right now, always assuming promotion to queen.
  - Expand opening selection. Right now, first move of the engine is always e2e4 and from there will follow any opening it can use from the ECO.
  - Develop mid-game move evaluation. Right now, a random legal move is selected.
  - Implement additional communication protocol. Right now, only UCI has been started, but developing a [CECP](https://www.chessprogramming.org/Chess_Engine_Communication_Protocol) (Winboard) protocol would make the engine compatible with more interfaces.
