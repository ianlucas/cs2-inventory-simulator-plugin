# CS2 Inventory Simulator Plugin

A simple plugin for integrating with [CS2 Inventory Simulator](https://inventory.cstrike.app). It features all we know (publically) so far to give economy items in-game.

> [!CAUTION]
> This plugin has not been fully and thoroughly tested. Compatibility with other plugins has also not been tested. Your server can be banned by Valve for using this plugin, [see the server guidelines](https://blog.counter-strike.net/index.php/server_guidelines). Use at your own risk.

## Current Features

- Weapon/Knife
  - Paint Kit, Wear, Seed, Name tag, StatTrak, and Stickers.
- Gloves
  - Paint Kit, Wear, Seed. 
- Agents
- Music Kit
- Pins

## Feature Roadmap

- StatTrak increment
- ⛔ Agent Patches
- ⛔ Graffiti

> [!IMPORTANT]  
> ⛔ means I'm not aware of a way to modify using CSSharp or C++ and is very unlikely to be implemented any time soon.

> [!WARNING]  
> Right now I'm open to issue reports, please don't open feature request or suggestion issues - they will be closed. I may take your comments into account, but the issue is going to remain closed.

## Installation

1. Make sure `FollowCS2ServerGuidelines` is `false` in `addons/counterstrikesharp/configs/core.json`.
2. Add the contents of `gamedata/gamedata.json` to `addons/counterstrikesharp/gamedata/gamedata.json`.
3. [Download](https://github.com/ianlucas/cs2-inventory-simulator-plugin/releases) the latest release of CS2 Inventory Simulator Plugin.
4. Extract the .zip file into `addons/counterstrikesharp`.

### Configuration

#### `css_minmodels` ConVar

* Description: Limits the usage of agents by the players.
* Type: `int`
* Default: `0`
* Values:
	- `0` - agents allowed.
	- `1` - current map default agents. **Note:** currently the same as `2` as Valve is yet to add them back.
	- `2` - SAS and Phoenix agents only.

#### `css_inventory_simulator` ConVar

* Description: The base url to be used to consume Inventory Simulator's API.
* Type: `string`
* Default: `https://inventory.cstrike.app`

### Commands?

Not right now. I'm planning on adding a command for refreshing the inventory, but it's not really high priority for me as I'm going to use this on competitive matches, and I don't want players messing with skins mid-game, so right now the skins are only fetched when the player connects to the server.

### Known Issues

* MVP theme not playing for music kits.
* Players own equipped gloves won't change. (Fixed?)

## See also

If you are looking for a plugin that gives you more control, please see [cs2-WeaponPaints](https://github.com/Nereziel/cs2-WeaponPaints).
