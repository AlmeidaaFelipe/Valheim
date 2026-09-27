# HotbarDeathKeeper

A Valheim BepInEx mod that keeps your vanilla hotbar items (slots 1-8: weapon, shield,
tool, torch, etc) equipped after death, instead of losing them to the tombstone.

**Fully standalone - does not require [ExtraSlots](https://valheim.thunderstore.io/package/shudnal/ExtraSlots/)
or any other mod to work.** It only depends on BepInEx. If you happen to also use
ExtraSlots, the two are compatible and complement each other: ExtraSlots keeps its own
extra slots (Equipment/Quick/Food/Ammo/Misc), while this mod covers the vanilla hotbar,
which ExtraSlots does not touch. Neither mod requires the other to be installed.

**Client-side only.** Must be installed on your client to affect your character (the
character owner is the client, not the server). Does not need to be installed on the
server.

## Why this mod exists

This is not an "inventory prevention" mod. The idea came from a specific need: not losing
your **equipped** items - the ones on your hotbar (weapon, shield, tool, torch) - when you
die. On its own, this mod already covers that need completely by itself. If you also run
ExtraSlots (which keeps its own equipped items: Equipment/Quick/Food/Ammo/Misc), the
coverage extends to those slots too - but that's an optional bonus, not a requirement.

In other words: the only thing protected is what's equipped (hotbar, plus ExtraSlots slots
if you have that mod installed). The rest of your inventory still goes to the tombstone as
normal, with no protection at all.

The goal is not to remove the tombstone from the game or take away the death penalty -
that stays exactly the same. What changes is that, on respawn, you don't have to reforge
your weapon and armor from scratch before heading back to recover the rest. This mainly
helps early game, where losing your whole gear can grind you to a halt; with the mod, you
respawn, walk back to your tombstone, recover the rest of your inventory, and you're
already geared up to make the trip back.

## Installation

Via r2modman/Thunderstore Mod Manager: install normally, it resolves the BepInExPack
dependency on its own.

Manual: extract the release zip into your client's `BepInEx/plugins/HotbarDeathKeeper/`
folder.

## Configuration

After running it once, edit `BepInEx/config/hdk.valheim.hotbardeathkeeper.cfg`:

- `Enabled` (default: true)
- `Keep equipped state` (default: true) - automatically re-equips whatever was in your
  hand when you died.

## Building from source

Requires .NET SDK (net48 target) and a local Valheim dedicated server install for the
game/BepInEx assembly references used in `HotbarDeathKeeper.csproj`.

```
dotnet build -c Release
```

The compiled `HotbarDeathKeeper.dll` is written to `bin/Release/`.

## Repository layout

- `Plugin.cs`, `DeathKeeperPatches.cs` - mod source.
- `package/` - ready-to-zip Thunderstore package (manifest.json, icon.png, README.md,
  CHANGELOG.md, compiled DLL).

## License

MIT - see [LICENSE](LICENSE).
