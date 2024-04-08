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
    public override string ModuleVersion => "1.0.0-beta.17";

    private readonly string InventoryFilePath = "csgo/css_inventories.json";
    private readonly Dictionary<ulong, PlayerInventory> InventoryManager = new();
    private readonly HashSet<ulong> LoadedSteamIds = new();
    private static readonly ulong MinimumCustomItemID = 68719476736;
    private ulong NextItemId = MinimumCustomItemID;

    private readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public FakeConVar<string> InvSimProtocolCvar = new("css_inventory_simulator_protocol", "Protocol used by Inventory Simulator to consume its API.", "https");
    public FakeConVar<string> InvSimCvar = new("css_inventory_simulator", "Host of Inventory Simulator's API.", "inventory.cstrike.app");
    public FakeConVar<string> InvSimApiKeyCvar = new("css_inventory_simulator_apikey", "API Key for Inventory Simulator.", "");
    public FakeConVar<bool> StatTrakIgnoreBotsCvar = new("css_stattrak_ignore_bots", "Determines whether to ignore StatTrak increments for bot kills.", true);
    public FakeConVar<int> MinModelsCvar = new("css_minmodels", "Limits the number of custom models allowed in-game.", 0, flags: ConVarFlags.FCVAR_NONE, new RangeValidator<int>(0, 2));

    public override void Load(bool hotReload)
    {
        LoadPlayerInventories();

        if (IsWindows)
        {
            // Since the OnGiveNamedItemPost hook doesn't function reliably on Windows, we've opted to use the
            // OnEntityCreated hook instead. This approach should work adequately for standard game modes. However,
            // plugins might encounter compatibility issues if they frequently alter items, as observed in the
            // MatchZy knife round, for example.
            RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
        }
        else
        {
            // Using the GiveNamedItem Post hook remains the optimal choice for updating an item's attributes, as
            // using OnEntityCreated or OnEntitySpawned would necessitate calling Server.NextFrame, potentially
            // leading to timing problems similar to those seen in MatchZy's knife round. However, it's worth
            // noting that CounterStrikeSharp's DynamicHooks appears to have significant bugs on Windows. This
            // issue may be related to quirks in the GiveNamedItem implementation on Windows, as initially observed
            // with the inability to give knives on Windows compared to Linux, where the same function works as
            // expected. Therefore, Linux is likely to offer better compatibility with other plugins.
            VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPost, HookMode.Post);

            // We also hook into OnEntityCreated for cases where the plugin does not trigger the GiveNamedItem hook
            // (e.g., CS2 Retakes). Most of the time, GiveNamedItem will be called first, and we will know that we
            // have changed a weapon entity to avoid changing its attributes again.
            RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
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

        var inventory = GetPlayerInventory(player);
        GivePlayerMusicKit(player, inventory);
        GivePlayerAgent(player, inventory);
        GivePlayerGloves(player, inventory);
        GivePlayerPin(player, inventory);

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo _)
    {
        CCSPlayerController? attacker = @event.Attacker;
        if (!IsPlayerHumanAndValid(attacker) || !IsPlayerPawnValid(attacker))
            return HookResult.Continue;

        CCSPlayerController? victim = @event.Userid;
        if ((StatTrakIgnoreBotsCvar.Value ? !IsPlayerHumanAndValid(victim) : !IsPlayerValid(victim)) || !IsPlayerPawnValid(victim))
            return HookResult.Continue;

        GivePlayerStatTrakIncrease(attacker, @event.Weapon, @event.WeaponItemid);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        CCSPlayerController? player = @event.Userid;
        if (IsPlayerHumanAndValid(player))
        {
            RemovePlayerInventory(player.SteamID);
        }

        PlayerInventoryCleanUp();

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
