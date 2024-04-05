﻿/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using Newtonsoft.Json;
using System.Text.Json;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public readonly PlayerInventory EmptyInventory = new()
    {
        Knives = new(),
        Gloves = new(),
        TWeapons = new(),
        CTWeapons = new(),
        Agents = new()
    };

    public void LoadPlayerInventories()
    {
        try
        {
            var path = Path.Combine(Server.GameDirectory, InventoriesFilePath);
            if (!File.Exists(path))
                return;

            string json = File.ReadAllText(path);
            var inventories = JsonConvert.DeserializeObject<Dictionary<ulong, PlayerInventory>>(json);
            if (inventories != null)
            {
                foreach (var pair in inventories)
                {
                    PlayerInventoryLockSet.Add(pair.Key);
                    PlayerInventoryDict.Add(pair.Key, pair.Value);
                }
            }
        }
        catch
        {
            // Ignore any error.
        }
    }

    public void PlayerInventoryCleanUp()
    {
        var connected = Utilities.GetPlayers().Select(player => player.SteamID).ToHashSet();
        var disconnected = PlayerInventoryDict.Keys.Except(connected).ToList();
        foreach (var steamId in disconnected)
        {
            RemovePlayerInventory(steamId);
        }
    }

    public void RemovePlayerInventory(ulong steamId)
    {
        if (!PlayerInventoryLockSet.Contains(steamId))
        {
            PlayerInventoryDict.Remove(steamId);
        }
    }

    public PlayerInventory GetPlayerInventory(CCSPlayerController player)
    {
        if (PlayerInventoryDict.TryGetValue(player.SteamID, out var inventory))
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
}

public class PlayerInventory
{
    [JsonProperty("knives")]
    public required Dictionary<byte, WeaponEconItem> Knives { get; set; }

    [JsonProperty("gloves")]
    public required Dictionary<byte, BaseEconItem> Gloves { get; set; }

    [JsonProperty("tWeapons")]
    public required Dictionary<ushort, WeaponEconItem> TWeapons { get; set; }

    [JsonProperty("ctWeapons")]
    public required Dictionary<ushort, WeaponEconItem> CTWeapons { get; set; }

    [JsonProperty("agents")]
    public required Dictionary<byte, AgentItem> Agents { get; set; }

    [JsonProperty("pin")]
    public uint? Pin { get; set; }

    [JsonProperty("musicKit")]
    public ushort? MusicKit { get; set; }

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
