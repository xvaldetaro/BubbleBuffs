# BubbleBuffs Mod Context

## Mac Build Setup (already configured)
- `GamePath.props` points to Mac game location
- Symlink created: `Wrath.app/Contents/Resources/Wrath_Data` → `Data`
- Added `Microsoft.NETFramework.ReferenceAssemblies.net481` v1.0.3 to csproj

## Scripts
- `./build.sh` - builds and copies to Mods folder
- `./copy-config.sh` - copies buff config json to installed mod

## Paths
- Game: `~/Library/Application Support/Steam/SteamApps/common/Pathfinder Second Adventure/`
- Game DLLs: `Wrath.app/Contents/Resources/Data/Managed/`
- Installed mod: `Mods/BubbleBuffs/`
- User config: `bubblebuff-3e5b29b5cb3244c89efc7d638ed20f95.json` (save-specific)

## Key Files
- `BubbleBuffs/BubbleBuffer.cs` - main UI/view logic
- `BubbleBuffs/BubbleBuff.cs` - buff data model
- `BubbleBuffs/Main.cs` - mod entry point, UMM hooks
- `BubbleBuffs/Config/*.json` - localization, blueprints

## Tech
- Unity Mod Manager (UMM) mod
- Harmony patches
- References game DLLs with BepInEx.AssemblyPublicizer for private member access
