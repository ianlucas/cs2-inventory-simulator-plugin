# CS2 Inventory Simulator Plugin

A CounterStrikeSharp plugin for integrating with [CS2 Inventory Simulator](https://inventory.cstrike.app). It features all current knowledge on how to give economy items to players from the server-side.

> [!CAUTION]  
> This plugin has not been fully and thoroughly tested. Compatibility with other plugins has also not been tested. Your server can be banned by Valve for using this plugin, [see the server guidelines](https://blog.counter-strike.net/index.php/server_guidelines). Use at your own risk.

## Current Features

- Weapon
  - Paint Kit, Wear, Seed, Name tag, StatTrak (with increment), and Stickers.
- Knife
  - Paint Kit, Wear, Seed, Name tag, and StatTrak (with increment).
- Gloves
  - Paint Kit, Wear, Seed.
- Agent
  - Patches.
- Music Kit
  - StatTrak (with increment). 
- Pin

### Known Issues

* `Windows` Incompatibility with MatchZy's Knife Round ([view issue](https://github.com/roflmuffin/CounterStrikeSharp/issues/377)).

## Feature Roadmap

- ⛔ Graffiti - Reversing needed.

> [!WARNING]  
> Currently, I'm accepting issue reports, but please refrain from opening feature requests or suggestion issues as they will be closed. While I may consider your comments, the issue will remain closed.

## Installation

1. Make sure `FollowCS2ServerGuidelines` is `false` in `addons/counterstrikesharp/configs/core.json`.
2. [Download the latest release](https://github.com/ianlucas/cs2-inventory-simulator-plugin/releases) of CS2 Inventory Simulator Plugin.
3. Extract the ZIP file contents into `addons/counterstrikesharp`.

### Configuration

#### `invsim_hostname` ConVar

* Inventory Simulator API's hostname.
* **Type:** `string`
* **Default:** `inventory.cstrike.app`

#### `invsim_apikey` ConVar

* Inventory Simulator API's key.
* **Type:** `string`
* **Default:** _empty_

#### `invsim_stattrak_ignore_bots` ConVar

* Whether to ignore StatTrak increments for bot kills.
* **Type:** `bool`
* **Default:** `true`

#### `invsim_minmodels` ConVar

* Allows agents or use specific models for each team.
* **Type:** `int`
* **Default:** `0`
* **Values:**
	- `0` - All agents allowed.
	- `1` - Default agents for the current map. **Note:** Same as `2` as Valve has not yet added them back.
	- `2` - Only SAS and Phoenix agents allowed.

#### `invsim_ws_enabled` ConVar

* Whether players can refresh their inventory using `!ws` command.
* **Type:** `bool`
* **Default:** `false`

#### `invsim_ws_cooldown` ConVar

* Cooldown in seconds between player inventory refreshes.
* **Type:** `int`
* **Default:** `30`

### Commands

#### `!ws` Command

* Prints Inventory Simulator's website and refreshes player's inventory if `invsim_ws_enabled` ConVar is `true`.

## See also

If you are looking for a plugin that gives you more control, please see [cs2-WeaponPaints](https://github.com/Nereziel/cs2-WeaponPaints).