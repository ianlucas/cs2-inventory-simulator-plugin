/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace InventorySimulator;

public partial class InventorySimulator
{
    // sajad0x0 from UC ended up helping me figuring out this signature.
    public static readonly MemoryFunctionWithReturn<nint, string, int> ChangeSubclassFunc = new(
        GameData.GetSignature("ChangeSubclass"));

    public static readonly Func<nint, string, int> ChangeSubclass = ChangeSubclassFunc.Invoke;

    // This was made public by skuzzis.
    // First CS# public implementation by stefanx111.
    public static readonly MemoryFunctionWithReturn<nint, string, float, int> SetOrAddAttributeValueByNameFunc = new(
        GameData.GetSignature("CAttributeList_SetOrAddAttributeValueByName"));

    public static readonly Func<nint, string, float, int> SetOrAddAttributeValueByName = SetOrAddAttributeValueByNameFunc.Invoke;

    // This was made public by skuzzis.
    // First CS# public implementation by stefanx111.
    public static readonly MemoryFunctionWithReturn<nint, string, int, int> SetBodygroupFunc = new(
        GameData.GetSignature("CBaseModelEntity_SetBodygroup"));

    public static readonly Func<nint, string, int, int> SetBodygroup = SetBodygroupFunc.Invoke;
}