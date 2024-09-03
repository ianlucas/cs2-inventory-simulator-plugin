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
    public void GivePlayerMusicKit(CCSPlayerController player, PlayerInventory inventory)
    {
        if (!IsPlayerHumanAndValid(player)) return;
        if (player.InventoryServices == null) return;

        var item = inventory.MusicKit;
        if (item != null)
        {
            player.InventoryServices.MusicID = (ushort)item.Def;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
            player.MusicKitID = item.Def;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMusicKitID");
            player.MusicKitMVPs = item.Stattrak;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_iMusicKitMVPs");
        }
    }

    public void GivePlayerPin(CCSPlayerController player, PlayerInventory inventory)
    {
        if (player.InventoryServices == null) return;

        var pin = inventory.Pin;
        if (pin == null) return;

        for (var index = 0; index < player.InventoryServices.Rank.Length; index++)
        {
            player.InventoryServices.Rank[index] = index == 5 ? (MedalRank_t)pin.Value : MedalRank_t.MEDAL_RANK_NONE;
            Utilities.SetStateChanged(player, "CCSPlayerController", "m_pInventoryServices");
        }
    }

    public void GivePlayerGloves(CCSPlayerController player, PlayerInventory inventory)
    {
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || pawn.Handle == IntPtr.Zero)
            return;

        if (inventory.Gloves.TryGetValue(player.TeamNum, out var item))
        {
            var glove = pawn.EconGloves;
            Server.NextFrame(() =>
            {
                glove.Initialized = true;
                glove.ItemDefinitionIndex = item.Def;
                UpdateEconItemID(glove);

                glove.NetworkedDynamicAttributes.Attributes.RemoveAll();
                glove.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture prefab", item.Paint);
                glove.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture seed", item.Seed);
                glove.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture wear", item.Wear);

                glove.AttributeList.Attributes.RemoveAll();
                glove.AttributeList.SetOrAddAttributeValueByName("set item texture prefab", item.Paint);
                glove.AttributeList.SetOrAddAttributeValueByName("set item texture seed", item.Seed);
                glove.AttributeList.SetOrAddAttributeValueByName("set item texture wear", item.Wear);

                pawn.SetBodygroup("default_gloves", 1);
            });
        }
    }

    public void GivePlayerAgent(CCSPlayerController player, PlayerInventory inventory)
    {
        if (invsim_minmodels.Value > 0)
        {
            // For now any value non-zero will force SAS & Phoenix.
            // In the future: 1 - Map agents only, 2 - SAS & Phoenix.
            if (player.Team == CsTeam.Terrorist)
                SetPlayerModel(player, "characters/models/tm_phoenix/tm_phoenix.vmdl");

            if (player.Team == CsTeam.CounterTerrorist)
                SetPlayerModel(player, "characters/models/ctm_sas/ctm_sas.vmdl");

            return;
        }

        if (inventory.Agents.TryGetValue(player.TeamNum, out var item))
        {
            var patches = item.Patches.Count != 5 ? Enumerable.Repeat((uint)0, 5).ToList() : item.Patches;
            SetPlayerModel(player, GetAgentModelPath(item.Model), item.VoFallback, item.VoPrefix, item.VoFemale, patches);
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
                weapon.ChangeSubclass(item.Def);
            }
            weapon.AttributeManager.Item.ItemDefinitionIndex = item.Def;
            weapon.AttributeManager.Item.EntityQuality = 3;
        }
        else
        {
            weapon.AttributeManager.Item.EntityQuality = item.Stattrak >= 0 ? 9 : 0;
        }

        UpdateEconItemID(weapon.AttributeManager.Item);

        // After Fire Sale update (2024-05-23) we have a really weird issue with Fade skins. I happaned to find
        // this because I had a Glock-18 | Fade equipped as a T and a Flip Knife | Fade as a CT: the first
        // spawn as a T would have the skin applied to the Glock, however if I switched to CT my knife would
        // not have the skin applied. Likewise, if I first spawn as CT, the Glock would not have the skin
        // applied if I switched. I only spotted this behavior with Fade skins, but it may happen for other
        // paint kits, but I'm going to apply this wonky workaround only for Fade skins. If people face issues
        // with other skins we may need to generalize this to all skins. Thanks @bklol for the insight.
        // If this happens to be a Valve bug, then we need to check every update if it was fixed.
        if (item.Paint == 38 && item.FadeSeed == null)
        {
            // Here's what I found: if FallbackSeed < 3, then no skin will be applied when switching team even if
            // we still increment NextFadeSeed (0, 1, 2). If a fade skin has the same seed as other fade skin it
            // won't apply, so that's why we're incrementing NextFadeSeed here.
            // Let me know if you find the real issue and fix (if any).
            item.FadeSeed = NextFadeSeed++;
        }

        weapon.FallbackPaintKit = item.Paint;
        weapon.FallbackSeed = item.FadeSeed != null ? item.FadeSeed.Value : item.Seed;
        weapon.FallbackWear = item.Wear;
        weapon.AttributeManager.Item.CustomName = item.Nametag;
        weapon.AttributeManager.Item.AccountID = (uint)player.SteamID;

        weapon.AttributeManager.Item.NetworkedDynamicAttributes.Attributes.RemoveAll();
        weapon.AttributeManager.Item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture prefab", item.Paint);
        weapon.AttributeManager.Item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture seed", item.Seed);
        weapon.AttributeManager.Item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture wear", item.Wear);

        weapon.AttributeManager.Item.AttributeList.Attributes.RemoveAll();
        weapon.AttributeManager.Item.AttributeList.SetOrAddAttributeValueByName("set item texture prefab", item.Paint);
        weapon.AttributeManager.Item.AttributeList.SetOrAddAttributeValueByName("set item texture seed", item.Seed);
        weapon.AttributeManager.Item.AttributeList.SetOrAddAttributeValueByName("set item texture wear", item.Wear);

        if (item.Stattrak >= 0)
        {
            weapon.FallbackStatTrak = item.Stattrak;
            weapon.AttributeManager.Item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("kill eater", ViewAsFloat(item.Stattrak));
            weapon.AttributeManager.Item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("kill eater score type", 0);
            weapon.AttributeManager.Item.AttributeList.SetOrAddAttributeValueByName("kill eater", ViewAsFloat(item.Stattrak));
            weapon.AttributeManager.Item.AttributeList.SetOrAddAttributeValueByName("kill eater score type", 0);
        }

        if (!isKnife)
        {
            foreach (var sticker in item.Stickers)
            {
                // To set the ID of the sticker, we need to use a workaround. In the items_game.txt file, locate the
                // sticker slot 0 id entry. It should be marked with stored_as_integer set to 1. This means we need to
                // treat a uint as a float. For example, if the uint stickerId is 2229, we would interpret its value as
                // if it were a float (e.g., float stickerId = 3.12349e-42f).
                // @see https://gitlab.com/KittenPopo/csgo-2018-source/-/blame/main/game/shared/econ/econ_item_view.cpp#L194
                weapon.AttributeManager.Item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName($"sticker slot {sticker.Slot} id", ViewAsFloat(sticker.Def));
                weapon.AttributeManager.Item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName($"sticker slot {sticker.Slot} wear", sticker.Wear);
            }
            UpdatePlayerWeaponMeshGroupMask(player, weapon, item.Legacy);
        }
    }

    public void GivePlayerWeaponStatTrakIncrement(
        CCSPlayerController player,
        string designerName,
        string weaponItemId)
    {
        try
        {
            var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            if (
                weapon == null ||
                !IsCustomWeaponItemID(weapon) ||
                weapon.FallbackStatTrak < 0 ||
                weapon.AttributeManager.Item.AccountID != (uint)player.SteamID ||
                weapon.AttributeManager.Item.ItemID != ulong.Parse(weaponItemId) ||
                weapon.FallbackStatTrak >= 999_999)
            {
                return;
            }

            var isKnife = IsKnifeClassName(designerName);
            var newValue = weapon.FallbackStatTrak + 1;
            var def = weapon.AttributeManager.Item.ItemDefinitionIndex;
            weapon.FallbackStatTrak = newValue;
            weapon.AttributeManager.Item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("kill eater", ViewAsFloat(newValue));
            weapon.AttributeManager.Item.AttributeList.SetOrAddAttributeValueByName("kill eater", ViewAsFloat(newValue));
            var inventory = GetPlayerInventory(player);
            var item = isKnife ? inventory.GetKnife(player.TeamNum) : inventory.GetWeapon(player.Team, def);
            if (item != null)
            {
                item.Stattrak = newValue;
                SendStatTrakIncrement(player.SteamID, item.Uid);
            }
        }
        catch
        {
            // Ignore any errors.
        }
    }

    public void GivePlayerMusicKitStatTrakIncrement(CCSPlayerController player)
    {
        if (PlayerInventoryManager.TryGetValue(player.SteamID, out var inventory))
        {
            var item = inventory.MusicKit;
            if (item != null)
            {
                item.Stattrak += 1;
                SendStatTrakIncrement(player.SteamID, item.Uid);
            }
        }
    }

    public void GiveOnPlayerSpawn(CCSPlayerController player)
    {
        var inventory = GetPlayerInventory(player);
        GivePlayerPin(player, inventory);
        GivePlayerAgent(player, inventory);
        GivePlayerGloves(player, inventory);
    }

    public void GiveOnRefreshPlayerInventory(CCSPlayerController player)
    {
        var inventory = GetPlayerInventory(player);
        GivePlayerPin(player, inventory);
    }

    public void GivePlayerGraffiti(CCSPlayerController player, CPlayerSprayDecal sprayDecal)
    {
        var inventory = GetPlayerInventory(player);
        var item = inventory.Graffiti;

        if (item != null)
        {
            sprayDecal.Player = item.Def;
            Utilities.SetStateChanged(sprayDecal, "CPlayerSprayDecal", "m_nPlayer");
            sprayDecal.TintID = item.Tint;
            Utilities.SetStateChanged(sprayDecal, "CPlayerSprayDecal", "m_nTintID");
        }
    }

    public void SprayPlayerGraffiti(CCSPlayerController player)
    {
        if (!IsPlayerHumanAndValid(player)) return;
        var inventory = GetPlayerInventory(player);
        var item = inventory.Graffiti;
        if (item == null) return;
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || pawn.LifeState != (int)LifeState_t.LIFE_ALIVE) return;
        var absOrigin = pawn.AbsOrigin;
        var cameraServices = pawn.CameraServices;
        var movimentServices = pawn.MovementServices?.As<CCSPlayer_MovementServices>();
        if (absOrigin == null || cameraServices == null || movimentServices == null) return;
        if (!pawn.IsAbleToApplySpray()) return;
        player.ExecuteClientCommand("play sounds/items/spraycan_shake");
        PlayerSprayCooldownManager[player.SteamID] = Now();
        var trace = GameTraceManager.Trace(
            new Vector(absOrigin.X, absOrigin.Y, absOrigin.Z + cameraServices.OldPlayerViewOffsetZ),
            pawn.EyeAngles,
            false,
            true);
        if (trace != null)
        {
            var endPos = trace.Value.Item1;
            var normalPos = trace.Value.Item2;
            var sprayDecal = Utilities.CreateEntityByName<CPlayerSprayDecal>("player_spray_decal");
            if (sprayDecal != null)
            {
                sprayDecal.EndPos.Add(endPos);
                sprayDecal.Start.Add(endPos);
                sprayDecal.Left.Add(movimentServices.Left);
                sprayDecal.Normal.Add(normalPos);
                sprayDecal.AccountID = (uint)player.SteamID;
                sprayDecal.Player = item.Def;
                sprayDecal.TintID = item.Tint;
                sprayDecal.DispatchSpawn();
                player.ExecuteClientCommand("play sounds/items/spraycan_spray");
            }
        }
    }
}
