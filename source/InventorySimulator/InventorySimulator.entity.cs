/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

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
                // Legacy refer to the old weapon models from CS:GO, in CS2 `MeshGroupMask` defines
                // if the game should display the legacy (=2) or new model (=1).
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
        // 1. We update the weapon's mesh group mask.
        UpdateWeaponMeshGroupMask(weapon, isLegacy);

        // 2. If the current view model is showing it up, make sure it has the correct mesh group mask.
        var viewModel = GetPlayerViewModel(player);
        if (viewModel != null && viewModel.Weapon.Value != null && viewModel.Weapon.Value.Index == weapon.Index)
        {
            UpdateWeaponMeshGroupMask(viewModel, isLegacy);
            Utilities.SetStateChanged(viewModel, "CBaseEntity", "m_CBodyComponent");
        }
    }

    public void UpdatePlayerEconItemID(CEconItemView econItemView)
    {
        // Okay, so ItemID appears to be a global identification of the item. Since we're
        // faking it, we are using some arbitrary big numbers.
        var itemId = g_ItemId++;
        econItemView.ItemID = itemId;

        // @see https://gitlab.com/KittenPopo/csgo-2018-source/-/blob/main/game/shared/econ/econ_item_view.h#L313
        econItemView.ItemIDLow = (uint)itemId & 0xFFFFFFFF;
        econItemView.ItemIDHigh = (uint)itemId >> 32;
    }

    public void SetPlayerModel(CCSPlayerController player, string model)
    {
        try
        {
            Server.NextFrame(() =>
            {
                player.PlayerPawn.Value!.SetModel(model);
            });
        }
        catch (Exception)
        {
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
