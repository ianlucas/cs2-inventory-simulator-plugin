/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;

namespace InventorySimulator;

public partial class InventorySimulator
{
    [ConsoleCommand("css_ws", "Refreshes player's inventory.")]
    public void OnWSCommand(CCSPlayerController? player, CommandInfo _)
    {
        var url = invsim_ws_print_full_url.Value ? GetApiUrl() : invsim_hostname.Value;
        player?.PrintToChat(Localizer["invsim.announce", url]);

        if (!invsim_ws_enabled.Value || player == null)
            return;
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

        if (FetchingPlayerInventory.ContainsKey(player.SteamID))
        {
            player.PrintToChat(Localizer["invsim.ws_in_progress"]);
            return;
        }

        RefreshPlayerInventory(player, true);
        player.PrintToChat(Localizer["invsim.ws_new"]);
    }

    [ConsoleCommand("css_spray", "Spray player's graffiti.")]
    public void OnSprayCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (player != null && invsim_spray_enabled.Value)
        {
            if (PlayerSprayCooldownManager.TryGetValue(player.SteamID, out var timestamp))
            {
                var cooldown = invsim_spray_cooldown.Value;
                var diff = Now() - timestamp;
                if (diff < cooldown)
                {
                    player.PrintToChat(Localizer["invsim.spray_cooldown", cooldown - diff]);
                    return;
                }
            }

            SprayPlayerGraffiti(player);
        }
    }

    [ConsoleCommand("css_wslogin", "Authenticate player to Inventory Simulator.")]
    public void OnWsloginCommand(CCSPlayerController? player, CommandInfo _)
    {
        if (invsim_apikey.Value != "" && invsim_wslogin.Value && player != null)
        {
            player.PrintToChat(Localizer["invsim.login_in_progress"]);
            if (AuthenticatingPlayer.ContainsKey(player.SteamID))
                return;
            SendSignIn(player.SteamID);
        }
    }
}
