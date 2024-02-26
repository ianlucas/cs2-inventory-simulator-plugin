/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace InventorySimulator
{
    public class InventorySimulator : BasePlugin
    {
        public override string ModuleAuthor => "Ian Lucas";
        public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
        public override string ModuleName => "InventorySimulator";
        public override string ModuleVersion => "0.0.1";

        private List<CS_Item>? g_Items;
        private readonly Dictionary<ulong, PlayerEquipment> g_PlayerEquipment = new();
        private readonly Dictionary<ushort, Dictionary<int, bool>> g_LegacyItemDefPaintKit = new();
        private readonly Dictionary<ushort, string> g_ItemDefModel = new();
        private readonly bool g_IsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT || Environment.OSVersion.Platform == PlatformID.Win32Windows;

        public override void Load(bool hotReload)
        {
            Task.Run(async () =>
            {
                await FetchEconomyItems();
            });

            RegisterListener<Listeners.OnTick>(() =>
            {
                foreach (var player in Utilities.GetPlayers())
                {
                    try {
                        if (!IsValidHumanPlayer(player)) continue;

                        var viewModels = GetPlayerViewModels(player);
                        if (viewModels == null) continue;

                        var viewModel = viewModels[0];
                        if (viewModel == null || viewModel.Value == null || viewModel.Value.Weapon == null || viewModel.Value.Weapon.Value == null) continue;

                        CBasePlayerWeapon weapon = viewModel.Value.Weapon.Value;
                        if (weapon == null || !weapon.IsValid) continue;
                        
                        var equipment = GetPlayerEquipment(player);
                        var isKnife = viewModel.Value.VMName.Contains("knife");
                        var itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
                        var team = player.TeamNum;

                        if (!isKnife)
                        {
                            if (!equipment.HasProperty("pa", team, itemDef)) continue;

                            // Looks like changing the weapon's MeshGroupMask during creation is not enough, so we
                            // force it every tick to make sure we're displaying the right model on player's POV.
                            UpdateWeaponModel(viewModel.Value, IsLegacyModel(itemDef, weapon.FallbackPaintKit));
                            Utilities.SetStateChanged(viewModel.Value, "CBaseEntity", "m_CBodyComponent");
                        } else
                        {
                            if (!g_IsWindows || !equipment.HasProperty("me", team)) continue;
                            // In Windows, we cannot give knives using GiveNamedItem, so we force into the viewmodel
                            // using the SetModel function. A caveat is that the animations are broken and the player
                            // will always see the rarest deploy animation.
                            var newModel = GetKnifeModel(itemDef);
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

            RegisterListener<Listeners.OnEntityCreated>(entity =>
            {
                var designerName = entity.DesignerName;

                if (designerName.Contains("weapon")) {
                    var isKnife = IsKnifeClassName(designerName);

                    Server.NextFrame(() =>
                    {
                        var weapon = new CBasePlayerWeapon(entity.Handle);
                        if (!weapon.IsValid || weapon.OwnerEntity.Value == null || weapon.OwnerEntity.Index <= 0 || weapon.AttributeManager == null || weapon.AttributeManager.Item == null) return;

                        int weaponOwner = (int)weapon.OwnerEntity.Index;
                        var pawn = new CBasePlayerPawn(NativeAPI.GetEntityFromIndex(weaponOwner));
                        if (!pawn.IsValid) return;

                        var playerIndex = (int)pawn.Controller.Index;
                        var player = Utilities.GetPlayerFromIndex(playerIndex);
                        if (!IsValidHumanPlayer(player)) return;

                        var equipment = GetPlayerEquipment(player);
                        var itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
                        var team = player.TeamNum;
                        var hasKnife = equipment.HasProperty("me", team);

                        if (isKnife && !hasKnife) return;
                        
                        var hasNametag = equipment.HasProperty("nt", team, itemDef);
                        var hasPaintKit = equipment.HasProperty("pa", team, itemDef);
                        var hasStickers = equipment.HasProperty("ss", team, itemDef);
                        var hasWear = equipment.HasProperty("fl", team, itemDef);
                        var hasSeed = equipment.HasProperty("se", team, itemDef);
                        var hasStatTrak = equipment.HasProperty("st", team, itemDef);
                        var isCustomItem = hasKnife || hasPaintKit || hasNametag || hasStickers || hasWear || hasSeed || hasStatTrak;
                        
                        if (!isKnife && !isCustomItem) return;
                        
                        if (isKnife)
                        {
                            itemDef = equipment.GetUShort("me", team);
                            weapon.AttributeManager.Item.ItemDefinitionIndex = itemDef;
                            if (g_IsWindows)
                            {
                                weapon.SetModel(GetKnifeModel(itemDef));
                            }
                            weapon.AttributeManager.Item.EntityQuality = 3;
                        }

                        weapon.AttributeManager.Item.ItemID = 16384;
                        weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
                        weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;

                        var paintKit = equipment.GetInt("pa", team, itemDef, 0);
                        weapon.FallbackPaintKit = paintKit;

                        if (!isKnife && IsLegacyModel(itemDef, paintKit))
                        {
                            UpdateWeaponModel(weapon, true);
                        }
                            
                        weapon.FallbackSeed = equipment.GetInt("se", team, itemDef, 1);
                        weapon.FallbackWear = equipment.GetFloat("fl", team, itemDef, 0.0f);
                        weapon.FallbackStatTrak = equipment.GetInt("st", team, itemDef, -1);
                        weapon.AttributeManager.Item.CustomName = equipment.GetString("nt", team, itemDef, "");
                    });
                }
            });
            
        }

        [GameEventHandler]
        public HookResult OnPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;

            if (!IsValidHumanPlayer(player))
                return HookResult.Continue;

            var steamId = player.SteamID;

            Task.Run(async () =>
            {
                await FetchPlayerEquipment(steamId);
            });

            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Pre)]
        public HookResult OnPlayerSpawnedPre(EventPlayerSpawn @event, GameEventInfo info)
        {
            // Plugin takes care of giving knives to players.
            // This is a workaround for the weapon removal crash issue.
            Server.ExecuteCommand("mp_ct_default_melee \"\"");
            Server.ExecuteCommand("mp_t_default_melee \"\"");

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnPlayerSpawned(EventPlayerSpawn @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (!IsValidPlayer(player) || !IsValidPlayerPawn(player))
                return HookResult.Continue;

            ApplyMusicKit(player);
            ApplyGloves(player);
            ApplyKnife(player);

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
        {
            CCSPlayerController? player = @event.Userid;
            if (IsValidHumanPlayer(player))
            {
                g_PlayerEquipment.Remove(player.SteamID);
            }

            return HookResult.Continue;
        }

        public async Task FetchEconomyItems()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string apiUrl = "https://cdn.statically.io/gh/ianlucas/cslib/main/assets/data/items.json";
                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    string jsonContent = await response.Content.ReadAsStringAsync();
                    g_Items = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CS_Item>>(jsonContent);
                    Logger.LogInformation("Loaded economy items from cslib's CDN.");

                    if (g_Items != null)
                    {
                        foreach (var item in g_Items)
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

        public async Task FetchPlayerEquipment(ulong steamId)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    string userApiUrl = $"https://inventory.cstrike.app/api/equipped/{steamId}.json";
                    HttpResponseMessage response = await client.GetAsync(userApiUrl);
                    response.EnsureSuccessStatusCode();

                    string jsonContent = await response.Content.ReadAsStringAsync();
                    var equipment = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonContent);

                    g_PlayerEquipment[steamId] = new PlayerEquipment(equipment);
                    Logger.LogInformation($"Loaded player {steamId} equipment");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error fetching player {steamId} equipment: {ex.Message}");
            }
        }

        public void ApplyMusicKit(CCSPlayerController player)
        {
            if (player.InventoryServices == null) return;
            var equipment = GetPlayerEquipment(player);
            if (!equipment.HasProperty("mk")) return;
            player.InventoryServices.MusicID = equipment.GetUShort("mk");
        }

        public void ApplyGloves(CCSPlayerController player)
        {
            var equipment = GetPlayerEquipment(player);

            var team = player.TeamNum;
            if (!equipment.HasProperty("gl", team)) return;

            var itemDef = equipment.GetUShort("gl", team);
            if (!equipment.HasProperty("pa", team, itemDef)) return;

            var glove = player.PlayerPawn.Value!.EconGloves;
            glove.ItemDefinitionIndex = itemDef;
            glove.ItemIDLow = 16384 & 0xFFFFFFFF;
            glove.ItemIDHigh = 16384 >> 32;

            Server.NextFrame(() =>
            {
                glove.Initialized = true;
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture prefab", equipment.GetInt("pa", team, itemDef, 0));
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture seed", equipment.GetInt("se", team, itemDef, 1));
                SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture wear", equipment.GetFloat("fl", team, itemDef, 1));
                SetBodygroup(player, "default_gloves", 1);
            });
        }

        public void ApplyKnife(CCSPlayerController player)
        {
            if (HasKnife(player))
            {
                return;
            }

            var equipment = GetPlayerEquipment(player);
            var team = player.TeamNum;
            // On Windows we cannot give knives using GiveNamedItems, still no
            // explanation from a C++/RE expert. We could use subclass_change, but
            // from my testing it'd require a full client update to show the skin.
            // Until someone figure this out, on Windows we force the knife on the
            // viewmodel.
            if (g_IsWindows || !equipment.HasProperty("me", team))
            {
                var suffix = (team == 2 ? "_t" : "");
                player.GiveNamedItem($"weapon_knife{suffix}");
                return;
            }

            var model = GetItemDefModel(equipment.GetUShort("me", team));
            player.GiveNamedItem($"weapon_{model}");
        }

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

        public string GetItemDefModel(ushort itemDef)
        {
            if (g_ItemDefModel.TryGetValue(itemDef, out var model))
            {
                return model != null ? model : "";
            }
            return "";
        }

        public string GetKnifeModel(ushort itemDef)
        {
            var itemModel = GetItemDefModel(itemDef);
            if (itemModel == "") return "";
            var modelName = itemModel == "bayonet" ? "knife_bayonet" : itemModel;
            return $"weapons/models/knife/{modelName}/weapon_{modelName}.vmdl";
        }

        public void UpdateWeaponModel(CBaseEntity weapon, bool isLegacy)
        {
            if (weapon.CBodyComponent != null && weapon.CBodyComponent.SceneNode != null)
            {
                var skeleton = weapon.CBodyComponent.SceneNode.GetSkeletonInstance();
                if (skeleton != null)
                {
                    skeleton.ModelState.MeshGroupMask = (ulong)(isLegacy ? 2 : 1);
                }
            }
        }

        // This is hack by KillStr3aK.
        public unsafe CHandle<CBaseViewModel>[] GetViewModelFixedArray(nint pointer, string @class, string member, int length)
        {
            nint ptr = pointer + Schema.GetSchemaOffset(@class, member);
            Span<nint> references = MemoryMarshal.CreateSpan<nint>(ref ptr, length);
            CHandle<CBaseViewModel>[] values = new CHandle<CBaseViewModel>[length];

            for (int i = 0; i < length; i++)
            {
                values[i] = (CHandle<CBaseViewModel>)Activator.CreateInstance(typeof(CHandle<CBaseViewModel>), references[i])!;
            }

            return values;
        }

        // This is a hack by KillStr3aK.
        public unsafe CHandle<CBaseViewModel>[]? GetPlayerViewModels(CCSPlayerController player)
        {
            if (player.PlayerPawn.Value == null || player.PlayerPawn.Value.ViewModelServices == null) return null;
            CCSPlayer_ViewModelServices viewModelServices = new CCSPlayer_ViewModelServices(player.PlayerPawn.Value.ViewModelServices!.Handle);
            return GetViewModelFixedArray(viewModelServices.Handle, "CCSPlayer_ViewModelServices", "m_hViewModel", 3);
        }

        // This was made public by skuzzis.
        // CS# public implementation by stefanx111.
        public void SetOrAddAttributeValueByName(CAttributeList attributeList, string name, float value)
        {
            var SetOrAddAttributeValueByNameFunc = VirtualFunction.Create<IntPtr, string, float, int>(GameData.GetSignature("CAttributeList_SetOrAddAttributeValueByName"));
            SetOrAddAttributeValueByNameFunc(attributeList.Handle, name, value);
        }

        // This was made public by skuzzis.
        // CS# public implementation by stefanx111.
        public void SetBodygroup(CCSPlayerController player, string model, int i)
        {
            var SetBodygroupFunc = VirtualFunction.Create<IntPtr, string, int, int>(GameData.GetSignature("CBaseModelEntity_SetBodygroup"));
            SetBodygroupFunc(player.PlayerPawn.Value!.Handle, model, i);
        }

        public PlayerEquipment GetPlayerEquipment(CCSPlayerController player)
        {
            if (g_PlayerEquipment.TryGetValue(player.SteamID, out var equipment))
            {
                return equipment;
            }
            return new PlayerEquipment(null);
        }

        public bool IsKnifeClassName(string className)
        {
            return className.Contains("bayonet") || className.Contains("knife");
        }

        public bool IsValidPlayer(CCSPlayerController? player)
        {
            return player != null && player.IsValid && !player.IsHLTV;
        }

        public bool IsValidHumanPlayer(CCSPlayerController? player)
        {
            return IsValidPlayer(player) && !player!.IsBot;
        }

        public bool IsValidPlayerPawn(CCSPlayerController player)
        {
            return player.PlayerPawn != null && player.PlayerPawn.IsValid;
        }

        public bool HasKnife(CCSPlayerController player)
        {
            foreach (var weapon in player.PlayerPawn.Value!.WeaponServices!.MyWeapons)
            {
                if (weapon is { IsValid: true, Value.IsValid: true })
                {
                    if (IsKnifeClassName(weapon.Value.DesignerName))
                        return true;
                }
            }
            return false;
        }
    }

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

        [JsonProperty("specials")]
        public List<int>? Specials { get; set; }

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

    public class PlayerEquipment
    {
        public Dictionary<string, object>? Equipment { get; set; }

        public PlayerEquipment(Dictionary<string, object>? equipment)
        {
            Equipment = equipment ?? new Dictionary<string, object>();
        }

        public bool HasProperty(string prefix, byte team)
        {
            if (Equipment == null) return false;
            return Equipment.ContainsKey($"{prefix}_{team}");
        }

        public bool HasProperty(string prefix, byte team, ushort itemDef)
        {
            if (Equipment == null) return false;
            return Equipment.ContainsKey($"{prefix}_{team}_{itemDef}");
        }

        public bool HasProperty(string prefix)
        {
            if (Equipment == null) return false;
            return Equipment.ContainsKey(prefix);
        }
    
        public ushort GetUShort(string prefix, byte team)
        {
            if (Equipment == null) return 0;
            var key = $"{prefix}_{team}";
            return Convert.ToUInt16((Int64)Equipment[key]);
        }

        public ushort GetUShort(string prefix)
        {
            if (Equipment == null || !Equipment.ContainsKey(prefix)) return 0;
            return Convert.ToUInt16((Int64)Equipment[prefix]);
        }

        public int GetInt(string prefix, byte team, ushort itemDef, int defaultValue)
        {
            var key = $"{prefix}_{team}_{itemDef}";
            if (Equipment == null || !Equipment.ContainsKey(key)) return defaultValue;
            return Convert.ToInt32((Int64)Equipment[key]);
        }

        public float GetFloat(string prefix, byte team, ushort itemDef, float defaultValue)
        {
            var key = $"{prefix}_{team}_{itemDef}";
            if (Equipment == null || !Equipment.ContainsKey(key)) return defaultValue;
            return Equipment[key] switch
            {
                double d => (float)d,
                int i => (float)i,
                _ => defaultValue
            };
        }

        public string GetString(string prefix, byte team, ushort itemDef, string defaultValue)
        {
            var key = $"{prefix}_{team}_{itemDef}";
            if (Equipment == null || !Equipment.ContainsKey(key)) return defaultValue;
            return (string)Equipment[key];
        }
    }
}
