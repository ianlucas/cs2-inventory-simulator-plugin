/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the GPL-3.0 License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace InventorySimulator
{
    public class InventorySimulator : BasePlugin
    {
        public override string ModuleAuthor => "Ian Lucas";
        public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
        public override string ModuleName => "InventorySimulator";
        public override string ModuleVersion => "0.0.1";

        private static List<CS_Item>? g_EconomyItems;
        private static Dictionary<ulong, Dictionary<string, object>?> g_PlayerEquipment = new();
        private static Dictionary<ushort, Dictionary<int, bool>> g_LegacyItemDefPaintKit = new();
        private static Dictionary<ushort, string> g_ItemDefModel = new();

        public override void Load(bool hotReload)
        {
            Task.Run(async () =>
            {
                await LoadEconomyItems();
            });

            /// <summary>
            /// Hacks our way to display the expected skins on the player's view model.
            /// </summary>
            RegisterListener<Listeners.OnTick>(() =>
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    try {
                        if (!player.IsValid || player.IsBot || player.IsHLTV) continue;
                        var viewModels = GetPlayerViewModels(player);
                        if (viewModels == null) continue;
                        var viewModel = viewModels[0];
                        if (viewModel == null || viewModel.Value == null || viewModel.Value.Weapon == null || viewModel.Value.Weapon.Value == null) continue;
                        CBasePlayerWeapon weapon = viewModel.Value.Weapon.Value;
                        if (weapon == null || !weapon.IsValid) continue;
                        var isKnife = viewModel.Value.VMName.Contains("knife");
                        if (!isKnife)
                        {
                            if (
                                viewModel.Value.CBodyComponent != null
                                && viewModel.Value.CBodyComponent.SceneNode != null
                            )
                            {
                                var skeleton = GetSkeletonInstance(viewModel.Value.CBodyComponent.SceneNode);
                                skeleton.ModelState.MeshGroupMask = (ulong) (
                                    IsLegacyModel(weapon.AttributeManager.Item.ItemDefinitionIndex, weapon.FallbackPaintKit) ? 2 : 1
                                );
                            }
                            Utilities.SetStateChanged(viewModel.Value, "CBaseEntity", "m_CBodyComponent");
                        } else
                        {
                            var newModel = GetKnifeModel(weapon.AttributeManager.Item.ItemDefinitionIndex);
                            if (newModel != "" && viewModel.Value.VMName != newModel)
                            {
                                viewModel.Value.VMName = newModel;
                                viewModel.Value.SetModel(newModel);
                            }
                        }
                    } catch (Exception)
                    {}
                }
            });

            /// <summary>
            /// Updates the attributes of an entity when it's created.
            /// </summary>
            RegisterListener<Listeners.OnEntityCreated>(entity =>
            {
                var designerName = entity.DesignerName;

                if (designerName.Contains("weapon")) {
                    var isKnife = designerName.Contains("bayonet") || designerName.Contains("knife");

                    Server.NextFrame(() =>
                    {
                        var weapon = new CBasePlayerWeapon(entity.Handle);
                        if (!weapon.IsValid) return;
                        if (weapon.OwnerEntity.Value == null) return;
                        if (weapon.OwnerEntity.Index <= 0) return;
                        int weaponOwner = (int)weapon.OwnerEntity.Index;
                        var pawn = new CBasePlayerPawn(NativeAPI.GetEntityFromIndex(weaponOwner));
                        if (!pawn.IsValid) return;
                        var playerIndex = (int)pawn.Controller.Index;
                        var player = Utilities.GetPlayerFromIndex(playerIndex);
                        if (!(player != null && player.IsValid && !player.IsBot && !player.IsHLTV)) return;
                        var equipment = g_PlayerEquipment[player.SteamID];
                        if (equipment == null) return;
                        if (weapon.AttributeManager == null || weapon.AttributeManager.Item == null) return;

                        var itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
                        var team = player.TeamNum;
                        var knifeKey = $"me_{team}";
                        
                        if (isKnife && equipment.ContainsKey(knifeKey))
                        {
                            itemDef = Convert.ToUInt16(
                                (Int64) equipment[knifeKey]
                            );
                        }
                        
                        var paintKitKey = $"pa_{team}_{itemDef}";
                        var seedKey = $"se_{team}_{itemDef}";
                        var wearKey = $"fl_{team}_{itemDef}";
                        var statTrakKey = $"st_{team}_{itemDef}";
                        var nameTagKey = $"nt_{team}_{itemDef}";
                        var hasPaintKit = equipment.ContainsKey(paintKitKey);

                        weapon.AttributeManager.Item.ItemDefinitionIndex = itemDef;

                        if (isKnife)
                        {
                            weapon.SetModel(GetKnifeModel(itemDef));
                            weapon.AttributeManager.Item.EntityQuality = 3;
                        }

                        weapon.AttributeManager.Item.ItemID = 16384;
                        weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
                        weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;

                        if (hasPaintKit)
                        {
                            var paintKit = Convert.ToInt32((Int64) equipment[paintKitKey]);
                            weapon.FallbackPaintKit = paintKit;
                            if (!isKnife && IsLegacyModel(itemDef, paintKit))
                            {
                                if (weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
                                {
                                    var skeleton = GetSkeletonInstance(weapon.CBodyComponent.SceneNode);
                                    skeleton.ModelState.MeshGroupMask = 2;
                                }
                            }
                        }
                            
                        if (hasPaintKit)
                        {
                            weapon.FallbackSeed = 
                            (
                                equipment.ContainsKey(seedKey)
                                ? Convert.ToInt32((Int64) equipment[seedKey])
                                : 1
                            );
                        }
                        
                        if (hasPaintKit)
                        {
                            if (equipment.ContainsKey(wearKey))
                            {
                                var wear = (
                                    equipment[wearKey] is double
                                    ? (double) equipment[wearKey]
                                    : (int) equipment[wearKey]
                                );
                                weapon.FallbackWear = (float) wear;
                            } else
                            {
                                weapon.FallbackWear = 0.0f;
                            }
                        }

                        if (hasPaintKit)
                        {
                            weapon.FallbackStatTrak = (
                                equipment.ContainsKey(statTrakKey)
                                ? Convert.ToInt32((Int64) equipment[statTrakKey])
                                : -1
                            );
                        }

                        if (hasPaintKit)
                        {
                            SchemaStringMember<CEconItemView> member = new SchemaStringMember<CEconItemView>(
                                weapon.AttributeManager.Item, "CEconItemView", "m_szCustomName"
                            );
                            member.Set(
                                equipment.ContainsKey(nameTagKey)
                                ? (string) equipment[nameTagKey]
                                : ""
                            );
                        }
                    });
                }
            });
            
        }

        /// <summary>
        /// Load the player's equipment when they connect.
        /// </summary>
        [GameEventHandler]
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;

            if (player == null || !player.IsValid || player.IsBot || player.IsHLTV)
            {
                return HookResult.Continue;
            }

            var steamId = player.SteamID;

            Task.Run(async () =>
            {
                await LoadPlayerEquipment(steamId);
            });

            return HookResult.Continue;
        }

        /// <summary>
        /// Reapply the music kit to the player when they spawn. It used to have
        /// a bug on CS:GO that the music kit would go away when spraying. Need
        /// to check if this is still the case on CS2.
        /// </summary>
        [GameEventHandler]
        public HookResult OnPlayerSpawned(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot || player.PlayerPawn == null || !player.PlayerPawn.IsValid)
            {
                return HookResult.Continue;
            }

            ApplyMusicKit(player);

            return HookResult.Continue;
        }

        /// <summary>
        /// Remove the player's equipment when they disconnect.
        /// </summary>
        [GameEventHandler]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (player == null || !player.IsValid || player.IsBot || player.PlayerPawn == null || !player.PlayerPawn.IsValid)
            {
                return HookResult.Continue;
            }

            g_PlayerEquipment.Remove(player.SteamID);

            return HookResult.Continue;
        }

        /// <summary>
        /// Load economy items from cslib's CDN.
        /// </summary>
        public async Task LoadEconomyItems()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string apiUrl = "https://cdn.statically.io/gh/ianlucas/cslib/main/assets/data/items.json";
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    string jsonContent = await response.Content.ReadAsStringAsync();
                    g_EconomyItems = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CS_Item>>(jsonContent);
                    Logger.LogInformation("Loaded economy items from cslib's CDN.");

                    if (g_EconomyItems != null)
                    {
                        foreach (var item in g_EconomyItems)
                        {
                            if (item.Type == "weapon" && item.Def != null && item.Index != null)
                            {
                                if (!g_LegacyItemDefPaintKit.ContainsKey((ushort) item.Def))
                                {
                                    g_LegacyItemDefPaintKit[(ushort) item.Def] = new Dictionary<int, bool>();
                                }

                                g_LegacyItemDefPaintKit[(ushort) item.Def][(int) item.Index] = item.Legacy == true;
                            }
                            if (
                                item.Type == "melee"
                                && item.Def != null
                                && item.Model != null
                                && !g_ItemDefModel.ContainsKey((ushort) item.Def)
                            )
                            {
                                g_ItemDefModel[(ushort) item.Def] = (string) item.Model;
                            }
                        }
                    }
                    
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching economy items from cslib's CDN: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a player's equipment from inventory.cstrike.app.
        /// </summary>
        public async Task LoadPlayerEquipment(ulong steamId)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string userApiUrl = $"https://inventory.cstrike.app/api/equipped/{steamId}.json";
                    HttpResponseMessage response = await client.GetAsync(userApiUrl);
                    response.EnsureSuccessStatusCode();

                    string jsonContent = await response.Content.ReadAsStringAsync();
                    var userData = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                    g_PlayerEquipment[steamId] = userData;
                    Logger.LogInformation($"Loaded player {steamId} equipment");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching player {steamId} equipment: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply the music kit to a given player.
        /// </summary>
        public void ApplyMusicKit(CCSPlayerController player)
        {
            if (player.InventoryServices == null) return;
            var equipment = g_PlayerEquipment[player.SteamID];
            if (equipment == null || !equipment.ContainsKey("mk")) return;
            player.InventoryServices.MusicID = Convert.ToUInt16(
                (Int64) equipment["mk"]
            );
        }

        /// <summary>
        /// Check if a given item definition and paint kit is using the legacy model (aka CS:GO model).
        /// </summary>
        public bool IsLegacyModel(ushort itemDef, Int32 paintKit)
        {
            if (g_LegacyItemDefPaintKit.TryGetValue(itemDef, out var paintKitDict))
            {
                if (paintKitDict.TryGetValue(paintKit, out var isLegacy))
                {
                    return isLegacy;
                }
            }
            return false;
        }

        /// <summary>
        /// Get the model for a given item definition.
        /// </summary>
        public string GetItemDefModel(ushort itemDef)
        {
            if (g_ItemDefModel.TryGetValue(itemDef, out var model))
            {
                return model != null ? model : "";
            }
            return "";
        }

        /// <summary>
        /// Get the knife model for a given item definition.
        /// </summary>
        public string GetKnifeModel(ushort itemDef)
        {
            var itemModel = GetItemDefModel(itemDef);
            if (itemModel == "") return "";
            var modelName = itemModel == "bayonet" ? "knife_bayonet" : itemModel;
            return $"weapons/models/knife/{modelName}/weapon_{modelName}.vmdl";
        }

        /// <summary>
        /// Get the skeleton instance of a scene node.
        /// This is a hack by Nereziel/daffyyyy.
        /// </summary>
        private static CSkeletonInstance GetSkeletonInstance(CGameSceneNode node)
        {
            Func<nint, nint> GetSkeletonInstance = VirtualFunction.Create<nint, nint>(node.Handle, 8);
            return new CSkeletonInstance(GetSkeletonInstance(node.Handle));
        }

        /// <summary>
        /// Get a fixed array of CBaseViewModels.
        /// This is a hack by KillStr3aK.
        /// </summary>
        public unsafe T[] GetFixedArray<T>(nint pointer, string @class, string member, int length) where T : CHandle<CBaseViewModel>
        {
            nint ptr = pointer + Schema.GetSchemaOffset(@class, member);
            Span<nint> references = MemoryMarshal.CreateSpan<nint>(ref ptr, length);
            T[] values = new T[length];

            for (int i = 0; i < length; i++)
            {
                values[i] = (T)Activator.CreateInstance(typeof(T), references[i])!;
            }

            return values;
        }

        /// <summary>
        /// Get the player's view models.
        /// This is a hack by KillStr3aK.
        /// </summary>
        public unsafe CHandle<CBaseViewModel>[]? GetPlayerViewModels(CCSPlayerController player)
        {
            if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null) return null;
            CCSPlayer_ViewModelServices viewModelServices = new CCSPlayer_ViewModelServices(player.PlayerPawn.Value.ViewModelServices!.Handle);
            return GetFixedArray<CHandle<CBaseViewModel>>(viewModelServices.Handle, "CCSPlayer_ViewModelServices", "m_hViewModel", 3);
        }
    }

    /// <summary>
    /// A class to manipulate a string in memory.
    /// We currently need it to update string members.
    /// This is a hack by Iksix.
    /// </summary>
    public class SchemaStringMember<SchemaClass> : NativeObject where SchemaClass : NativeObject
    {
        public SchemaStringMember(SchemaClass instance, string className, string member) : base(Schema.GetSchemaValue<nint>(instance.Handle, className, member))
        { }

        public unsafe void Set(string str)
        {
            byte[] bytes = this.GetStringBytes(str);

            for (int i = 0; i < bytes.Length; i++)
            {
                Unsafe.Write((void*)(this.Handle.ToInt64() + i), bytes[i]);
            }

            Unsafe.Write((void*)(this.Handle.ToInt64() + bytes.Length), 0);
        }

        private byte[] GetStringBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }
    }

    /// <summary>
    /// A Counter-Strike item definition from @ianlucas/cslib.
    /// <see href="https://github.com/ianlucas/cslib/blob/9a4a3d8ea29778fb7b8bd0af934ec3dc40024b1b/src/economy.ts#L9-L44" />
    /// </summary>
    public class CS_Item
    {
        [JsonProperty("altname")]
        public string? AltName { get; set; }

        [JsonProperty("base")]
        public bool? Base { get; set; }

        [JsonProperty("category")]
        public string? Category { get; set; }

        [JsonProperty("contents")]
        public List<int>? Contents { get; set; }

        [JsonProperty("def")]
        public ushort? Def { get; set; }

        [JsonProperty("free")]
        public bool? Free { get; set; }

        [JsonProperty("id")]
        public int? Id { get; set; }

        [JsonProperty("image")]
        public string? Image { get; set; }

        [JsonProperty("index")]
        public int? Index { get; set; }

        [JsonProperty("keys")]
        public List<int>? Keys { get; set; }

        [JsonProperty("legacy")]
        public bool? Legacy { get; set; }

        [JsonProperty("localimage")]
        public bool? LocalImage { get; set; }

        [JsonProperty("model")]
        public string? Model { get; set; }

        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("specialcontents")]
        public List<int>? SpecialContents { get; set; }

        [JsonProperty("specialimage")]
        public int? SpecialImage { get; set; }

        [JsonProperty("rarity")]
        public string? Rarity { get; set; }

        [JsonProperty("teams")]
        public List<int>? Teams { get; set; }

        [JsonProperty("tint")]
        public int? Tint { get; set; }

        [JsonProperty("type")]
        public string? Type { get; set; }

        [JsonProperty("wearmax")]
        public float? WearMax { get; set; }

        [JsonProperty("wearmin")]
        public float? WearMin { get; set; }
    }
}
