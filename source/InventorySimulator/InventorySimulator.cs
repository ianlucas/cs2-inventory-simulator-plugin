/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API;
using System.Runtime.InteropServices;

namespace InventorySimulator;

[MinimumApiVersion(227)]
public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
    public override string ModuleName => "InventorySimulator";
    public override string ModuleVersion => "1.0.0-beta.24";

    public readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public readonly Dictionary<ulong, long> PlayerCooldownManager = new();

    public readonly FakeConVar<bool> invsim_stattrak_ignore_bots = new("invsim_stattrak_ignore_bots", "Whether to ignore StatTrak increments for bot kills.", true);
    public readonly FakeConVar<bool> invsim_ws_enabled = new("invsim_ws_enabled", "Whether players can refresh their inventory using !ws.", false);
    public readonly FakeConVar<int> invsim_ws_cooldown = new("invsim_ws_cooldown", "Cooldown in seconds between player inventory refreshes.", 30);

    public override void Load(bool hotReload)
    {
        LoadPlayerInventories();

        if (!IsWindows)
        {
            // GiveNamedItemFunc hooking is not working on Windows due an issue with CounterStrikeSharp's
            // DynamicHooks. See: https://github.com/roflmuffin/CounterStrikeSharp/issues/377
            VirtualFunctions.GiveNamedItemFunc.Hook(OnGiveNamedItemPost, HookMode.Post);
        }

        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.OnEntityCreated>(OnEntityCreated);
    }

    [GameEventHandler]
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null && IsPlayerHumanAndValid(player))
        {
            RefreshPlayerInventory(player);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null && IsPlayerHumanAndValid(player))
        {
            RefreshPlayerInventory(player);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null &&
            IsPlayerHumanAndValid(player) &&
            IsPlayerPawnValid(player))
        {
            GiveOnPlayerSpawn(player);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnItemPickup(EventItemPickup @event, GameEventInfo _)
    {
        if (IsWindows)
        {
            var player = @event.Userid;
            if (player != null &&
                IsPlayerHumanAndValid(player) &&
                IsPlayerPawnValid(player))
            {
                GiveOnItemPickup(player);
            }
        }

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo _)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        if (attacker != null && victim != null)
        {
            var isValidAttacker = IsPlayerHumanAndValid(attacker) && !IsPlayerPawnValid(attacker);
            var isValidVictim = (
                invsim_stattrak_ignore_bots.Value
                    ? IsPlayerHumanAndValid(victim)
                    : IsPlayerValid(victim)) &&
                IsPlayerPawnValid(victim);
            if (isValidAttacker && isValidVictim)
            {
                GivePlayerWeaponStatTrakIncrease(attacker, @event.Weapon, @event.WeaponItemid);
            }
        }

        return HookResult.Continue;
    }

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnRoundMvp(EventRoundMvp @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null &&
            IsPlayerHumanAndValid(player) &&
            IsPlayerPawnValid(player))
        {
            GivePlayerMusicKitStatTrakIncrease(player);
        }

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null && IsPlayerHumanAndValid(player))
        {
            RemovePlayerInventory(player.SteamID);
            ClearInventoryManager();
        }

        return HookResult.Continue;
    }

    public void OnTick()
    {
        // According to @bklol the right way to change the Music Kit is to update the player's inventory, I'm
        // pretty sure that's the best way to change anything inventory-related, but that's not something
        // public and we brute force the setting of the Music Kit here.
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

    [ConsoleCommand("css_ws", "Refreshes player's inventory.")]
    public void OnCommandWS(CCSPlayerController? player, CommandInfo _)
    {
        player?.PrintToChat(Localizer["invsim.announce", GetApiUrl()]);

        if (!invsim_ws_enabled.Value || player == null) return;
        if (PlayerCooldownManager.TryGetValue(player.SteamID, out var timestamp))
        {
            var cooldown = invsim_ws_cooldown.Value;
            var diff = Now() - timestamp;
            if (diff < cooldown)
            {
                player.PrintToChat(Localizer["invsim.ws_cooldown", cooldown - diff]);
                return;
            }
        }

        if (FetchingPlayerInventory.Contains(player.SteamID))
        {
            player.PrintToChat(Localizer["invsim.ws_in_progress"]);
            return;
        }

        RefreshPlayerInventory(player, true);
        player.PrintToChat(Localizer["invsim.ws_new"]);
    }
}
