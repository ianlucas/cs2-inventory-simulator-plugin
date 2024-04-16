/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API;
using System.Runtime.InteropServices;

namespace InventorySimulator;

[MinimumApiVersion(211)]
public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
    public override string ModuleName => "InventorySimulator";
    public override string ModuleVersion => "1.0.0-beta.20";

    public readonly FakeConVar<bool> StatTrakIgnoreBotsCvar = new("css_stattrak_ignore_bots", "Determines whether to ignore StatTrak increments for bot kills.", true);
    
    public readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    public override void Load(bool hotReload)
    {
        LoadPlayerInventories();

        if (IsWindows)
        {
            // Since the OnGiveNamedItemPost hook doesn't function reliably on Windows, we've opted to use the
            // OnEntityCreated hook instead. This approach should work adequately for standard game modes. However,
            // plugins might encounter compatibility issues if they frequently alter items, as observed in the
            // MatchZy knife round, for example. (See CounterStrikeSharp#377)
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

        RegisterListener<Listeners.OnTick>(OnTick);
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

        GivePlayerWeaponStatTrakIncrease(attacker, @event.Weapon, @event.WeaponItemid);

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo _)
    {
        CCSPlayerController? player = @event.Userid;
        if (!IsPlayerHumanAndValid(player) || !IsPlayerPawnValid(player))
            return HookResult.Continue;

        GivePlayerMusicKitStatTrakIncrease(player);

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

    public void OnTick()
    {
        // Those familiar with the proper method of modification might find amusement in our temporary fix and
        // workaround, which appears to be effective. (However, we're uncertain whether other players can hear
        // the MVP sound, which needs verification.)
        foreach (var player in Utilities.GetPlayers())
            GivePlayerMusicKit(player);
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
