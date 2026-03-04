#!/usr/bin/env bash
# deploy.sh -- Deploy or package AepodXpBar BepInEx mod.
#
# Usage:
#   ./deploy.sh                    Deploy DLL to game plugins folder
#   ./deploy.sh [TARGET_DIR]       Deploy to specific directory
#   ./deploy.sh --package          Create distributable zip
#
# Environment:
#   $ErenshorGamePath              Path to Erenshor game install (required for deploy)
#                                  Default: /mnt/d/SteamLibrary/steamapps/common/Erenshor

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DLL_NAME="AepodXpBar.dll"
DLL_PATH="$SCRIPT_DIR/bin/Debug/netstandard2.1/$DLL_NAME"
MOD_VERSION="1.0.0"

# Parse flags
PACKAGE_MODE=false
POSITIONAL_ARGS=()
for arg in "$@"; do
    case "$arg" in
        --package) PACKAGE_MODE=true ;;
        *) POSITIONAL_ARGS+=("$arg") ;;
    esac
done

# --- Verify build artifact ---

if [ ! -f "$DLL_PATH" ]; then
    echo "ERROR: $DLL_NAME not found at $DLL_PATH"
    echo "Build first: ./build.sh"
    exit 1
fi

# ═══════════════════════════════════════════════════════════════
# Package mode: create distributable zip
# ═══════════════════════════════════════════════════════════════
if $PACKAGE_MODE; then
    DIST_DIR="$SCRIPT_DIR/dist"
    ZIP_NAME="AepodXpBar-v${MOD_VERSION}.zip"
    ZIP_PATH="$DIST_DIR/$ZIP_NAME"

    echo "=== Packaging AepodXpBar v${MOD_VERSION} ==="

    mkdir -p "$DIST_DIR"

    rm -f "$ZIP_PATH"

    # Stage flat -- DLL + README at zip root
    STAGING="$DIST_DIR/staging"
    rm -rf "$STAGING"
    mkdir -p "$STAGING"

    echo "  $DLL_NAME"
    cp "$DLL_PATH" "$STAGING/$DLL_NAME"

    if [ -f "$SCRIPT_DIR/README.md" ]; then
        echo "  README.md"
        cp "$SCRIPT_DIR/README.md" "$STAGING/README.md"
    fi

    echo ""
    echo "Creating $ZIP_NAME..."
    (cd "$STAGING" && zip -r "$ZIP_PATH" .)

    rm -rf "$STAGING"

    echo ""
    echo "=== Package created ==="
    echo "  $(ls -lh "$ZIP_PATH" | awk '{print $5, $NF}')"
    exit 0
fi

# ═══════════════════════════════════════════════════════════════
# Deploy mode: copy DLL to game plugins folder
# ═══════════════════════════════════════════════════════════════

if [ ${#POSITIONAL_ARGS[@]} -ge 1 ]; then
    TARGET="${POSITIONAL_ARGS[0]}"
else
    GAME_DIR="${ErenshorGamePath:-/mnt/d/SteamLibrary/steamapps/common/Erenshor}"
    if [ ! -d "$GAME_DIR" ]; then
        echo "ERROR: Game directory not found: $GAME_DIR"
        echo "Usage: $0 [TARGET_DIR]"
        echo "  or:  export ErenshorGamePath=/path/to/Erenshor && $0"
        exit 1
    fi
    TARGET="$GAME_DIR/BepInEx/plugins"
fi

if [ ! -d "$TARGET" ]; then
    echo "ERROR: Target directory not found: $TARGET"
    exit 1
fi

echo "=== Deploying AepodXpBar ==="
echo "  Source: $DLL_PATH"
echo "  Target: $TARGET/$DLL_NAME"
echo ""

cp "$DLL_PATH" "$TARGET/$DLL_NAME"
sync 2>/dev/null || true

echo "  Deployed $DLL_NAME ($(ls -lh "$TARGET/$DLL_NAME" | awk '{print $5}'))"
echo ""
echo "=== Deployment complete ==="
echo "  Config will be generated at: BepInEx/config/com.aepod.erenshor.xpbar.cfg"
echo "  Check log: BepInEx/LogOutput.log"
