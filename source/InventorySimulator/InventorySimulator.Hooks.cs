﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public HookResult OnGiveNamedItemPost(DynamicHook hook)
    {
        var className = hook.GetParam<string>(1);
        if (!className.Contains("weapon"))
            return HookResult.Continue;

        var itemServices = hook.GetParam<CCSPlayer_ItemServices>(0);
        var weapon = hook.GetReturn<CBasePlayerWeapon>();
        var player = GetPlayerFromItemServices(itemServices);

        if (player != null)
        {
            GivePlayerWeaponSkin(player, weapon);
        }

        return HookResult.Continue;
    }

    public HookResult OnPlayerCanRespawnPost(DynamicHook hook)
    {
        if (!invsim_validation_enabled.Value)
            return HookResult.Continue;

        var pawn = hook.GetParam<CCSPlayerPawn>(1);
        var controller = pawn.Controller.Value?.As<CCSPlayerController>();

        if (controller != null && !controller.IsBot && !PlayerInventoryManager.ContainsKey(controller.SteamID))
        {
            hook.SetReturn(false);
        }

        return HookResult.Continue;
    }
}
