/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using System.Runtime.InteropServices;

namespace InventorySimulator;

[MinimumApiVersion(175)]
public partial class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
    public override string ModuleName => "InventorySimulator";
    public override string ModuleVersion => "0.0.4";

    private readonly Dictionary<ulong, PlayerInventory> g_PlayerInventory = new();
    private readonly bool g_IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

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

    [GameEventHandler(HookMode.Pre)]
    public HookResult OnPlayerSpawnPre(EventPlayerSpawn @event, GameEventInfo info)
    {
        // Plugin takes care of giving knives to players.
        // This is a workaround for the weapon removal crash issue.
        Server.ExecuteCommand("mp_ct_default_melee \"\"");
        Server.ExecuteCommand("mp_t_default_melee \"\"");

        // Looks like this throws some informational logs in the console due to it being
        // triggered and probably executed on every spawn in the game, but I want to keep
        // it here to ensure any outsider exec will not mess with this.
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (!IsPlayerValid(player) || !IsPlayerPawnValid(player))
            return HookResult.Continue;

        GivePlayerMusicKit(player);
        GivePlayerAgent(player);
        GivePlayerGloves(player);
        GivePlayerKnife(player);

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
                if (viewModel == null) continue;
                if (viewModel.Weapon.Value == null) continue;

                CBasePlayerWeapon weapon = viewModel.Weapon.Value;
                if (weapon == null || !weapon.IsValid) continue;

                var inventory = GetPlayerInventory(player);
                var isKnife = viewModel.VMName.Contains("knife");
                var itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
                var team = player.TeamNum;

                if (!isKnife)
                {
                    if (!inventory.HasProperty("pa", team, itemDef)) continue;

                    // Looks like changing the weapon's MeshGroupMask during creation is not enough, so we
                    // update the view model's skeleton as well.
                    if (UpdateWeaponModel(viewModel, inventory.HasProperty("pal", team, itemDef)))
                    {
                        Utilities.SetStateChanged(viewModel, "CBaseEntity", "m_CBodyComponent");
                    }
                }
                else
                {
                    if (!g_IsWindows || !inventory.HasProperty("me", team)) continue;

                    // In Windows, we cannot give knives using GiveNamedItem, so we force into the viewmodel
                    // using the SetModel function. A caveat is that the animations are broken and the player
                    // will always see the rarest deploy animation.
                    var model = inventory.GetString("mem", team);
                    if (model != null)
                    {
                        var modelPath = GetKnifeModelPath(model);
                        if (viewModel.VMName != modelPath)
                        {
                            viewModel.VMName = modelPath;
                            viewModel.SetModel(modelPath);
                        }
                    }
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
                    weapon.AttributeManager.Item.ItemDefinitionIndex = itemDef;
                    if (g_IsWindows)
                    {
                        var model = inventory.GetString("mem", team);
                        if (model != null)
                        {
                            weapon.SetModel(GetKnifeModelPath(model));
                        }
                    }
                    weapon.AttributeManager.Item.EntityQuality = 3;
                }

                weapon.AttributeManager.Item.ItemID = 16384;
                weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
                weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;

                var paintKit = inventory.GetInt("pa", team, itemDef, 0);
                weapon.FallbackPaintKit = paintKit;
                weapon.FallbackSeed = inventory.GetInt("se", team, itemDef, 1);
                weapon.FallbackWear = inventory.GetFloat("fl", team, itemDef, 0.0f);
                weapon.FallbackStatTrak = inventory.GetInt("st", team, itemDef, -1);
                weapon.AttributeManager.Item.CustomName = inventory.GetString("nt", team, itemDef, "");

                if (!isKnife)
                {
                    UpdateWeaponModel(weapon, inventory.HasProperty("pal", team, itemDef));
                }
            });
        }
    }
}

