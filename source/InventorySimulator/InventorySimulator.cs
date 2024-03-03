/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;

namespace InventorySimulator;

[MinimumApiVersion(175)]
public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
    public override string ModuleName => "InventorySimulator";
    public override string ModuleVersion => "0.0.5";

    private readonly Dictionary<ulong, PlayerInventory> g_PlayerInventory = new();
    private ulong g_ItemId = UInt64.MaxValue - 32768;

    public override void Load(bool hotReload)
    {
        RegisterListener<Listeners.OnTick>(OnTick);
        RegisterListener<Listeners.OnEntitySpawned>(OnEntitySpawned);
    }

    [GameEventHandler]
    public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;

        if (!IsPlayerHumanAndValid(player))
            return HookResult.Continue;

        var steamId = player.SteamID;

        Task.Run(async () =>
        {
            await FetchPlayerInventory(steamId);
        });

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (!IsPlayerHumanAndValid(player) || !IsPlayerPawnValid(player))
            return HookResult.Continue;

        GivePlayerMusicKit(player);
        GivePlayerAgent(player);
        GivePlayerGloves(player);

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (IsPlayerHumanAndValid(player))
        {
            g_PlayerInventory.Remove(player.SteamID);
        }

        return HookResult.Continue;
    }

    public void OnTick()
    {
        foreach (var player in Utilities.GetPlayers())
        {
            try
            {
                if (!IsPlayerHumanAndValid(player)) continue;

                var viewModel = GetPlayerViewModel(player);
                if (viewModel == null || viewModel.Weapon.Value == null) continue;
                if (viewModel.VMName.Contains("knife")) continue;

                CBasePlayerWeapon weapon = viewModel.Weapon.Value;
                if (weapon == null || !weapon.IsValid) continue;

                var inventory = GetPlayerInventory(player);
                var itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
                var team = player.TeamNum;

                if (!inventory.HasProperty("pa", team, itemDef)) continue;

                // Looks like changing the weapon's MeshGroupMask during creation is not enough, so we
                // update the view model's skeleton as well.
                if (UpdateWeaponModel(viewModel, inventory.HasProperty("pal", team, itemDef)))
                {
                    Utilities.SetStateChanged(viewModel, "CBaseEntity", "m_CBodyComponent");
                }
            }
            catch (Exception)
            { }
        }
    }

    public void OnEntitySpawned(CEntityInstance entity)
    {
        var designerName = entity.DesignerName;

        if (designerName.Contains("weapon"))
        {
            var isKnife = IsKnifeClassName(designerName);

            Server.NextFrame(() =>
            {
                var weapon = new CBasePlayerWeapon(entity.Handle);
                if (!weapon.IsValid || weapon.OwnerEntity.Value == null || weapon.OwnerEntity.Index <= 0) return;

                int weaponOwner = (int)weapon.OwnerEntity.Index;
                var pawn = new CBasePlayerPawn(NativeAPI.GetEntityFromIndex(weaponOwner));
                if (!pawn.IsValid) return;

                var playerIndex = (int)pawn.Controller.Index;
                var player = Utilities.GetPlayerFromIndex(playerIndex);
                if (!IsPlayerHumanAndValid(player)) return;

                var inventory = GetPlayerInventory(player);
                var itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
                var team = player.TeamNum;
                var hasKnife = inventory.HasProperty("me", team);

                if (isKnife && !hasKnife) return;

                var hasNametag = inventory.HasProperty("nt", team, itemDef);
                var hasPaintKit = inventory.HasProperty("pa", team, itemDef);
                var hasStickers = inventory.HasProperty("ss", team, itemDef);
                var hasWear = inventory.HasProperty("fl", team, itemDef);
                var hasSeed = inventory.HasProperty("se", team, itemDef);
                var hasStatTrak = inventory.HasProperty("st", team, itemDef);
                var isCustomItem = hasKnife || hasPaintKit || hasNametag || hasStickers || hasWear || hasSeed || hasStatTrak;

                if (!isKnife && !isCustomItem) return;

                if (isKnife)
                {
                    itemDef = inventory.GetUShort("me", team);
                    if (weapon.AttributeManager.Item.ItemDefinitionIndex != itemDef)
                    {
                        SubclassChange(weapon, itemDef);
                    }
                    weapon.AttributeManager.Item.ItemDefinitionIndex = itemDef;
                    weapon.AttributeManager.Item.EntityQuality = 3;
                }

                UpdateItemID(weapon.AttributeManager.Item);

                var paintKit = inventory.GetInt("pa", team, itemDef, 0);
                weapon.FallbackPaintKit = paintKit;
                weapon.FallbackSeed = inventory.GetInt("se", team, itemDef, 1);
                weapon.FallbackWear = inventory.GetFloat("fl", team, itemDef, 0.0f);
                weapon.FallbackStatTrak = inventory.GetInt("st", team, itemDef, -1);
                weapon.AttributeManager.Item.CustomName = inventory.GetString("nt", team, itemDef, "");

                // This APPEARS to fix the issue where sometimes the skin name won't be displayed on HUD.
                SetOrAddAttributeValueByName(weapon.AttributeManager.Item.NetworkedDynamicAttributes, "set item texture prefab", paintKit);

                if (!isKnife)
                {
                    UpdateWeaponModel(weapon, inventory.HasProperty("pal", team, itemDef));
                }
            });
        }
    }
}

