/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public void GivePlayerMusicKit(CCSPlayerController player, PlayerInventory inventory)
    {
        if (!IsPlayerHumanAndValid(player))
            return;
        if (player.InventoryServices == null)
            return;

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
        if (player.InventoryServices == null)
            return;

        var pin = inventory.Pin;
        if (pin == null)
            return;

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

        var fallback = invsim_fallback_team.Value;
        var item = inventory.GetGloves(player.TeamNum, fallback);
        if (item != null)
        {
            if (invsim_ws_gloves_fix.Value)
            {
                // Workaround by @daffyyyy.
                var model = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName;
                if (!string.IsNullOrEmpty(model))
                {
                    pawn.SetModel("characters/models/tm_jumpsuit/tm_jumpsuit_varianta.vmdl");
                    pawn.SetModel(model);
                }
            }

            var glove = pawn.EconGloves;
            Server.NextFrame(() =>
            {
                if (pawn.IsValid)
                {
                    ApplyGloveAttributesFromItem(glove, item);
                    pawn.SetBodygroup("default_gloves", 1);
                }
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
        if (IsCustomWeaponItemID(weapon))
            return;

        var isKnife = weapon.DesignerName.IsKnifeClassName();
        var entityDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
        var inventory = GetPlayerInventory(player);
        var fallback = invsim_fallback_team.Value;
        var item = isKnife ? inventory.GetKnife(player.TeamNum, fallback) : inventory.GetWeapon(player.Team, entityDef, fallback);
        if (item != null)
        {
            item.WearOverride ??= inventory.GetWeaponEconItemWear(item);
            ApplyWeaponAtrributesFromItem(weapon.AttributeManager.Item, item, weapon, player);
        }
    }

    public void GivePlayerWeaponStatTrakIncrement(CCSPlayerController player, string designerName, string weaponItemId)
    {
        try
        {
            var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;

            if (
                weapon == null
                || !IsCustomWeaponItemID(weapon)
                || weapon.FallbackStatTrak < 0
                || weapon.AttributeManager.Item.AccountID != (uint)player.SteamID
                || weapon.AttributeManager.Item.ItemID != ulong.Parse(weaponItemId)
                || weapon.FallbackStatTrak >= 999_999
            )
            {
                return;
            }

            var isKnife = designerName.IsKnifeClassName();
            var newValue = weapon.FallbackStatTrak + 1;
            var def = weapon.AttributeManager.Item.ItemDefinitionIndex;
            weapon.FallbackStatTrak = newValue;
            weapon.AttributeManager.Item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("kill eater", ViewAsFloat(newValue));
            weapon.AttributeManager.Item.AttributeList.SetOrAddAttributeValueByName("kill eater", ViewAsFloat(newValue));
            var inventory = GetPlayerInventory(player);
            var fallback = invsim_fallback_team.Value;
            var item = isKnife ? inventory.GetKnife(player.TeamNum, fallback) : inventory.GetWeapon(player.Team, def, fallback);
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

    public void GivePlayerCurrentWeapons(CCSPlayerController player, PlayerInventory inventory, PlayerInventory oldInventory)
    {
        var pawn = player.PlayerPawn.Value;
        var weaponServices = pawn?.WeaponServices;
        if (pawn == null || weaponServices == null)
            return;
        var activeDesignerName = weaponServices.ActiveWeapon.Value?.DesignerName;
        var targets = new List<(string, string, int, int, bool, gear_slot_t)>();
        foreach (var handle in weaponServices.MyWeapons)
        {
            var weapon = handle.Value?.As<CCSWeaponBase>();
            if (weapon == null || weapon.DesignerName.Contains("weapon_") != true)
                continue;
            if (weapon.OriginalOwnerXuidLow != (uint)player.SteamID)
                continue;
            var data = weapon.VData;
            if (data == null)
                continue;
            if (data.GearSlot is gear_slot_t.GEAR_SLOT_RIFLE or gear_slot_t.GEAR_SLOT_PISTOL or gear_slot_t.GEAR_SLOT_KNIFE)
            {
                var entityDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
                var fallback = invsim_fallback_team.Value;
                var oldItem =
                    data.GearSlot is gear_slot_t.GEAR_SLOT_KNIFE
                        ? oldInventory.GetKnife(player.TeamNum, fallback)
                        : oldInventory.GetWeapon(player.Team, entityDef, fallback);
                var item =
                    data.GearSlot is gear_slot_t.GEAR_SLOT_KNIFE ? inventory.GetKnife(player.TeamNum, fallback) : inventory.GetWeapon(player.Team, entityDef, fallback);

                if (oldItem == item)
                    continue;

                var clip = weapon.Clip1;
                var reserve = weapon.ReserveAmmo[0];

                targets.Add((weapon.DesignerName, weapon.GetDesignerName(), clip, reserve, activeDesignerName == weapon.DesignerName, data.GearSlot));
            }
        }
        foreach (var target in targets)
        {
            var designerName = target.Item1;
            var actualDesignerName = target.Item2;
            var clip = target.Item3;
            var reserve = target.Item4;
            var active = target.Item5;
            var gearSlot = target.Item6;

            var oldWeapon = weaponServices.MyWeapons.FirstOrDefault(h => h.Value?.DesignerName == designerName);
            oldWeapon?.Value?.AddEntityIOEvent("Kill", oldWeapon.Value, null, "", 0.1f);

            var weapon = new CBasePlayerWeapon(player.GiveNamedItem(actualDesignerName));
            Server.RunOnTick(
                Server.TickCount + 32,
                () =>
                {
                    if (weapon.IsValid && pawn.IsValid)
                    {
                        weapon.Clip1 = clip;
                        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_iClip1");
                        weapon.ReserveAmmo[0] = reserve;
                        Utilities.SetStateChanged(weapon, "CBasePlayerWeapon", "m_pReserveAmmo");
                        Server.NextFrame(() =>
                        {
                            if (active && player.IsValid)
                            {
                                var command = gearSlot switch
                                {
                                    gear_slot_t.GEAR_SLOT_RIFLE => "slot1",
                                    gear_slot_t.GEAR_SLOT_PISTOL => "slot2",
                                    gear_slot_t.GEAR_SLOT_KNIFE => "slot3",
                                    _ => null,
                                };
                                if (command != null)
                                    player.ExecuteClientCommand(command);
                            }
                        });
                    }
                }
            );
        }
    }

    public void GiveOnPlayerSpawn(CCSPlayerController player)
    {
        var inventory = GetPlayerInventory(player);
        GivePlayerPin(player, inventory);
        GivePlayerAgent(player, inventory);
        GivePlayerGloves(player, inventory);
    }

    public void GiveOnLoadPlayerInventory(CCSPlayerController player)
    {
        GiveTeamPreviewItems("team_select", player);
        GiveTeamPreviewItems("team_intro", player);
    }

    public void GiveOnRefreshPlayerInventory(CCSPlayerController player, PlayerInventory oldInventory)
    {
        var inventory = GetPlayerInventory(player);
        GivePlayerPin(player, inventory);
        if (invsim_ws_immediately.Value)
        {
            GivePlayerGloves(player, inventory);
            GivePlayerCurrentWeapons(player, inventory, oldInventory);
        }
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

    public unsafe void SprayPlayerGraffiti(CCSPlayerController player)
    {
        if (!IsPlayerHumanAndValid(player))
            return;
        var inventory = GetPlayerInventory(player);
        var item = inventory.Graffiti;
        if (item == null)
            return;
        var pawn = player.PlayerPawn.Value;
        if (pawn == null || pawn.LifeState != (int)LifeState_t.LIFE_ALIVE)
            return;
        var movementServices = pawn.MovementServices?.As<CCSPlayer_MovementServices>();
        if (movementServices == null)
            return;
        var trace = stackalloc GameTrace[1];
        if (!pawn.IsAbleToApplySpray((IntPtr)trace) || (IntPtr)trace == IntPtr.Zero)
            return;
        player.EmitSound("SprayCan.Shake");
        PlayerSprayCooldownManager[player.SteamID] = Now();
        var endPos = Vector3toVector(trace->EndPos);
        var normalPos = Vector3toVector(trace->Normal);
        var sprayDecal = Utilities.CreateEntityByName<CPlayerSprayDecal>("player_spray_decal");
        if (sprayDecal != null)
        {
            sprayDecal.EndPos.Add(endPos);
            sprayDecal.Start.Add(endPos);
            sprayDecal.Left.Add(movementServices.Left);
            sprayDecal.Normal.Add(normalPos);
            sprayDecal.AccountID = (uint)player.SteamID;
            sprayDecal.Player = item.Def;
            sprayDecal.TintID = item.Tint;
            sprayDecal.DispatchSpawn();
            player.EmitSound("SprayCan.Paint");
        }
    }

    public void GiveTeamPreviewItems(string prefix, CCSPlayerController? player = null)
    {
        var teamPreviews = Utilities
            .FindAllEntitiesByDesignerName<CCSGO_TeamPreviewCharacterPosition>($"{prefix}_counterterrorist")
            .Concat(Utilities.FindAllEntitiesByDesignerName<CCSGO_TeamPreviewCharacterPosition>($"{prefix}_terrorist"));

        foreach (var teamPreview in teamPreviews)
        {
            if (teamPreview.Xuid == 0 || (player != null && player.SteamID != teamPreview.Xuid))
                continue;
            player ??= Utilities.GetPlayerFromSteamId(teamPreview.Xuid);
            if (player == null || !IsPlayerHumanAndValid(player))
                continue;
            var inventory = GetPlayerInventory(player);
            GivePlayerTeamPreview(player, teamPreview, inventory);
        }
    }

    public void GivePlayerTeamPreview(CCSPlayerController player, CCSGO_TeamPreviewCharacterPosition teamPreview, PlayerInventory inventory)
    {
        var fallback = invsim_fallback_team.Value;

        var gloveItem = inventory.GetGloves(player.TeamNum, fallback);
        if (gloveItem != null)
        {
            ApplyGloveAttributesFromItem(teamPreview.GlovesItem, gloveItem);
            Utilities.SetStateChanged(teamPreview, "CCSGO_TeamPreviewCharacterPosition", "m_glovesItem");
        }

        var weaponItem = teamPreview.WeaponItem.IsKnifeClassName()
            ? inventory.GetKnife(player.TeamNum, fallback)
            : inventory.GetWeapon(player.Team, teamPreview.WeaponItem.ItemDefinitionIndex, fallback);
        if (weaponItem != null)
        {
            ApplyWeaponAtrributesFromItem(teamPreview.WeaponItem, weaponItem);
            Utilities.SetStateChanged(teamPreview, "CCSGO_TeamPreviewCharacterPosition", "m_weaponItem");
        }

        if (inventory.Agents.TryGetValue(player.TeamNum, out var agentItem) && agentItem.Def != null)
        {
            teamPreview.AgentItem.ItemDefinitionIndex = agentItem.Def.Value;
            Utilities.SetStateChanged(teamPreview, "CCSGO_TeamPreviewCharacterPosition", "m_agentItem");
        }
    }
}
