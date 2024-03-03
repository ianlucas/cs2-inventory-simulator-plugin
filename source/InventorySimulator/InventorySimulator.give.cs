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
        UpdateItemID(glove);

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

    public void GivePlayerWeaponSkin(CCSPlayerController player, CBasePlayerWeapon weapon)
    {
        var isKnife = IsKnifeClassName(weapon.DesignerName);
        var inventory = GetPlayerInventory(player);
        var itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
        var team = player.TeamNum;
        var hasKnife = inventory.HasProperty("me", team);

        if (isKnife && !hasKnife) return;

        var hasNametag = inventory.HasProperty("nt", team, itemDef);
        var hasPaintKit = inventory.HasProperty("pa", team, itemDef);
        var hasStickers = inventory.HasProperty("ss", team, itemDef);
        var hasWear = inventory.HasProperty("fl", team, itemDef);
        var hasSeed = inventory.HasProperty("se", team, itemDef);
        var hasStatTrak = inventory.HasProperty("st", team, itemDef);
        var isCustomItem = hasKnife || hasPaintKit || hasNametag || hasStickers || hasWear || hasSeed || hasStatTrak;

        if (!isKnife && !isCustomItem) return;

        if (isKnife)
        {
            itemDef = inventory.GetUShort("me", team);
            if (weapon.AttributeManager.Item.ItemDefinitionIndex != itemDef)
            {
                SubclassChange(weapon, itemDef);
            }
            weapon.AttributeManager.Item.ItemDefinitionIndex = itemDef;
            weapon.AttributeManager.Item.EntityQuality = 3;
        }

        UpdateItemID(weapon.AttributeManager.Item);

        var paintKit = inventory.GetInt("pa", team, itemDef, 0);
        weapon.FallbackPaintKit = paintKit;
        weapon.FallbackSeed = inventory.GetInt("se", team, itemDef, 1);
        weapon.FallbackWear = inventory.GetFloat("fl", team, itemDef, 0.0f);
        weapon.FallbackStatTrak = inventory.GetInt("st", team, itemDef, -1);
        weapon.AttributeManager.Item.CustomName = inventory.GetString("nt", team, itemDef, "");

        // This APPEARS to fix the issue where sometimes the skin name won't be displayed on HUD.
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, "set item texture prefab", paintKit);

        if (!isKnife)
        {
            UpdateWeaponModel(weapon, inventory.HasProperty("pal", team, itemDef));
        }
    }
}
