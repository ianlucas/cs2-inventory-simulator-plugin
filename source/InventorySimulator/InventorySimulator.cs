/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars.Validators;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API;
using System.Runtime.InteropServices;

namespace InventorySimulator;

[MinimumApiVersion(197)]
public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
    public override string ModuleName => "InventorySimulator";
    public override string ModuleVersion => "1.0.0-beta.8";

    private readonly string g_InventoriesFilePath = "csgo/css_inventories.json";
    private readonly Dictionary<ulong, PlayerInventory> g_PlayerInventory = new();
    private readonly HashSet<ulong> g_PlayerInventoryLocked = new();
    private ulong g_ItemId = UInt64.MaxValue - 65536;

    private readonly bool g_IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public FakeConVar<int> MinModelsCvar = new("css_minmodels", "Limits the number of custom models in-game.", 0, flags: ConVarFlags.FCVAR_NONE, new RangeValidator<int>(0, 2));
    public FakeConVar<string> InvSimCvar = new("css_inventory_simulator", "Inventory Simulator's URL to consume API.", "https://inventory.cstrike.app");

    public override void Load(bool hotReload)
    {
        LoadPlayerInventories();

        if (g_IsWindows)
        {
            // As GiveNamedItem Post hook doesn't work properly on Windows, we hook OnEntityCreated.
            // Should work well enough for vanilla gamemodes, but plugins may lack compatibility
            // if they change items too fast (see MatchZy knife round for instance).
            RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
        }
        else
        {
            // Using GiveNamedItem Post hook is the best place to update the item's attributes
            // as OnEntityCreated and OnEntitySpawned will require calling Server.NextFrame, and
            // that may cause some timing issues we can observe on knife round from MatchZy.
            // Unfortunately, CounterStikeSharp's DynamicHooks seems really bugged on Windows,
            // maybe this is related to GiveNamedItem implementation on Windows having some quirks,
            // as initially noted we cannot give knives on Windows, but we can on Linux using the
            // same function. So Linux is expected to have the better compatibility with other plugins.
            VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPost, HookMode.Post);
        }
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
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo _)
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
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
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

    public HookResult OnGiveNamedItemPost(DynamicHook hook)
    {
        var className = hook.GetParam<string>(1);
        if (!className.Contains("weapon"))
            return HookResult.Continue;

        var itemServices = hook.GetParam<CCSPlayer_ItemServices>(0);
        var weapon = hook.GetReturn<CBasePlayerWeapon>();
        var player = GetPlayerFromItemServices(itemServices);

        if (player != null)
        {
            GivePlayerWeaponSkin(player, weapon);
        }

        return HookResult.Continue;
    }
}
