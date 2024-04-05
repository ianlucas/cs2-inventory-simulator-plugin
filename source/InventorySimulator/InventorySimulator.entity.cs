/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;

namespace InventorySimulator;

public partial class InventorySimulator
{

    public void UpdateWeaponMeshGroupMask(CBaseEntity weapon, bool isLegacy)
    {
        if (weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
        {
            var skeleton = weapon.CBodyComponent.SceneNode.GetSkeletonInstance();
            if (skeleton != null)
            {
                // The term "Legacy" refers to the old weapon models from CS:GO. In CS2, the MeshGroupMask is used to
                // determine whether the game should display the legacy model (value = 2) or the new model (value = 1).
                var value = (ulong)(isLegacy ? 2 : 1);
                if (skeleton.ModelState.MeshGroupMask != value)
                {
                    skeleton.ModelState.MeshGroupMask = value;
                }
            }
        }
    }

    public void UpdatePlayerWeaponMeshGroupMask(CCSPlayerController player, CBasePlayerWeapon weapon, bool isLegacy)
    {
        // 1. We update the weapon's MeshGroupMask.
        UpdateWeaponMeshGroupMask(weapon, isLegacy);

        // 2. If the current view model is displaying it, ensure that it has the correct MeshGroupMask.
        var viewModel = GetPlayerViewModel(player);
        if (viewModel != null && viewModel.Weapon.Value != null && viewModel.Weapon.Value.Index == weapon.Index)
        {
            UpdateWeaponMeshGroupMask(viewModel, isLegacy);
            Utilities.SetStateChanged(viewModel, "CBaseEntity", "m_CBodyComponent");
        }
    }

    public void UpdatePlayerEconItemID(CEconItemView econItemView)
    {
        // Alright, so the ItemID serves as a global identifier for items. Since we're simulating it, we're
        // using arbitrary large numbers.
        var itemId = NextItemId++;
        econItemView.ItemID = itemId;

        // @see https://gitlab.com/KittenPopo/csgo-2018-source/-/blob/main/game/shared/econ/econ_item_view.h#L313
        econItemView.ItemIDLow = (uint)itemId & 0xFFFFFFFF;
        econItemView.ItemIDHigh = (uint)itemId >> 32;
    }

    public bool IsCustomWeaponItemID(CBasePlayerWeapon weapon)
    {
        return weapon.AttributeManager.Item.ItemID >= MinimumCustomItemID;
    }

    public void SetPlayerModel(CCSPlayerController player, string model, List<uint>? patches = null)
    {
        try
        {
            Server.NextFrame(() =>
            {
                if (patches != null && patches.Count == 5)
                {
                    for (var index = 0; index < patches.Count; index++)
                    {
                        player.PlayerPawn.Value!.PlayerPatchEconIndices[index] = patches[index];
                    }
                }
                player.PlayerPawn.Value!.SetModel(model);

            });
        }
        catch
        {
            // Ignore any error.
        }
    }

    public CCSPlayerController? GetPlayerFromItemServices(CCSPlayer_ItemServices itemServices)
    {
        var pawn = itemServices.Pawn.Value;
        if (pawn == null || !pawn.IsValid || !pawn.Controller.IsValid || pawn.Controller.Value == null) return null;
        var player = new CCSPlayerController(pawn.Controller.Value.Handle);
        if (!IsPlayerHumanAndValid(player)) return null;
        return player;
    }

    public bool IsPlayerValid(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsHLTV;
    }

    public bool IsPlayerHumanAndValid(CCSPlayerController? player)
    {
        return IsPlayerValid(player) && !player!.IsBot;
    }

    public bool IsPlayerPawnValid(CCSPlayerController player)
    {
        return player.PlayerPawn != null && player.PlayerPawn.Value != null && player.PlayerPawn.IsValid;
    }
}
