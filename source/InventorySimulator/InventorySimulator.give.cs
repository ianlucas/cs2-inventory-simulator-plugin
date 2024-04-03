/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CounterStrikeSharp.API;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public void GivePlayerMusicKit(CCSPlayerController player)
    {
        if (player.InventoryServices == null) return;
        
        var musicId = GetPlayerInventory(player).MusicKit;
        if (musicId == null) return;

        player.InventoryServices.MusicID = musicId.Value;
    }

    public void GivePlayerPin(CCSPlayerController player)
    {
        if (player.InventoryServices == null) return;

        var pin = GetPlayerInventory(player).Pin;
        if (pin == null) return;

        for (var index = 0; index < player.InventoryServices.Rank.Length; index++)
        {
            player.InventoryServices.Rank[index] = index == 5 ? (MedalRank_t)pin.Value : MedalRank_t.MEDAL_RANK_NONE;
        }
    }

    public void GivePlayerGloves(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value!.Handle == IntPtr.Zero)
        {
            // Some plugin or specific game scenario is throwing exceptions at this point whenever we try to access
            // any member from the Player Pawn. We perform the actual check that triggers the exception. (I've
            // tried catching it in the past, but it seems it won't work...)
            return;
        }

        if (GetPlayerInventory(player).Gloves.TryGetValue(player.TeamNum, out var item))
        {
            var glove = player.PlayerPawn.Value.EconGloves;
            Server.NextFrame(() =>
            {
                glove.Initialized = true;
                glove.ItemDefinitionIndex = item.Def;
                UpdatePlayerEconItemID(glove);
                glove.NetworkedDynamicAttributes.Attributes.RemoveAll();
                glove.AttributeList.Attributes.RemoveAll();
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture prefab", item.Paint);
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture seed", item.Seed);
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture wear", item.Wear);
                // We also need to update AttributeList to overwrite owned glove attributes.
                SetOrAddAttributeValueByName(glove.AttributeList, "set item texture prefab", item.Paint);
                SetOrAddAttributeValueByName(glove.AttributeList, "set item texture seed", item.Seed);
                SetOrAddAttributeValueByName(glove.AttributeList, "set item texture wear", item.Wear);
                SetBodygroup(player, "default_gloves");
            });
        }
    }

    public void GivePlayerAgent(CCSPlayerController player)
    {
        if (MinModelsCvar.Value > 0)
        {
            // For now any value non-zero will force SAS & Phoenix.
            // In the future: 1 - Map agents only, 2 - SAS & Phoenix.
            if (player.Team == CsTeam.Terrorist)
                SetPlayerModel(player, "characters/models/tm_phoenix/tm_phoenix.vmdl");

            if (player.Team == CsTeam.CounterTerrorist)
                SetPlayerModel(player, "characters/models/ctm_sas/ctm_sas.vmdl");

            return;
        }

        if (GetPlayerInventory(player).Agents.TryGetValue(player.TeamNum, out var item))
        {
            var patches = item.Patches.Count != 5 ? Enumerable.Repeat((uint)0, 5).ToList() : item.Patches;
            SetPlayerModel(player, GetAgentModelPath(item.Model), patches);
        }
    }

    public void GivePlayerWeaponSkin(CCSPlayerController player, CBasePlayerWeapon weapon)
    {
        if (IsCustomWeaponItemID(weapon)) return;

        var isKnife = IsKnifeClassName(weapon.DesignerName);
        var entityDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
        var inventory = GetPlayerInventory(player);
        var item = isKnife ? inventory.GetKnife(player.TeamNum) : inventory.GetWeapon(player.Team, entityDef);
        if (item == null) return;

        if (isKnife)
        {
            if (entityDef != item.Def)
            {
                SubclassChange(weapon, item.Def);
            }
            weapon.AttributeManager.Item.ItemDefinitionIndex = item.Def;
            weapon.AttributeManager.Item.EntityQuality = 3;
        }

        UpdatePlayerEconItemID(weapon.AttributeManager.Item);

        weapon.FallbackPaintKit = item.Paint;
        weapon.FallbackSeed = item.Seed;
        weapon.FallbackWear = item.Wear;
        weapon.FallbackStatTrak = item.Stattrak;
        weapon.AttributeManager.Item.CustomName = item.Nametag;

        weapon.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, "set item texture prefab", item.Paint);
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, "set item texture seed", item.Seed);
        SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, "set item texture wear", item.Wear);

        if (!isKnife)
        {
            foreach (var sticker in item.Stickers)
            {
                // To set the ID of the sticker, we need to use a workaround. In the items_game.txt file, locate the
                // sticker slot 0 id entry. It should be marked with stored_as_integer set to 1. This means we need to
                // treat a uint as a float. For example, if the uint stickerId is 2229, we would interpret its value as
                // if it were a float (e.g., float stickerId = 3.12349e-42f).
                // @see https://gitlab.com/KittenPopo/csgo-2018-source/-/blame/main/game/shared/econ/econ_item_view.cpp#L194
                SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, $"sticker slot {sticker.Slot} id", ViewUintAsFloat(sticker.Def));
                SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, $"sticker slot {sticker.Slot} wear", sticker.Wear);
            }
            UpdatePlayerWeaponMeshGroupMask(player, weapon, item.Legacy);
        }
    }
}
