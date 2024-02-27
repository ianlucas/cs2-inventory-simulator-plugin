/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Runtime.InteropServices;

namespace InventorySimulator;

public partial class InventorySimulator
{
    // This is a hack by KillStr3aK.
    public unsafe CHandle<CBaseViewModel>[]? GetPlayerViewModels(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null) return null;
        CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value.ViewModelServices!.Handle);
        nint ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
        var references = MemoryMarshal.CreateSpan(ref ptr, 3);
        var values = new CHandle<CBaseViewModel>[3];

        for (int i = 0; i < 3; i++)
        {
            values[i] = (CHandle<CBaseViewModel>)Activator.CreateInstance(typeof(CHandle<CBaseViewModel>), references[i])!;
        }

        return values;
    }
}
