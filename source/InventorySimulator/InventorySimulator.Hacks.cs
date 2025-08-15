/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InventorySimulator;

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

public static class MemoryPatcher
{
    private static class WindowsNative
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        internal const uint PAGE_EXECUTE_READWRITE = 0x40;
    }

    private static class LinuxNative
    {
        [DllImport("libc", SetLastError = true)]
        internal static extern int mprotect(IntPtr addr, UIntPtr len, int prot);

        [Flags]
        internal enum Protection
        {
            PROT_READ = 0x1,
            PROT_WRITE = 0x2,
            PROT_EXEC = 0x4,
        }
    }

    public static bool ApplyPatch(IntPtr address, byte[] patchBytes)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return ApplyPatchWindows(address, patchBytes);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return ApplyPatchLinux(address, patchBytes);
        }
        return false;
    }

    private static unsafe bool ApplyPatchWindows(IntPtr address, byte[] patchBytes)
    {
        if (!WindowsNative.VirtualProtect(address, (UIntPtr)patchBytes.Length, WindowsNative.PAGE_EXECUTE_READWRITE, out uint oldProtect))
        {
            return false;
        }

        try
        {
            fixed (byte* pPatchBytes = patchBytes)
            {
                Unsafe.CopyBlock(address.ToPointer(), pPatchBytes, (uint)patchBytes.Length);
            }
        }
        finally
        {
            WindowsNative.VirtualProtect(address, (UIntPtr)patchBytes.Length, oldProtect, out _);
        }
        return true;
    }

    private static unsafe bool ApplyPatchLinux(IntPtr address, byte[] patchBytes)
    {
        long pageSize = Environment.SystemPageSize;
        long pageStartMask = ~(pageSize - 1);
        IntPtr pageStartAddress = (IntPtr)(address.ToInt64() & pageStartMask);
        UIntPtr regionSize = (UIntPtr)(address.ToInt64() - pageStartAddress.ToInt64() + patchBytes.Length);
        int protection = (int)(LinuxNative.Protection.PROT_READ | LinuxNative.Protection.PROT_WRITE | LinuxNative.Protection.PROT_EXEC);

        if (LinuxNative.mprotect(pageStartAddress, regionSize, protection) != 0)
        {
            return false;
        }

        fixed (byte* pPatchBytes = patchBytes)
        {
            Unsafe.CopyBlock(address.ToPointer(), pPatchBytes, (uint)patchBytes.Length);
        }

        return true;
    }
}
