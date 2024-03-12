/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Cvars.Validators;

namespace InventorySimulator;

[MinimumApiVersion(175)]
public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
    public override string ModuleName => "InventorySimulator";
    public override string ModuleVersion => "1.0.0-beta";
    
    private readonly string g_InventoriesFilePath = "csgo/css_inventories.json";
    private readonly Dictionary<ulong, PlayerInventory> g_PlayerInventory = new();
    private readonly HashSet<ulong> g_PlayerInventoryLocked = new();
    private ulong g_ItemId = UInt64.MaxValue - 65536;

    public FakeConVar<int> MinModelsCvar = new("css_minmodels", "Limits the number of custom models in-game.", 0, flags: ConVarFlags.FCVAR_NONE, new RangeValidator<int>(0, 2));
    public FakeConVar<string> InvSimCvar = new("css_inventory_simulator", "Inventory Simulator's URL to consume API.", "https://inventory.cstrike.app");

    public override void Load(bool hotReload)
    {
        LoadPlayerInventories();
        // I think ideally hooking GiveNamedItem is the way to go, however we're unable to use it
        // as it currently crashes on Windows. Seems like both OnEntityCreated and OnEntitySpawned
        // do the trick for changing entity properties, however we hit some incompatibility with
        // other plugins that are also hooking it (probably because of the NextFrame call).
        // I switched back to OnEntityCreated to check if compatibility with MatchZy is recovered,
        // they are hooking OnEntitySpawned to get some stats...
        RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
    }

    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo _)
    {
        CCSPlayerController? player = @event.Userid;
        if (!IsPlayerHumanAndValid(player))
            return HookResult.Continue;

        var steamId = player.SteamID;
        FetchPlayerInventory(steamId);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        CCSPlayerController? player = @event.Userid;
        if (!IsPlayerHumanAndValid(player))
            return HookResult.Continue;

        var steamId = player.SteamID;
        FetchPlayerInventory(steamId);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (!IsPlayerHumanAndValid(player) || !IsPlayerPawnValid(player))
            return HookResult.Continue;

        GivePlayerMusicKit(player);
        GivePlayerAgent(player);
        GivePlayerGloves(player);
        GivePlayerPin(player);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (IsPlayerHumanAndValid(player) && !g_PlayerInventoryLocked.Contains(player.SteamID))
        {
            g_PlayerInventory.Remove(player.SteamID);
        }

        return HookResult.Continue;
    }

    public void OnEntityCreated(CEntityInstance entity)
    {
        var designerName = entity.DesignerName;

        if (designerName.Contains("weapon"))
        {
            Server.NextFrame(() =>
            {
                var weapon = new CBasePlayerWeapon(entity.Handle);
                if (!weapon.IsValid || weapon.OriginalOwnerXuidLow == 0) return;

                var player = Utilities.GetPlayerFromSteamId((ulong)weapon.OriginalOwnerXuidLow);
                if (player == null || !IsPlayerHumanAndValid(player)) return;

                GivePlayerWeaponSkin(player, weapon);
            });
        }
    }
}
