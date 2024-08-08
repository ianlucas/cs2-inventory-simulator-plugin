/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace InventorySimulator;

public static class Extensions
{
    // sajad0x0 from UC ended up helping me figuring out this signature.
    public static readonly MemoryFunctionWithReturn<IntPtr, string, int> ChangeSubclassMemFunc = new(
        GameData.GetSignature("ChangeSubclass"));

    public static readonly Func<nint, string, int> ChangeSubclassFunc = ChangeSubclassMemFunc.Invoke;

    // This was made public by skuzzis.
    // First CS# public implementation by stefanx111.
    public static readonly MemoryFunctionWithReturn<IntPtr, string, float, int> SetOrAddAttributeValueByNameMemFunc = new(
        GameData.GetSignature("CAttributeList_SetOrAddAttributeValueByName"));

    public static readonly Func<IntPtr, string, float, int> SetOrAddAttributeValueByNameFunc = SetOrAddAttributeValueByNameMemFunc.Invoke;

    // This was made public by skuzzis.
    // First CS# public implementation by stefanx111.
    public static readonly MemoryFunctionWithReturn<IntPtr, string, int, int> SetBodygroupMemFunc = new(
        GameData.GetSignature("CBaseModelEntity_SetBodygroup"));

    public static readonly Func<IntPtr, string, int, int> SetBodygroupFunc = SetBodygroupMemFunc.Invoke;

    public static int ChangeSubclass(this CBasePlayerWeapon weapon, ushort itemDef)
    {
        return ChangeSubclassFunc(weapon.Handle, itemDef.ToString());
    }

    public static int SetOrAddAttributeValueByName(this CAttributeList attributeList, string attribDefName, float value)
    {
        return SetOrAddAttributeValueByNameFunc(attributeList.Handle, attribDefName, value);
    }

    public static int SetBodygroup(this CCSPlayerPawn pawn, string group, int value)
    {
        return SetBodygroupFunc(pawn.Handle, group, value);
    }
}
