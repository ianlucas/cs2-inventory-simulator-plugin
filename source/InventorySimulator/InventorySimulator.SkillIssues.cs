/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using System;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace InventorySimulator;

public static class SkillIssues
{
    private static readonly Dictionary<string, int> _itemDefinitionIndexes = new()
    {
        { "weapon_deagle", 1 },
        { "weapon_elite", 2 },
        { "weapon_fiveseven", 3 },
        { "weapon_glock", 4 },
        { "weapon_ak47", 7 },
        { "weapon_aug", 8 },
        { "weapon_awp", 9 },
        { "weapon_famas", 10 },
        { "weapon_g3sg1", 11 },
        { "weapon_galilar", 13 },
        { "weapon_m249", 14 },
        { "weapon_m4a1", 16 },
        { "weapon_mac10", 17 },
        { "weapon_p90", 19 },
        { "weapon_mp5sd", 23 },
        { "weapon_ump45", 24 },
        { "weapon_xm1014", 25 },
        { "weapon_bizon", 26 },
        { "weapon_mag7", 27 },
        { "weapon_negev", 28 },
        { "weapon_sawedoff", 29 },
        { "weapon_tec9", 30 },
        { "weapon_taser", 31 },
        { "weapon_hkp2000", 32 },
        { "weapon_mp7", 33 },
        { "weapon_mp9", 34 },
        { "weapon_nova", 35 },
        { "weapon_p250", 36 },
        { "weapon_scar20", 38 },
        { "weapon_sg556", 39 },
        { "weapon_ssg08", 40 },
        { "weapon_c4", 49 },
        { "weapon_m4a1_silencer", 60 },
        { "weapon_usp_silencer", 61 },
        { "weapon_cz75a", 63 },
        { "weapon_revolver", 64 },
        { "weapon_knife", 42 },
        { "weapon_knife_t", 59 },
        { "weapon_bayonet", 500 },
        { "weapon_knife_css", 503 },
        { "weapon_knife_flip", 505 },
        { "weapon_knife_gut", 506 },
        { "weapon_knife_karambit", 507 },
        { "weapon_knife_m9_bayonet", 508 },
        { "weapon_knife_tactical", 509 },
        { "weapon_knife_falchion", 512 },
        { "weapon_knife_survival_bowie", 514 },
        { "weapon_knife_butterfly", 515 },
        { "weapon_knife_push", 516 },
        { "weapon_knife_cord", 517 },
        { "weapon_knife_canis", 518 },
        { "weapon_knife_ursus", 519 },
        { "weapon_knife_gypsy_jackknife", 520 },
        { "weapon_knife_outdoor", 521 },
        { "weapon_knife_stiletto", 522 },
        { "weapon_knife_widowmaker", 523 },
        { "weapon_knife_skeleton", 525 },
        { "weapon_knife_kukri", 526 },
    };

    public static string? GetDesignerName(this CEconItemView item) =>
        _itemDefinitionIndexes.Where(w => w.Value == item.ItemDefinitionIndex).Select(w => w.Key).FirstOrDefault();

    public static string GetDesignerName(this CBasePlayerWeapon weapon)
    {
        var designerName = weapon.AttributeManager.Item.GetDesignerName() ?? weapon.DesignerName;
        return designerName.IsKnifeClassName() ? "weapon_knife" : designerName;
    }

    public static bool IsKnifeClassName(this string className) => className.Contains("bayonet") || className.Contains("knife");

    public static bool IsKnifeClassName(this CEconItemView item) => item.GetDesignerName()?.IsKnifeClassName() ?? false;
}
