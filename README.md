# CS2 Inventory Simulator Plugin

A simple plugin for integrating with [CS2 Inventory Simulator](https://inventory.cstrike.app). It features all we know (publically) so far to give economy items in-game.

> [!CAUTION]
> This plugin has not been fully and thoroughly tested. Compatibility with other plugins has also not been tested. Your server can be banned by Valve for using this plugin, [see the server guidelines](https://blog.counter-strike.net/index.php/server_guidelines). Use at your own risk.

## Current Features

- Weapon
  - Paint Kit, Wear, Seed, Name tag, StatTrak (with increment), and Stickers.
- Knife
  - Paint Kit, Wear, Seed, Name tag, and StatTrak (with increment).
- Gloves
  - Paint Kit, Wear, Seed.
- Agents
  - Patches.
- Music Kit
- Pins

## Feature Roadmap

- ⛔ Graffiti

> [!IMPORTANT]  
> ⛔ indicates that I'm unaware of a way to modify using CSSharp or C++ and that it's highly unlikely to be implemented anytime soon.

> [!WARNING]  
> Currently, I'm accepting issue reports, but please refrain from opening feature requests or suggestion issues as they will be closed. While I may consider your comments, the issue will remain closed.

## Installation

1. Make sure `FollowCS2ServerGuidelines` is `false` in `addons/counterstrikesharp/configs/core.json`.
2. [Download the latest release](https://github.com/ianlucas/cs2-inventory-simulator-plugin/releases) of CS2 Inventory Simulator Plugin.
3. Extract the ZIP file contents into `addons/counterstrikesharp`.

### Configuration

#### `css_inventory_simulator` ConVar

* Host of Inventory Simulator's API.
* **Type:** `string`
* **Default:** `inventory.cstrike.app`

#### `css_inventory_simulator_apikey` ConVar

* API Key for Inventory Simulator.
* **Type:** `string`
* **Default:** _empty_

#### `css_stattrak_ignore_bots` ConVar

* Determines whether to ignore StatTrak increments for bot kills.
* **Type:** `bool`
* **Default:** `true`

#### `css_minmodels` ConVar

* Limits the number of custom models allowed in-game.
* **Type:** `int`
* **Default:** `0`
* **Values:**
	- `0` - All agents allowed.
	- `1` - Default agents for the current map. **Note:** Currently the same as `2` as Valve has not yet added them back.
	- `2` - Only SAS and Phoenix agents allowed.

### Commands?

Not at the moment. I'm considering adding a command for refreshing the inventory, but it's not high priority for me. Since I'll be using this during competitive matches, I don't want players to be able to change skins mid-game. Currently, skins are only fetched when a player connects to the server.

### Known Issues

* MVP theme does not play for music kits.

## See also

If you are looking for a plugin that gives you more control, please see [cs2-WeaponPaints](https://github.com/Nereziel/cs2-WeaponPaints).
