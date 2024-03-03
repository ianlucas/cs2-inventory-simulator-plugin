/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Microsoft.Extensions.Logging;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public void GivePlayerMusicKit(CCSPlayerController player)
    {
        if (player.InventoryServices == null) return;
        var inventory = GetPlayerInventory(player);
        if (!inventory.HasProperty("mk")) return;
        player.InventoryServices.MusicID = inventory.GetUShort("mk");
    }

    public void GivePlayerGloves(CCSPlayerController player)
    {
        var inventory = GetPlayerInventory(player);

        var team = player.TeamNum;
        if (!inventory.HasProperty("gl", team)) return;

        var itemDef = inventory.GetUShort("gl", team);
        if (!inventory.HasProperty("pa", team, itemDef)) return;

        var glove = player.PlayerPawn.Value!.EconGloves;
        glove.ItemDefinitionIndex = itemDef;
        glove.ItemIDLow = 16384 & 0xFFFFFFFF;
        glove.ItemIDHigh = 16384 >> 32;

        Server.NextFrame(() =>
        {
            glove.Initialized = true;
            SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture prefab", inventory.GetInt("pa", team, itemDef, 0));
            SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture seed", inventory.GetInt("se", team, itemDef, 1));
            SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture wear", inventory.GetFloat("fl", team, itemDef, 1));
            SetBodygroup(player, "default_gloves");
        });
    }

    public void GivePlayerAgent(CCSPlayerController player)
    {
        var team = player.TeamNum;
        var inventory = GetPlayerInventory(player);
        var model = inventory.GetString("agm", team);

        if (model == null) return;

        try
        {
            Server.NextFrame(() =>
            {
                player.PlayerPawn.Value!.SetModel(
                    GetAgentModelPath(model)
                );
            });
        }
        catch (Exception)
        {
            Logger.LogInformation($"Could not set player model for {player.PlayerName}");
        }
    }
}
