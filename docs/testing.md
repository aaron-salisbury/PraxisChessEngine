# Engine Testing

Praxis uses two complementary forms of automated chess-engine validation.

## Perft

Move-generation correctness is tested directly in MSTest with canonical perft positions. These tests intentionally exercise the engine's own `MoveGenerator` without UCI or another chess process in between.

The suite includes the starting position, Kiwipete, and standard positions 3 through 6. Keep these deterministic tests fast enough for normal CI. Add deeper counts selectively when a move-generation defect needs a stronger regression test.

## UCI smoke test

GitHub Actions publishes the actual Praxis executable and uses `cutechess-cli` to run short Praxis-vs-Praxis games over UCI.

The smoke test is meant to catch integration failures such as:

- failure to start or complete the UCI handshake;
- malformed or illegal `bestmove` responses;
- crashes or hangs during a game;
- broken position synchronization;
- basic clock-management failures.

This is deliberately a smoke test, not a playing-strength benchmark. Strength and regression matches belong after the modernization roadmap is complete.

For local testing, install Cute Chess and run the same command used by `.github/workflows/build-and-test.yml`. Arena remains useful for manually watching games and investigating behavior that an automated pass/fail test cannot judge.
