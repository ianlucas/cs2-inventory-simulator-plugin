/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

namespace InventorySimulator;

public partial class InventorySimulator
{
    public string GetWeaponClassName(string model)
    {
        return $"weapon_{model}";
    }

    public string GetKnifeModelPath(string model)
    {
        model = model == "bayonet" ? "knife_bayonet" : model;
        return $"weapons/models/knife/{model}/weapon_{model}.vmdl";
    }

    public string GetAgentModelPath(string model)
    {
        return $"characters/models/{model}.vmdl";
    }

    public bool IsKnifeClassName(string className)
    {
        return className.Contains("bayonet") || className.Contains("knife");
    }
}
