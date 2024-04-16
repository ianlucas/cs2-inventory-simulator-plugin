/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.Json;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public readonly string InventoryFilePath = "csgo/css_inventories.json";
    public readonly Dictionary<ulong, PlayerInventory> InventoryManager = new();
    public readonly Dictionary<ulong, MusicKitItem> MusicKitManager = new();
    public readonly HashSet<ulong> LoadedSteamIds = new();
    public readonly PlayerInventory EmptyInventory = new();

    public void LoadPlayerInventories()
    {
        try
        {
            var path = Path.Combine(Server.GameDirectory, InventoryFilePath);
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            var inventories = JsonConvert.DeserializeObject<Dictionary<ulong, PlayerInventory>>(json);
            if (inventories != null)
            {
                foreach (var pair in inventories)
                {
                    LoadedSteamIds.Add(pair.Key);
                    AddPlayerInventory(pair.Key, pair.Value);
                }
            }
        }
        catch
        {
            Logger.LogError($"Error when processing \"css_inventories.json\".");
        }
    }

    public void AddPlayerInventory(ulong steamId, PlayerInventory inventory)
    {
        InventoryManager.Add(steamId, inventory);
        if (inventory.MusicKit != null)
            MusicKitManager.Add(steamId, inventory.MusicKit);
        else MusicKitManager.Remove(steamId);
    }

    public void ClearInventoryManager()
    {
        var connected = Utilities.GetPlayers().Select(player => player.SteamID).ToHashSet();
        var disconnected = InventoryManager.Keys.Except(connected).ToList();
        foreach (var steamId in disconnected)
        {
            RemovePlayerInventory(steamId);
        }
    }

    public void RemovePlayerInventory(ulong steamId)
    {
        if (!LoadedSteamIds.Contains(steamId))
        {
            InventoryManager.Remove(steamId);
            MusicKitManager.Remove(steamId);
        }
    }

    public PlayerInventory GetPlayerInventory(CCSPlayerController player)
    {
        if (InventoryManager.TryGetValue(player.SteamID, out var inventory))
        {
            return inventory;
        }
        return EmptyInventory;
    }

    public float ViewAsFloat(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return BitConverter.ToSingle(bytes, 0);
    }

    public float ViewAsFloat(uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return BitConverter.ToSingle(bytes, 0);
    }
}

public class StickerItem
{
    [JsonProperty("def")]
    public uint Def { get; set; }

    [JsonProperty("slot")]
    public ushort Slot { get; set; }

    [JsonProperty("wear")]
    public float Wear { get; set; }
}

public class BaseEconItem
{
    [JsonProperty("def")]
    public ushort Def { get; set; }

    [JsonProperty("paint")]
    public int Paint { get; set; }

    [JsonProperty("seed")]
    public int Seed { get; set; }

    [JsonProperty("wear")]
    public float Wear { get; set; }
}

public class WeaponEconItem : BaseEconItem
{
    [JsonProperty("legacy")]
    public bool Legacy { get; set; }

    [JsonProperty("nametag")]
    public required string Nametag { get; set; }

    [JsonProperty("stattrak")]
    public required int Stattrak { get; set; }

    [JsonProperty("stickers")]
    public required List<StickerItem> Stickers { get; set; }

    [JsonProperty("uid")]
    public required int Uid { get; set; }
}

public class AgentItem
{
    [JsonProperty("model")]
    public required string Model { get; set; }

    [JsonProperty("patches")]
    public required List<uint> Patches { get; set; }

    [JsonProperty("vofallback")]
    public required bool VoFallback { get; set; }

    [JsonProperty("vofemale")]
    public required bool VoFemale { get; set; }

    [JsonProperty("voprefix")]
    public required string VoPrefix { get; set; }
}

public class MusicKitItem
{
    [JsonProperty("def")]
    public int Def { get; set; }

    [JsonProperty("stattrak")]
    public required int Stattrak { get; set; }

    [JsonProperty("uid")]
    public required int Uid { get; set; }
}

public class PlayerInventory
{
    [JsonProperty("knives")]
    public Dictionary<byte, WeaponEconItem> Knives { get; set; }

    [JsonProperty("gloves")]
    public Dictionary<byte, BaseEconItem> Gloves { get; set; }

    [JsonProperty("tWeapons")]
    public Dictionary<ushort, WeaponEconItem> TWeapons { get; set; }

    [JsonProperty("ctWeapons")]
    public Dictionary<ushort, WeaponEconItem> CTWeapons { get; set; }

    [JsonProperty("agents")]
    public Dictionary<byte, AgentItem> Agents { get; set; }

    [JsonProperty("pin")]
    public uint? Pin { get; set; }

    [JsonProperty("musicKit")]
    public MusicKitItem? MusicKit { get; set; }

    [JsonConstructor]
    public PlayerInventory(
        Dictionary<byte, WeaponEconItem>? knives = null,
        Dictionary<byte, BaseEconItem>? gloves = null,
        Dictionary<ushort, WeaponEconItem>? tWeapons = null,
        Dictionary<ushort, WeaponEconItem>? ctWeapons = null,
        Dictionary<byte, AgentItem>? agents = null,
        uint? pin = null,
        MusicKitItem? musicKit = null)
    {
        Knives = knives ?? new();
        Gloves = gloves ?? new();
        TWeapons = tWeapons ?? new();
        CTWeapons = ctWeapons ?? new();
        Agents = agents ?? new();
        Pin = pin;
        MusicKit = musicKit;
    }

    public WeaponEconItem? GetKnife(byte team)
    {
        if (Knives.TryGetValue(team, out var knife))
        {
            return knife;
        }
        return null;
    }

    public WeaponEconItem? GetWeapon(CsTeam team, ushort def)
    {
        if ((team == CsTeam.Terrorist ? TWeapons : CTWeapons).TryGetValue(def, out var weapon))
        {
            return weapon;
        }
        return null;
    }
}
