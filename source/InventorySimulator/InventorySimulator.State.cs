/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using System.Runtime.InteropServices;

namespace InventorySimulator;

public partial class InventorySimulator
{
    /* public readonly FakeConVar<bool> invsim_stattrak_ignore_bots = new("invsim_stattrak_ignore_bots", "Whether to ignore StatTrak increments for bot kills.", true);
    public readonly FakeConVar<bool> invsim_ws_enabled = new("invsim_ws_enabled", "Whether players can refresh their inventory using !ws.", false);
    public readonly FakeConVar<int> invsim_minmodels = new("invsim_minmodels", "Allows agents or use specific models for each team.", 0, flags: ConVarFlags.FCVAR_NONE, new RangeValidator<int>(0, 2));
    public readonly FakeConVar<int> invsim_ws_cooldown = new("invsim_ws_cooldown", "Cooldown in seconds between player inventory refreshes.", 30);
    public readonly FakeConVar<string> invsim_apikey = new("invsim_apikey", "Inventory Simulator API's key.", "");
    public readonly FakeConVar<string> invsim_hostname = new("invsim_hostname", "Inventory Simulator API's hostname.", "inventory.cstrike.app");
    public readonly FakeConVar<string> invsim_protocol = new("invsim_protocol", "Inventory Simulator API's protocol.", "https"); */

    public readonly HashSet<ulong> FetchingPlayerInventory = [];
    public readonly HashSet<ulong> LoadedPlayerInventory = [];

    public readonly Dictionary<ulong, long> PlayerCooldownManager = [];
    public readonly Dictionary<ulong, (CCSPlayerController?, PlayerInventory)> PlayerOnTickInventoryManager = [];
    public readonly Dictionary<ulong, PlayerInventory> PlayerInventoryManager = [];

    public readonly PlayerInventory EmptyInventory = new();

    public static readonly string InventoryFilePath = "csgo/addons/counterstrikesharp/configs/plugins/InventorySimulator/inventories.json";
    public static readonly ulong MinimumCustomItemID = 68719476736;

    public ulong NextItemId = MinimumCustomItemID;
    public int NextFadeSeed = 3;
}
