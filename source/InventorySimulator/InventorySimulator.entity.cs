/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public bool UpdateWeaponModel(CBaseEntity weapon, bool isLegacy)
    {
        if (weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
        {
            var skeleton = weapon.CBodyComponent.SceneNode.GetSkeletonInstance();
            if (skeleton != null)
            {
                var value = (ulong)(isLegacy ? 2 : 1);
                if (skeleton.ModelState.MeshGroupMask != value)
                {
                    skeleton.ModelState.MeshGroupMask = value;
                    return true;
                }
                return false;
            }
        }
        return false;
    }

    public bool IsValidPlayer(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsHLTV;
    }

    public bool IsValidHumanPlayer(CCSPlayerController? player)
    {
        return IsValidPlayer(player) && !player!.IsBot;
    }

    public bool IsValidPlayerPawn(CCSPlayerController player)
    {
        return player.PlayerPawn != null && player.PlayerPawn.IsValid;
    }

    public bool HasKnife(CCSPlayerController player)
    {
        foreach (var weapon in player.PlayerPawn.Value!.WeaponServices!.MyWeapons)
        {
            if (weapon is { IsValid: true, Value.IsValid: true })
            {
                if (IsKnifeClassName(weapon.Value.DesignerName))
                    return true;
            }
        }
        return false;
    }
}
