#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CUTECHESS_VERSION="v1.5.1"
CACHE_ROOT="${XDG_CACHE_HOME:-$HOME/.cache}/praxis/cutechess/$CUTECHESS_VERSION"
CUTECHESS_CLI="$CACHE_ROOT/cutechess-cli"
ARTIFACT_ROOT="$ROOT/artifacts"
PUBLISH_DIR="$ARTIFACT_ROOT/praxis"
SMOKE_DIR="$ARTIFACT_ROOT/smoke"

ensure_cutechess() {
    if [[ -x "$CUTECHESS_CLI" ]]; then
        return
    fi

    for command in git cmake ninja; do
        if ! command -v "$command" >/dev/null 2>&1; then
            echo "Missing '$command'. Install the Cute Chess build prerequisites documented in docs/testing.md." >&2
            exit 1
        fi
    done

    local work
    work="$(mktemp -d)"
    trap 'rm -rf "$work"' RETURN

    echo "Building Cute Chess CLI $CUTECHESS_VERSION..."
    git clone --branch "$CUTECHESS_VERSION" --depth 1 https://github.com/cutechess/cutechess.git "$work/cutechess"
    cmake -S "$work/cutechess" -B "$work/build" -G Ninja -DCMAKE_BUILD_TYPE=Release
    cmake --build "$work/build" --target cutechess-cli

    local built
    built="$(find "$work/build" -type f -name cutechess-cli -perm -111 | head -n 1)"
    test -n "$built"
    mkdir -p "$CACHE_ROOT"
    cp "$built" "$CUTECHESS_CLI"
}

ensure_cutechess

rm -rf "$PUBLISH_DIR" "$SMOKE_DIR"
mkdir -p "$PUBLISH_DIR" "$SMOKE_DIR"

echo "Publishing Praxis..."
dotnet publish "$ROOT/src/PraxisChessEngine.CLIApp/PraxisChessEngine.CLIApp.csproj" \
    --configuration Release \
    --output "$PUBLISH_DIR"

echo "Running UCI self-play smoke test..."
"$CUTECHESS_CLI" \
    -engine name=PraxisWhite cmd="$PUBLISH_DIR/PraxisChessEngine" proto=uci \
    -engine name=PraxisBlack cmd="$PUBLISH_DIR/PraxisChessEngine" proto=uci \
    -each tc=10+0.1 \
    -rounds 2 \
    -repeat \
    -recover \
    -pgnout "$SMOKE_DIR/praxis-smoke.pgn"

echo "Smoke test passed. PGN: $SMOKE_DIR/praxis-smoke.pgn"
