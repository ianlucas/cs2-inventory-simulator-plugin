/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Collections.Concurrent;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public readonly FakeConVar<bool> invsim_stattrak_ignore_bots = new(
        "invsim_stattrak_ignore_bots",
        "Whether to ignore StatTrak increments for bot kills.",
        true
    );
    public readonly FakeConVar<bool> invsim_spraychanger_enabled = new(
        "invsim_spraychanger_enabled",
        "Whether to change player vanilla spray if they have a graffiti equipped.",
        false
    );
    public readonly FakeConVar<bool> invsim_spray_on_use = new(
        "invsim_spray_on_use",
        "Whether to try to apply spray when player presses use.",
        false
    );
    public readonly FakeConVar<bool> invsim_ws_enabled = new(
        "invsim_ws_enabled",
        "Whether players can refresh their inventory using !ws.",
        false
    );
    public readonly FakeConVar<bool> invsim_ws_print_full_url = new(
        "invsim_ws_print_full_url",
        "Whether print full URL when the player uses !ws.",
        true
    );
    public readonly FakeConVar<bool> invsim_require_inventory = new(
        "invsim_require_inventory",
        "Require the player's inventory to be fetched before allowing them to connect to the game.",
        false
    );
    public readonly FakeConVar<int> invsim_minmodels = new(
        "invsim_minmodels",
        "Allows agents or use specific models for each team.",
        0,
        flags: ConVarFlags.FCVAR_NONE,
        new RangeValidator<int>(0, 2)
    );
    public readonly FakeConVar<int> invsim_ws_cooldown = new(
        "invsim_ws_cooldown",
        "Cooldown in seconds between player inventory refreshes.",
        30
    );
    public readonly FakeConVar<int> invsim_spray_cooldown = new(
        "invsim_spray_cooldown",
        "Cooldown in seconds between player sprays.",
        30
    );
    public readonly FakeConVar<string> invsim_apikey = new(
        "invsim_apikey",
        "Inventory Simulator API's key.",
        ""
    );
    public readonly FakeConVar<string> invsim_hostname = new(
        "invsim_hostname",
        "Inventory Simulator API's hostname.",
        "inventory.cstrike.app"
    );
    public readonly FakeConVar<string> invsim_protocol = new(
        "invsim_protocol",
        "Inventory Simulator API's protocol.",
        "https"
    );
    public readonly FakeConVar<string> invsim_file = new(
        "invsim_file",
        "File to load when plugin is loaded.",
        "inventories.json"
    );

    public readonly HashSet<ulong> FetchingPlayerInventory = [];
    public readonly HashSet<ulong> LoadedPlayerInventory = [];

    public readonly ConcurrentDictionary<ulong, long> PlayerCooldownManager = [];
    public readonly ConcurrentDictionary<ulong, long> PlayerSprayCooldownManager = [];
    public readonly ConcurrentDictionary<
        ulong,
        (CCSPlayerController?, PlayerInventory)
    > PlayerOnTickInventoryManager = [];
    public readonly ConcurrentDictionary<ulong, PlayerInventory> PlayerInventoryManager = [];
    public readonly ConcurrentDictionary<ulong, Timer> PlayerUseCmdManager = [];
    public readonly ConcurrentDictionary<ulong, bool> PlayerUseCmdBlockManager = [];
    public readonly ConcurrentDictionary<IntPtr, short> ServerSideClientUserid = [];

    public readonly PlayerInventory EmptyInventory = new();

    public static readonly string InventoryFileDir =
        "csgo/addons/counterstrikesharp/configs/plugins/InventorySimulator";
    public static readonly ulong MinimumCustomItemID = 68719476736;

    public ulong NextItemId = MinimumCustomItemID;
}
