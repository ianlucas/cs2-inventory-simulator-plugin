# CS2 Inventory Simulator Plugin

A simple plugin for integrating with [CS2 Inventory Simulator](https://inventory.cstrike.app). It features basically all we know (publically) so far to display economy items in-game, so it's full of hacks of all sorts and is missing a lot of features.

> [!CAUTION]
> This plugin has not been fully and thoroughly tested. Compatibility with other plugins has also not been tested. Use at your own risk.

> [!CAUTION]
> As you probably know, Valve can ban your server for using plugins like this one, so be advised. [See more information on Valve Guidelines...](https://blog.counter-strike.net/index.php/server_guidelines)

## Current Features

- Weapon/Knife
  - Paint Kit, Wear, Seed, Name tag, StatTrak.
- Music Kit

## Feature Roadmap

- [ ] Agents
- [ ] StatTrak increment
- [ ] ⛔ Weapon Stickers
- [ ] ⛔ Pins
- [ ] ⛔ Agent Patches
- [ ] ⛔ Gloves
- [ ] ⛔ Graffiti

> [!IMPORTANT]  
> ⛔ means I'm not aware of a way to modify using CSSharp or C++ and is very unlikely to be implemented soon.

> [!WARNING]  
> Right now I'm open to issue reports, please don't open feature request or suggestion issues - they will be closed. I may take your comments into account, but the issue is going to remain closed.

## Installation

- [Download](https://github.com/ianlucas/cs2-InventorySimulatorPlugin/releases) the latest release.
- Extract the .zip file into `addons/counterstrikesharp/plugins`.
- Make sure `FollowCS2ServerGuidelines` is `false` in `addons/counterstrikesharp/configs/core.json`.

### Configuration?

Not right now. I'm planning on adding options for the Inventory Simulator endpoint and `cslib`'s `items.json` endpoint (so you can point to yours). So right now you depend on my online services or a fork of the project.

### Commands?

Not right now. I'm planning on adding a command for refreshing the inventory, but it's not really high priority for me as I'm going to use this on competitive matches, and I don't want players messing with skins mid-game, so right now the skins are only fetched when the player connects to the server.

### Known issues

- All knives will have the rare deploy animation.

## See also

If you are looking for a plugin that gives you more control, please see [cs2-weaponPaints](https://github.com/Nereziel/cs2-WeaponPaints).
