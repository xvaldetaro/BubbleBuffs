#!/bin/bash
set -e

cd "$(dirname "$0")"

DEST="$HOME/Library/Application Support/Steam/SteamApps/common/Pathfinder Second Adventure/Mods/BubbleBuffs/UserSettings"

echo "Copying config to Mods folder..."
cp "bubblebuff-3e5b29b5cb3244c89efc7d638ed20f95.json" "$DEST/"

echo "Done!"
