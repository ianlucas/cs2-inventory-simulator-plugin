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
using System.Runtime.InteropServices;

namespace InventorySimulator;

public class InventorySimulator : BasePlugin
{
    public override string ModuleAuthor => "Ian Lucas";
    public override string ModuleDescription => "Inventory Simulator (inventory.cstrike.app)";
    public override string ModuleName => "InventorySimulator";
    public override string ModuleVersion => "0.0.1";

    private readonly Dictionary<ulong, PlayerInventory> g_PlayerInventory = new();
    private Dictionary<ushort, List<int>> g_LookupWeaponLegacy = new();
    private Dictionary<ushort, string> g_LookupWeaponModel = new();
    private Dictionary<ushort, string> g_LookupAgentModel = new();
    private readonly bool g_IsWindows = Environment.OSVersion.Platform == PlatformID.Win32NT || Environment.OSVersion.Platform == PlatformID.Win32Windows;

    public override void Load(bool hotReload)
    {
        Task.Run(async () =>
        {
            await FetchLookupWeaponLegacy();
            await FetchLookupWeaponModel();
            await FetchLookupAgentModel();
        });

        RegisterListener<Listeners.OnTick>(() =>
        {
            foreach (var player in Utilities.GetPlayers())
            {
                try
                {
                    if (!IsValidHumanPlayer(player)) continue;

                    var viewModels = GetPlayerViewModels(player);
                    if (viewModels == null) continue;

                    var viewModel = viewModels[0];
                    if (viewModel == null || viewModel.Value == null || viewModel.Value.Weapon == null || viewModel.Value.Weapon.Value == null) continue;

                    CBasePlayerWeapon weapon = viewModel.Value.Weapon.Value;
                    if (weapon == null || !weapon.IsValid) continue;

                    var inventory = GetPlayerInventory(player);
                    var isKnife = viewModel.Value.VMName.Contains("knife");
                    var itemDef = weapon.AttributeManager.Item.ItemDefinitionIndex;
                    var team = player.TeamNum;

                    if (!isKnife)
                    {
                        if (!inventory.HasProperty("pa", team, itemDef)) continue;

                        // Looks like changing the weapon's MeshGroupMask during creation is not enough, so we
                        // force it every tick to make sure we're displaying the right model on player's POV.
                        UpdateWeaponModel(viewModel.Value, IsLegacyModel(itemDef, weapon.FallbackPaintKit));
                        Utilities.SetStateChanged(viewModel.Value, "CBaseEntity", "m_CBodyComponent");
                    }
                    else
                    {
                        if (!g_IsWindows || !inventory.HasProperty("me", team)) continue;
                        // In Windows, we cannot give knives using GiveNamedItem, so we force into the viewmodel
                        // using the SetModel function. A caveat is that the animations are broken and the player
                        // will always see the rarest deploy animation.
                        var newModel = GetKnifeModel(itemDef);
                        if (newModel != null && viewModel.Value.VMName != newModel)
                        {
                            viewModel.Value.VMName = newModel;
                            viewModel.Value.SetModel(newModel);
                        }
                    }
                }
                catch (Exception)
                { }
            }
        });

        RegisterListener<Listeners.OnEntityCreated>(entity =>
        {
            var designerName = entity.DesignerName;

            if (designerName.Contains("weapon"))
            {
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
                            var model = GetKnifeModel(itemDef);
                            if (model != null)
                            {
                                weapon.SetModel(model);
                            }
                        }
                        weapon.AttributeManager.Item.EntityQuality = 3;
                    }

                    weapon.AttributeManager.Item.ItemID = 16384;
                    weapon.AttributeManager.Item.ItemIDLow = 16384 & 0xFFFFFFFF;
                    weapon.AttributeManager.Item.ItemIDHigh = weapon.AttributeManager.Item.ItemIDLow >> 32;

                    var paintKit = inventory.GetInt("pa", team, itemDef, 0);
                    weapon.FallbackPaintKit = paintKit;

                    if (!isKnife && IsLegacyModel(itemDef, paintKit))
                    {
                        UpdateWeaponModel(weapon, true);
                    }

                    weapon.FallbackSeed = inventory.GetInt("se", team, itemDef, 1);
                    weapon.FallbackWear = inventory.GetFloat("fl", team, itemDef, 0.0f);
                    weapon.FallbackStatTrak = inventory.GetInt("st", team, itemDef, -1);
                    weapon.AttributeManager.Item.CustomName = inventory.GetString("nt", team, itemDef, "");
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

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        CCSPlayerController? player = @event.Userid;
        if (!IsValidPlayer(player) || !IsValidPlayerPawn(player))
            return HookResult.Continue;

        ApplyMusicKit(player);
        ApplyAgent(player);
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
            g_PlayerInventory.Remove(player.SteamID);
        }

        return HookResult.Continue;
    }

    public async Task<T?> Fetch<T>(string url)
    {
        try
        {
            using HttpClient client = new();
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            string jsonContent = await response.Content.ReadAsStringAsync();
            T? data = JsonConvert.DeserializeObject<T>(jsonContent);
            return data;
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error fetching data from {url}: {ex.Message}");
            return default;
        }
    }

    public async Task FetchLookupWeaponLegacy()
    {
        var lookupWeaponLegacy = await Fetch<Dictionary<ushort, List<int>>>(
            "https://raw.githubusercontent.com/ianlucas/cslib/main/assets/data/lookup-weapon-legacy.json"
        );
        if (lookupWeaponLegacy != null)
        {
            g_LookupWeaponLegacy = lookupWeaponLegacy;
        }
    }

    public async Task FetchLookupWeaponModel()
    {
        var lookupWeaponModel = await Fetch<Dictionary<ushort, string>>(
            "https://raw.githubusercontent.com/ianlucas/cslib/main/assets/data/lookup-weapon-model.json"
        );
        if (lookupWeaponModel != null)
        {
            g_LookupWeaponModel = lookupWeaponModel;
        }
    }

    public async Task FetchLookupAgentModel()
    {
        var lookupAgentModel = await Fetch<Dictionary<ushort, string>>(
            "https://raw.githubusercontent.com/ianlucas/cslib/main/assets/data/lookup-agent-model.json"
        );
        if (lookupAgentModel != null)
        {
            foreach (var entry in lookupAgentModel)
            {
                var model = $"characters/models/{entry.Value}.vmdl";
                g_LookupAgentModel.Add(entry.Key, model);
            }
        }
    }

    public async Task FetchPlayerInventory(ulong steamId)
    {
        var playerInventory = await Fetch<Dictionary<string, object>>(
            $"https://inventory.cstrike.app/api/equipped/{steamId}.json"
        );
        if (playerInventory != null)
        {
            g_PlayerInventory[steamId] = new PlayerInventory(playerInventory);
        }
    }

    public void ApplyMusicKit(CCSPlayerController player)
    {
        if (player.InventoryServices == null) return;
        var inventory = GetPlayerInventory(player);
        if (!inventory.HasProperty("mk")) return;
        player.InventoryServices.MusicID = inventory.GetUShort("mk");
    }

    public void ApplyGloves(CCSPlayerController player)
    {
        var inventory = GetPlayerInventory(player);

        var team = player.TeamNum;
        if (!inventory.HasProperty("gl", team)) return;

        var itemDef = inventory.GetUShort("gl", team);
        if (!inventory.HasProperty("pa", team, itemDef)) return;

        var glove = player.PlayerPawn.Value!.EconGloves;
        glove.ItemDefinitionIndex = itemDef;
        glove.ItemIDLow = 16384 & 0xFFFFFFFF;
        glove.ItemIDHigh = 16384 >> 32;

        Server.NextFrame(() =>
        {
            glove.Initialized = true;
            SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture prefab", inventory.GetInt("pa", team, itemDef, 0));
            SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture seed", inventory.GetInt("se", team, itemDef, 1));
            SetOrAddAttributeValueByName(glove.NetworkedDynamicAttributes, "set item texture wear", inventory.GetFloat("fl", team, itemDef, 1));
            SetBodygroup(player, "default_gloves", 1);
        });
    }

    public void ApplyKnife(CCSPlayerController player)
    {
        if (HasKnife(player))
        {
            return;
        }

        var inventory = GetPlayerInventory(player);
        var team = player.TeamNum;
        // On Windows we cannot give knives using GiveNamedItems, still no
        // explanation from a C++/RE expert. We could use subclass_change, but
        // from my testing it'd require a full client update to show the skin.
        // Until someone figure this out, on Windows we force the knife on the
        // viewmodel.
        if (g_IsWindows || !inventory.HasProperty("me", team))
        {
            var suffix = team == 2 ? "_t" : "";
            player.GiveNamedItem($"weapon_knife{suffix}");
            return;
        }

        var model = GetItemDefModel(inventory.GetUShort("me", team));
        if (model != null)
        {
            player.GiveNamedItem(model);
        }
    }

    public void ApplyAgent(CCSPlayerController player)
    {
        var team = player.TeamNum;
        var inventory = GetPlayerInventory(player);
        if (!inventory.HasProperty("ag", team)) return;

        try
        {
            var model = GetAgentModel(inventory.GetUShort("ag", team));
            if (model != null)
            {
                Server.NextFrame(() =>
                {
                    player.PlayerPawn.Value!.SetModel(model);
                });
            }
        }
        catch (Exception)
        {
            Logger.LogInformation($"Could not set player model for {player.PlayerName}");
        }
    }

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
        Span<nint> references = MemoryMarshal.CreateSpan(ref ptr, length);
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

    public PlayerInventory GetPlayerInventory(CCSPlayerController player)
    {
        if (g_PlayerInventory.TryGetValue(player.SteamID, out var inventory))
        {
            return inventory;
        }
        return new PlayerInventory(null);
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

