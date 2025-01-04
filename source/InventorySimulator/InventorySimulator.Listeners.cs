/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public void OnTick()
    {
        // According to @bklol the right way to change the Music Kit is to update the player's inventory, I'm
        // pretty sure that's the best way to change anything inventory-related, but that's not something
        // public and we brute force the setting of the Music Kit here.
        foreach (var (player, inventory) in PlayerOnTickInventoryManager.Values)
            if (player != null)
            {
                GivePlayerMusicKit(player, inventory);
            }
    }

    public void OnEntityCreated(CEntityInstance entity)
    {
        var designerName = entity.DesignerName;

        if (designerName.Contains("weapon"))
        {
            Server.NextFrame(() =>
            {
                var weapon = new CBasePlayerWeapon(entity.Handle);
                if (!weapon.IsValid || weapon.OriginalOwnerXuidLow == 0)
                    return;

                var player = Utilities.GetPlayerFromSteamId(weapon.OriginalOwnerXuidLow);
                if (player == null || !IsPlayerHumanAndValid(player))
                    return;

                GivePlayerWeaponSkin(player, weapon);
            });
        }
        else if (designerName == "player_spray_decal")
        {
            if (!invsim_spraychanger_enabled.Value)
                return;

            Server.NextFrame(() =>
            {
                var sprayDecal = new CPlayerSprayDecal(entity.Handle);
                if (!sprayDecal.IsValid || sprayDecal.AccountID == 0)
                    return;

                var player = Utilities.GetPlayerFromSteamId(sprayDecal.AccountID);
                if (player == null || !IsPlayerHumanAndValid(player))
                    return;

                GivePlayerGraffiti(player, sprayDecal);
            });
        }
    }

    public void OnInvsimFileChanged(object? _, string __)
    {
        LoadPlayerInventories();
    }

    public void OnInvSimRequireInventoryChange(object? _, bool value)
    {
        if (value)
        {
            Extensions.ConnectFunc.Hook(OnConnect, HookMode.Post);
            Extensions.SetSignonStateFunc.Hook(OnSetSignonState, HookMode.Pre);
        }
        else
        {
            Extensions.ConnectFunc.Unhook(OnConnect, HookMode.Post);
            Extensions.SetSignonStateFunc.Unhook(OnSetSignonState, HookMode.Pre);
        }
    }
}
