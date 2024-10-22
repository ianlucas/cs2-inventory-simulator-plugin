/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public HookResult OnProcessUsercmdsPost(DynamicHook hook)
    {
        if (!invsim_spray_on_use.Value)
            return HookResult.Continue;

        var player = hook.GetParam<CCSPlayerController>(0);
        if ((player.Buttons & PlayerButtons.Use) != 0 && player.PlayerPawn.Value?.IsAbleToApplySpray() == true)
        {
            if (IsPlayerUseCmdBusy(player))
                PlayerUseCmdBlockManager[player.SteamID] = true;
            if (PlayerUseCmdManager.TryGetValue(player.SteamID, out var timer))
                timer.Kill();
            PlayerUseCmdManager[player.SteamID] = AddTimer(0.1f, () =>
            {
                if (PlayerUseCmdBlockManager.ContainsKey(player.SteamID))
                    PlayerUseCmdBlockManager.Remove(player.SteamID, out var _);
                else if (player.IsValid && !IsPlayerUseCmdBusy(player))
                    player.ExecuteClientCommandFromServer("css_spray");
            });
        }

        return HookResult.Continue;
    }

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
}
