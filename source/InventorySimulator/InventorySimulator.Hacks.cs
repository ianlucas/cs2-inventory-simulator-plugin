/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Numerics;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;

namespace InventorySimulator;

public static class HackExtensions
{
    // This is a hack by KillStr3aK.
    public static unsafe CBaseViewModel? GetViewModel(this CCSPlayerController player)
    {
        if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null)
            return null;
        var viewModelServices = new CCSPlayer_ViewModelServices(player.PlayerPawn.Value.ViewModelServices.Handle);
        var ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
        var references = MemoryMarshal.CreateSpan(ref ptr, 3);
        var viewModel = (CHandle<CBaseViewModel>)Activator.CreateInstance(typeof(CHandle<CBaseViewModel>), references[0])!;
        if (viewModel == null || viewModel.Value == null)
            return null;
        return viewModel.Value;
    }
}

// This is a hack by Nuko.
[StructLayout(LayoutKind.Explicit, Size = 0x44)]
public unsafe struct TraceHitboxData
{
    [FieldOffset(0x38)]
    public int HitGroup;

    [FieldOffset(0x40)]
    public int HitboxId;
}

[StructLayout(LayoutKind.Explicit, Size = 0xB8)]
public unsafe struct GameTrace
{
    [FieldOffset(0)]
    public void* Surface;

    [FieldOffset(0x8)]
    public void* HitEntity;

    [FieldOffset(0x10)]
    public TraceHitboxData* HitboxData;

    [FieldOffset(0x50)]
    public uint Contents;

    [FieldOffset(0x78)]
    public Vector3 StartPos;

    [FieldOffset(0x84)]
    public Vector3 EndPos;

    [FieldOffset(0x90)]
    public Vector3 Normal;

    [FieldOffset(0x9C)]
    public Vector3 Position;

    [FieldOffset(0xAC)]
    public float Fraction;

    [FieldOffset(0xB6)]
    public bool AllSolid;
}
