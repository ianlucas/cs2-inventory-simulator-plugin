/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Entities;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public void ApplyGloveAttributesFromItem(CEconItemView glove, BaseEconItem item)
    {
        glove.Initialized = true;
        glove.ItemDefinitionIndex = item.Def;
        UpdateEconItemID(glove);

        glove.NetworkedDynamicAttributes.Attributes.RemoveAll();
        glove.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture prefab", item.Paint);
        glove.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture seed", item.Seed);
        glove.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture wear", item.Wear);

        glove.AttributeList.Attributes.RemoveAll();
        glove.AttributeList.SetOrAddAttributeValueByName("set item texture prefab", item.Paint);
        glove.AttributeList.SetOrAddAttributeValueByName("set item texture seed", item.Seed);
        glove.AttributeList.SetOrAddAttributeValueByName("set item texture wear", item.Wear);
    }

    public void ApplyWeaponAttributesFromItem(CEconItemView item, WeaponEconItem weaponItem, CBasePlayerWeapon? weapon = null, CCSPlayerController? player = null)
    {
        var isKnife = weapon?.DesignerName.IsKnifeClassName() ?? item.IsKnifeClassName();
        var entityDef = weapon?.AttributeManager.Item.ItemDefinitionIndex ?? item.ItemDefinitionIndex;

        if (isKnife)
        {
            if (weapon != null && entityDef != weaponItem.Def)
                weapon.ChangeSubclass(weaponItem.Def);

            item.ItemDefinitionIndex = weaponItem.Def;
            item.EntityQuality = 3;
        }
        else
        {
            item.EntityQuality = weaponItem.Stattrak >= 0 ? 9 : 4;
        }

        UpdateEconItemID(item);

        if (weapon != null)
        {
            weapon.FallbackPaintKit = weaponItem.Paint;
            weapon.FallbackSeed = weaponItem.Seed;
            weapon.FallbackWear = weaponItem.WearOverride ?? weaponItem.Wear;
        }

        if (player != null)
        {
            item.AccountID = (uint)player.SteamID;
        }

        item.CustomName = weaponItem.Nametag;

        item.NetworkedDynamicAttributes.Attributes.RemoveAll();
        item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture prefab", weaponItem.Paint);
        item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture seed", weaponItem.Seed);
        item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("set item texture wear", weaponItem.Wear);

        item.AttributeList.Attributes.RemoveAll();
        item.AttributeList.SetOrAddAttributeValueByName("set item texture prefab", weaponItem.Paint);
        item.AttributeList.SetOrAddAttributeValueByName("set item texture seed", weaponItem.Seed);
        item.AttributeList.SetOrAddAttributeValueByName("set item texture wear", weaponItem.Wear);

        if (weaponItem.Stattrak >= 0)
        {
            if (weapon != null)
            {
                weapon.FallbackStatTrak = weaponItem.Stattrak;
            }
            item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("kill eater", ViewAsFloat(weaponItem.Stattrak));
            item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName("kill eater score type", 0);
            item.AttributeList.SetOrAddAttributeValueByName("kill eater", ViewAsFloat(weaponItem.Stattrak));
            item.AttributeList.SetOrAddAttributeValueByName("kill eater score type", 0);
        }

        if (!isKnife)
        {
            foreach (var sticker in weaponItem.Stickers)
            {
                var slot = $"sticker slot {sticker.Slot}";
                // To set the ID of the sticker, we need to use a workaround. In the items_game.txt file, locate the
                // sticker slot 0 id entry. It should be marked with stored_as_integer set to 1. This means we need to
                // treat a uint as a float. For example, if the uint stickerId is 2229, we would interpret its value as
                // if it were a float (e.g., float stickerId = 3.12349e-42f).
                // @see https://gitlab.com/KittenPopo/csgo-2018-source/-/blame/main/game/shared/econ/econ_item_view.cpp#L194
                item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName($"{slot} id", ViewAsFloat(sticker.Def));
                item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName($"{slot} wear", sticker.Wear);
                if (sticker.Rotation != null)
                    item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName($"{slot} rotation", sticker.Rotation.Value);
                if (sticker.X != null)
                    item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName($"{slot} offset x", sticker.X.Value);
                if (sticker.Y != null)
                    item.NetworkedDynamicAttributes.SetOrAddAttributeValueByName($"{slot} offset y", sticker.Y.Value);
            }

            if (weapon != null && player != null)
            {
                UpdatePlayerWeaponMeshGroupMask(player, weapon, weaponItem.Legacy);
            }
        }
    }

    public void UpdateWeaponMeshGroupMask(CBaseEntity weapon, bool isLegacy)
    {
        if (weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
        {
            var skeleton = weapon.CBodyComponent.SceneNode.GetSkeletonInstance();
            if (skeleton != null)
            {
                // The term "Legacy" refers to the old weapon models from CS:GO. In CS2, the MeshGroupMask is used to
                // determine whether the game should display the legacy model (value = 2) or the new model (value = 1).
                var value = (ulong)(isLegacy ? 2 : 1);
                if (skeleton.ModelState.MeshGroupMask != value)
                {
                    skeleton.ModelState.MeshGroupMask = value;
                }
            }
        }
    }

    public void UpdatePlayerWeaponMeshGroupMask(CCSPlayerController player, CBasePlayerWeapon weapon, bool isLegacy)
    {
        // 1. We update the weapon's MeshGroupMask.
        UpdateWeaponMeshGroupMask(weapon, isLegacy);

        // 2. If the current view model is displaying it, ensure that it has the correct MeshGroupMask.
        var viewModel = player.GetViewModel();
        if (viewModel != null && viewModel.Weapon.Value != null && viewModel.Weapon.Value.Index == weapon.Index)
        {
            UpdateWeaponMeshGroupMask(viewModel, isLegacy);
            Utilities.SetStateChanged(viewModel, "CBaseEntity", "m_CBodyComponent");
        }
    }

    public void UpdateEconItemID(CEconItemView econItemView)
    {
        // Alright, so the ItemID serves as a global identifier for items. Since we're simulating it, we're
        // using arbitrary large numbers.
        var itemId = NextItemId++;
        econItemView.ItemID = itemId;

        // @see https://gitlab.com/KittenPopo/csgo-2018-source/-/blob/main/game/shared/econ/econ_item_view.h#L313
        econItemView.ItemIDLow = (uint)itemId & 0xFFFFFFFF;
        econItemView.ItemIDHigh = (uint)itemId >> 32;
    }

    public bool IsCustomWeaponItemID(CBasePlayerWeapon weapon)
    {
        return weapon.AttributeManager.Item.ItemID >= MinimumCustomItemID;
    }

    public void SetPlayerModel(
        CCSPlayerController player,
        string model,
        bool voFallback = true,
        string voPrefix = "",
        bool voFemale = false,
        List<uint>? patches = null
    )
    {
        try
        {
            Server.NextFrame(() =>
            {
                if (!player.IsValid)
                    return;
                var pawn = player.PlayerPawn.Value;
                if (pawn == null)
                    return;
                if (patches != null && patches.Count == 5)
                {
                    for (var index = 0; index < patches.Count; index++)
                    {
                        pawn.PlayerPatchEconIndices[index] = patches[index];
                    }
                }
                if (!voFallback)
                {
                    Server.NextFrame(() =>
                    {
                        if (pawn.IsValid)
                        {
                            pawn.StrVOPrefix = voPrefix;
                            pawn.HasFemaleVoice = voFemale;
                        }
                    });
                }
                pawn.SetModel(model);
            });
        }
        catch
        {
            // Ignore any error.
        }
    }

    public CCSPlayerController? GetPlayerFromItemServices(CCSPlayer_ItemServices itemServices)
    {
        var pawn = itemServices.Pawn.Value;
        if (pawn == null || !pawn.IsValid || !pawn.Controller.IsValid || pawn.Controller.Value == null)
            return null;
        var player = new CCSPlayerController(pawn.Controller.Value.Handle);
        if (!IsPlayerHumanAndValid(player))
            return null;
        return player;
    }

    public bool IsPlayerValid(CCSPlayerController? player)
    {
        return player != null && player.IsValid && !player.IsHLTV;
    }

    public bool IsPlayerHumanAndValid(CCSPlayerController? player)
    {
        return IsPlayerValid(player) && !player!.IsBot;
    }

    public bool IsPlayerPawnValid(CCSPlayerController player)
    {
        return player.PlayerPawn != null && player.PlayerPawn.Value != null && player.PlayerPawn.IsValid;
    }

    public bool IsPlayerUseCmdBusy(CCSPlayerController player)
    {
        if (player.PlayerPawn.Value?.IsBuyMenuOpen == true)
            return true;
        if (player.PlayerPawn.Value?.IsDefusing == true)
            return true;
        var weapon = player.PlayerPawn.Value?.WeaponServices?.ActiveWeapon.Value;
        if (weapon?.DesignerName != "weapon_c4")
            return false;
        var c4 = weapon.As<CC4>();
        return c4.IsPlantingViaUse;
    }
}
