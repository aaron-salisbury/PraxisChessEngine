# Engine Testing

Praxis uses two complementary forms of automated chess-engine validation.

## Perft

Move-generation correctness is tested directly in MSTest with canonical perft positions. These tests intentionally exercise the engine's own `MoveGenerator` without UCI or another chess process in between.

The suite includes the starting position, Kiwipete, and standard positions 3 through 6. Keep these deterministic tests fast enough for normal CI. Add deeper counts selectively when a move-generation defect needs a stronger regression test.

## UCI smoke test

The smoke test publishes the actual Praxis executable and uses `cutechess-cli` to run short Praxis-vs-Praxis games over UCI. GitHub Actions and local development both use `build/smoke-test.sh` so the smoke test has one implementation.

The smoke test is meant to catch integration failures such as:

- failure to start or complete the UCI handshake;
- malformed or illegal `bestmove` responses;
- crashes or hangs during a game;
- broken position synchronization;
- basic clock-management failures.

This is deliberately a smoke test, not a playing-strength benchmark. Strength and regression matches belong after the modernization roadmap is complete.

### Windows with WSL

From PowerShell at the repository root:

```powershell
./build/smoke-test.ps1
```

The PowerShell script launches the shared Bash smoke test in WSL. Cute Chess 1.5.1 is built on first use and cached under `~/.cache/praxis/cutechess/`; its temporary source/build directory is removed afterward. Praxis itself is freshly published for each run.

The WSL distribution needs .NET 10 and these build dependencies installed once:

```bash
sudo apt update
sudo apt install cmake ninja-build qt6-base-dev qt6-svg-dev qt6-5compat-dev
```

Subsequent smoke tests reuse the cached Cute Chess executable.

### Linux

Run:

```bash
bash build/smoke-test.sh
```

Install the same Cute Chess build dependencies if they are not already present.

Arena remains useful for manually watching games and investigating behavior that an automated pass/fail test cannot judge.
