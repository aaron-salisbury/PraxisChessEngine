$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$wslRepoRoot = (wsl wslpath -a $repoRoot).Trim()

if (-not $wslRepoRoot) {
    throw "Unable to resolve the repository path in WSL."
}

wsl bash "$wslRepoRoot/build/smoke-test.sh"

if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
