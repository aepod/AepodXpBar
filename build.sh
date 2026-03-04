#!/usr/bin/env bash
# build.sh -- Build AepodXpBar BepInEx mod.
#
# Usage:
#   ./build.sh              Build the mod DLL
#   ./build.sh --check      Verify environment only (no build)
#   ./build.sh --clean      Clean then build
#
# Environment:
#   $ErenshorGamePath       Path to Erenshor game install (required)
#                           Default: /mnt/d/SteamLibrary/steamapps/common/Erenshor

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT="$SCRIPT_DIR/AepodXpBar.csproj"
DLL_NAME="AepodXpBar.dll"
DLL_OUTPUT="$SCRIPT_DIR/bin/Debug/netstandard2.1/$DLL_NAME"

# Game paths
GAME_DIR="${ErenshorGamePath:-/mnt/d/SteamLibrary/steamapps/common/Erenshor}"
BEPINEX_PATH="$GAME_DIR/BepInEx/core"
CORLIB_PATH="$GAME_DIR/Erenshor_Data/Managed"

# Parse flags
CHECK_ONLY=false
CLEAN=false
for arg in "$@"; do
    case "$arg" in
        --check) CHECK_ONLY=true ;;
        --clean) CLEAN=true ;;
        *) echo "Unknown flag: $arg"; exit 1 ;;
    esac
done

# --- Environment Verification ---

echo "=== AepodXpBar Build ==="
echo "  Game path:   $GAME_DIR"
echo "  BepInEx:     $BEPINEX_PATH"
echo "  Managed:     $CORLIB_PATH"
echo ""

ERRORS=0

if [ ! -d "$GAME_DIR" ]; then
    echo "ERROR: Game directory not found: $GAME_DIR"
    echo "  Set \$ErenshorGamePath to your Erenshor install path."
    ERRORS=$((ERRORS + 1))
fi

if [ ! -f "$BEPINEX_PATH/BepInEx.dll" ]; then
    echo "ERROR: BepInEx.dll not found at $BEPINEX_PATH/BepInEx.dll"
    ERRORS=$((ERRORS + 1))
fi

if [ ! -f "$BEPINEX_PATH/0Harmony.dll" ]; then
    echo "ERROR: 0Harmony.dll not found at $BEPINEX_PATH/0Harmony.dll"
    ERRORS=$((ERRORS + 1))
fi

if [ ! -f "$CORLIB_PATH/Assembly-CSharp.dll" ]; then
    echo "ERROR: Assembly-CSharp.dll not found at $CORLIB_PATH/"
    ERRORS=$((ERRORS + 1))
fi

if [ ! -f "$CORLIB_PATH/Unity.TextMeshPro.dll" ]; then
    echo "ERROR: Unity.TextMeshPro.dll not found at $CORLIB_PATH/"
    ERRORS=$((ERRORS + 1))
fi

if ! command -v dotnet &>/dev/null; then
    echo "ERROR: dotnet SDK not found in PATH"
    ERRORS=$((ERRORS + 1))
fi

if [ $ERRORS -gt 0 ]; then
    echo ""
    echo "$ERRORS error(s) found. Fix before building."
    exit 1
fi

echo "Environment OK."

if $CHECK_ONLY; then
    echo "Check complete (--check mode, no build)."
    exit 0
fi

# --- Clean ---

if $CLEAN; then
    echo ""
    echo "Cleaning..."
    rm -rf "$SCRIPT_DIR/bin" "$SCRIPT_DIR/obj"
fi

# --- Build ---

echo ""
echo "Building $DLL_NAME..."

# Note: PostBuild .bat failure under WSL is expected and harmless.
# The DLL compiles successfully before the .bat fails.
dotnet build "$PROJECT" \
    -p:GamePath="$GAME_DIR" \
    -p:BepInExPath="$BEPINEX_PATH" \
    -p:CorlibPath="$CORLIB_PATH" \
    2>&1 || true

# --- Verify output ---

if [ -f "$DLL_OUTPUT" ]; then
    echo ""
    echo "=== Build successful ==="
    ls -lh "$DLL_OUTPUT"
else
    echo ""
    echo "ERROR: Build failed -- $DLL_OUTPUT not found"
    exit 1
fi
