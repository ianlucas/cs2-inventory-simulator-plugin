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
    public void GiveMusicKit(CCSPlayerController player)
    {
        if (player.InventoryServices == null) return;
        var inventory = GetPlayerInventory(player);
        if (!inventory.HasProperty("mk")) return;
        player.InventoryServices.MusicID = inventory.GetUShort("mk");
    }

    public void GiveGloves(CCSPlayerController player)
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
            SetBodygroup(player, "default_gloves", 1);
        });
    }

    public void GiveKnife(CCSPlayerController player)
    {
        if (HasKnife(player))
        {
            return;
        }

        var inventory = GetPlayerInventory(player);
        var team = player.TeamNum;
        // On Windows we cannot give knives using GiveNamedItems, still no
        // explanation from a C++/RE expert. We could use subclass_change, but
        // from my testing it'd require a full client update to show the skin.
        // Until someone figure this out, on Windows we force the knife on the
        // viewmodel.
        if (g_IsWindows || !inventory.HasProperty("me", team))
        {
            var suffix = team == 2 ? "_t" : "";
            player.GiveNamedItem($"weapon_knife{suffix}");
            return;
        }

        var model = GetItemDefModel(inventory.GetUShort("me", team));
        if (model != null)
        {
            player.GiveNamedItem(model);
        }
    }

    public void GiveAgent(CCSPlayerController player)
    {
        var team = player.TeamNum;
        var inventory = GetPlayerInventory(player);
        if (!inventory.HasProperty("ag", team)) return;

        try
        {
            var model = GetAgentModel(inventory.GetUShort("ag", team));
            if (model != null)
            {
                Server.NextFrame(() =>
                {
                    player.PlayerPawn.Value!.SetModel(model);
                });
            }
        }
        catch (Exception)
        {
            Logger.LogInformation($"Could not set player model for {player.PlayerName}");
        }
    }
}
