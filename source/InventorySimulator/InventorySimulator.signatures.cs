/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;

namespace InventorySimulator;

public partial class InventorySimulator
{
    // This was made public by skuzzis.
    // CS# public implementation by stefanx111.
    public void SetOrAddAttributeValueByName(CAttributeList attributeList, string name, float value)
    {
        var SetOrAddAttributeValueByNameFunc = VirtualFunction.Create<nint, string, float, int>(GameData.GetSignature("CAttributeList_SetOrAddAttributeValueByName"));
        SetOrAddAttributeValueByNameFunc(attributeList.Handle, name, value);
    }

    // This was made public by skuzzis.
    // CS# public implementation by stefanx111.
    public void SetBodygroup(CCSPlayerController player, string model, int i)
    {
        var SetBodygroupFunc = VirtualFunction.Create<nint, string, int, int>(GameData.GetSignature("CBaseModelEntity_SetBodygroup"));
        SetBodygroupFunc(player.PlayerPawn.Value!.Handle, model, i);
    }
}