/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace InventorySimulator;

public partial class InventorySimulator
{
    public bool IsLegacyModel(ushort itemDef, int paintIndex)
    {
        if (g_LookupWeaponLegacy.TryGetValue(itemDef, out var paintKitList))
        {
            return paintKitList.Contains(paintIndex);
        }
        return false;
    }

    public string? GetItemDefModel(ushort itemDef)
    {
        if (g_LookupWeaponModel.TryGetValue(itemDef, out var model))
        {
            return model;
        }
        return null;
    }

    public string? GetKnifeModel(ushort itemDef)
    {
        var model = GetItemDefModel(itemDef);
        if (model == null) return null;
        model = model.Replace("weapon_", "");
        model = model == "bayonet" ? "knife_bayonet" : model;
        return $"weapons/models/knife/{model}/weapon_{model}.vmdl";
    }

    public string? GetAgentModel(ushort itemDef)
    {
        if (g_LookupAgentModel.TryGetValue(itemDef, out var model))
        {
            return model;
        }
        return null;
    }

    public bool IsKnifeClassName(string className)
    {
        return className.Contains("bayonet") || className.Contains("knife");
    }
}
