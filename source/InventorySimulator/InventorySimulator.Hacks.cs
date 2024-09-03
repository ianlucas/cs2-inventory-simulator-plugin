/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using NativeVector = CounterStrikeSharp.API.Modules.Utils.Vector;
using System.Numerics;
using System.Runtime.InteropServices;

namespace InventorySimulator;

public static class HackExtensions
{
    // This is a hack by KillStr3aK.
    public static unsafe CBaseViewModel? GetViewModel(this CCSPlayerController player)
    {
        if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null) return null;
        CCSPlayer_ViewModelServices viewModelServices = new(player.PlayerPawn.Value.ViewModelServices.Handle);
        IntPtr ptr = viewModelServices.Handle + Schema.GetSchemaOffset("CCSPlayer_ViewModelServices", "m_hViewModel");
        var references = MemoryMarshal.CreateSpan(ref ptr, 3);
        var viewModel = (CHandle<CBaseViewModel>)Activator.CreateInstance(typeof(CHandle<CBaseViewModel>), references[0])!;
        if (viewModel == null || viewModel.Value == null) return null;
        return viewModel.Value;
    }
}

// This is a hack by Nuko, adapted from UgurhanK/BaseBuilder.
public static class GameTraceManager
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private unsafe delegate bool TraceFuncShape(
        IntPtr GameTraceManager,
        IntPtr vecStart,
        IntPtr vecEnd,
        IntPtr skip,
        ulong mask,
        byte a6,
        GameTrace* pGameTrace);

    private static readonly IntPtr TracePtr = NativeAPI.FindSignature(Addresses.ServerPath, GameData.GetSignature("Trace"));
    private static readonly TraceFuncShape TraceFunc = Marshal.GetDelegateForFunctionPointer<TraceFuncShape>(TracePtr);
    private static readonly IntPtr GameTraceManagerPtr = NativeAPI.FindSignature(Addresses.ServerPath, GameData.GetSignature("GameTraceManager"));
    private static readonly IntPtr GameTraceManagerAddress = Address.GetAbsoluteAddress(GameTraceManagerPtr, 3, 7);

    public static NativeVector Vector3toVector(Vector3 vec) => new(vec.X, vec.Y, vec.Z);

    public static unsafe (NativeVector, NativeVector)? Trace(
        NativeVector origin,
        QAngle viewangles,
        bool drawResult = false,
        bool fromPlayer = false)
    {
        var forward = new NativeVector();
        NativeAPI.AngleVectors(viewangles.Handle, forward.Handle, 0, 0);
        var reach = 8192;
        var endOrigin = new NativeVector(origin.X + forward.X * reach, origin.Y + forward.Y * reach, origin.Z + forward.Z * reach);
        var distance = 50;
        if (fromPlayer)
        {
            origin.X += forward.X * distance;
            origin.Y += forward.Y * distance;
            origin.Z += forward.Z * distance;
        }
        var trace = stackalloc GameTrace[1];
        var result = TraceFunc(*(IntPtr*)GameTraceManagerAddress, origin.Handle, endOrigin.Handle, 0, 0x1C1003, 4, trace);
        if (result)
        {
            return (
                Vector3toVector(trace->EndPos),
                Vector3toVector(trace->Normal));
        }
        return null;
    }
}

public static class Address
{
    static unsafe public IntPtr GetAbsoluteAddress(IntPtr addr, IntPtr offset, IntPtr size)
    {
        int code = *(int*)(addr + offset);
        return addr + code + size;
    }
}

[StructLayout(LayoutKind.Explicit, Size = 0x44)]
public unsafe struct TraceHitboxData
{
    [FieldOffset(0x38)] public int HitGroup;
    [FieldOffset(0x40)] public int HitboxId;
}

[StructLayout(LayoutKind.Explicit, Size = 0xB8)]
public unsafe struct GameTrace
{
    [FieldOffset(0)] public void* Surface;
    [FieldOffset(0x8)] public void* HitEntity;
    [FieldOffset(0x10)] public TraceHitboxData* HitboxData;
    [FieldOffset(0x50)] public uint Contents;
    [FieldOffset(0x78)] public Vector3 StartPos;
    [FieldOffset(0x84)] public Vector3 EndPos;
    [FieldOffset(0x90)] public Vector3 Normal;
    [FieldOffset(0x9C)] public Vector3 Position;
    [FieldOffset(0xAC)] public float Fraction;
    [FieldOffset(0xB6)] public bool AllSolid;
}
