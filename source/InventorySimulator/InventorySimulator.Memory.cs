/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public static void PatchChangeSubclass()
    {
        IntPtr addressToPatch = NativeAPI.FindSignature(Addresses.ServerPath, GameData.GetSignature("ChangeSubclass"));
        byte[] patchBytes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? [0xEB] : [0x90, 0x90, 0x90, 0x90, 0x90, 0x90];
        if (MemoryPatcher.ApplyPatch(addressToPatch, patchBytes))
            Server.PrintToConsole($"[ChangeSubclass] Patch applied successfully at 0x{addressToPatch.ToInt64():X}");
        else
            Server.PrintToConsole($"[ChangeSubclass] Failed to apply patch at 0x{addressToPatch.ToInt64():X}");
    }
}
