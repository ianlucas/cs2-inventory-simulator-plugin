/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public HookResult OnPlayerConnect(EventPlayerConnect @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null && IsPlayerHumanAndValid(player))
        {
            if (PlayerOnTickInventoryManager.TryGetValue(player.SteamID, out var tuple))
            {
                PlayerOnTickInventoryManager[player.SteamID] = (player, tuple.Item2);
            }
            RefreshPlayerInventory(player);
        }

        return HookResult.Continue;
    }

    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null && IsPlayerHumanAndValid(player))
        {
            RefreshPlayerInventory(player);
        }

        return HookResult.Continue;
    }

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

    public HookResult OnPlayerDeathPre(EventPlayerDeath @event, GameEventInfo _)
    {
        var attacker = @event.Attacker;
        var victim = @event.Userid;
        if (attacker != null && victim != null)
        {
            var isValidAttacker = (
                IsPlayerHumanAndValid(attacker) &&
                IsPlayerPawnValid(attacker));
            var isValidVictim = (
                Config.Invsim_stattrak_ignore_bots
                    ? IsPlayerHumanAndValid(victim)
                    : IsPlayerValid(victim)) &&
                IsPlayerPawnValid(victim);
            if (isValidAttacker && isValidVictim)
            {
                GivePlayerWeaponStatTrakIncrement(attacker, @event.Weapon, @event.WeaponItemid);
            }
        }

        return HookResult.Continue;
    }

    public HookResult OnRoundMvpPre(EventRoundMvp @event, GameEventInfo _)
    {
        var player = @event.Userid;
        if (player != null &&
            IsPlayerHumanAndValid(player) &&
            IsPlayerPawnValid(player))
        {
            GivePlayerMusicKitStatTrakIncrement(player);
        }

        return HookResult.Continue;
    }

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
}
