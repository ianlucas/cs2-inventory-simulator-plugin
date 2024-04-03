/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API;
using Newtonsoft.Json;
using System.Text.Json;

namespace InventorySimulator;

public partial class InventorySimulator
{
    private readonly PlayerInventory g_EmptyInventory = new();

    public void LoadPlayerInventories()
    {
        var path = Path.Combine(Server.GameDirectory, g_InventoriesFilePath);
        if (!File.Exists(path))
            return;
        try
        {
            string json = File.ReadAllText(path);
            var inventories = JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<string, object>>>(json);
            if (inventories != null)
            {
                foreach (var pair in inventories)
                {
                    g_PlayerInventoryLock.Add(pair.Key);
                    g_PlayerInventory[pair.Key] = new PlayerInventory(pair.Value);
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
        var disconnected = g_PlayerInventory.Keys.Except(connected).ToList();
        foreach (var steamId in disconnected)
        {
            RemovePlayerInventory(steamId);
        }
    }

    public void RemovePlayerInventory(ulong steamId)
    {
        if (!g_PlayerInventoryLock.Contains(steamId))
        {
            g_PlayerInventory.Remove(steamId);
        }
    }

    public PlayerInventory GetPlayerInventory(CCSPlayerController player)
    {
        if (g_PlayerInventory.TryGetValue(player.SteamID, out var inventory))
        {
            return inventory;
        }
        return g_EmptyInventory;
    }
}

public class PlayerInventory
{
    public Dictionary<string, object> Inventory;

    public PlayerInventory(Dictionary<string, object>? inventory = null)
    {
        Inventory = inventory ?? new();
    }

    private float ViewUintAsFloat(uint value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        return BitConverter.ToSingle(bytes, 0);
    }

    private float ConvertObjectToFloat(object value, float defaultValue)
    {
        return value switch
        {
            double d => (float)d,
            int i => i,
            long i => (float)i,
            _ => defaultValue
        };
    }

    public bool HasProperty(string prefix, byte team)
    {
        return Inventory.ContainsKey($"{prefix}_{team}");
    }

    public bool HasProperty(string prefix, byte team, ushort itemDef)
    {
        return Inventory.ContainsKey($"{prefix}_{team}_{itemDef}");
    }

    public bool HasProperty(string prefix)
    {
        return Inventory.ContainsKey(prefix);
    }

    public ushort? GetUShort(string prefix, byte team)
    {
        if (Inventory.TryGetValue($"{prefix}_{team}", out var value))
        {
            return Convert.ToUInt16((long)value);
        }
        return null;
    }

    public ushort GetUShort(string prefix, byte team, ushort defaultValue)
    {
        if (Inventory.TryGetValue($"{prefix}_{team}", out var value))
        {
            return Convert.ToUInt16((long)value);
        }
        return defaultValue;
    }

    public ushort? GetUShort(string prefix)
    {
        if (Inventory.TryGetValue(prefix, out var value))
        {
            return Convert.ToUInt16((long)value);
        }
        return null;
    }

    public ushort GetUShort(string prefix, ushort defaultValue)
    {
        if (Inventory.TryGetValue(prefix, out var value))
        {
            return Convert.ToUInt16((long)value);
        }
        return defaultValue;
    }

    public uint? GetUInt(string prefix)
    {
        if (Inventory.TryGetValue(prefix, out var value))
        {
            return (uint)((long)value);
        }
        return null;
    }

    public uint? GetUInt(string prefix, byte team)
    {
        if (Inventory.TryGetValue($"{prefix}_{team}", out var value))
        {
            return (uint)((long)value);
        }
        return null;
    }

    public int GetInt(string prefix, byte team, ushort itemDef, int defaultValue)
    {
        if (Inventory.TryGetValue($"{prefix}_{team}_{itemDef}", out var value))
        {
            return Convert.ToInt32((long)value);
        }
        return defaultValue;
    }

    public float GetFloat(string prefix, byte team, ushort itemDef, float defaultValue)
    {
        if (Inventory.TryGetValue($"{prefix}_{team}_{itemDef}", out var value))
        {
            return ConvertObjectToFloat(value, defaultValue);
        }
        return defaultValue;
    }

    public float GetFloat(string prefix, byte team, ushort itemDef, int slot, float defaultValue)
    {
        if (Inventory.TryGetValue($"{prefix}_{team}_{itemDef}_{slot}", out var value))
        {
            return ConvertObjectToFloat(value, defaultValue);
        }
        return defaultValue;
    }

    public string GetString(string prefix, byte team, ushort itemDef, string defaultValue)
    {
        if (Inventory.TryGetValue($"{prefix}_{team}_{itemDef}", out var value))
        {
            return (string)value;
        }
        return defaultValue;
    }

    public string? GetString(string prefix, byte team)
    {
        if (Inventory.TryGetValue($"{prefix}_{team}", out var value))
        {
            return (string)value;
        }
        return null;
    }

    public float GetIntAsFloat(string prefix, byte team, ushort itemDef, int slot, uint defaultValue)
    {
        if (Inventory.TryGetValue($"{prefix}_{team}_{itemDef}_{slot}", out var value))
        {
            return ViewUintAsFloat((uint)((long)value));
        }
        return ViewUintAsFloat(defaultValue);
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
    [JsonProperty("nametag")]
    public required string Nametag { get; set; }

    [JsonProperty("stattrak")]
    public required int Stattrak { get; set; }

    [JsonProperty("stickers")]
    public required List<StickerItem> Stickers { get; set; }
}

public class KnifeEconItem : BaseEconItem
{
    [JsonProperty("stattrak")]
    public required int Stattrak { get; set; }
}

public class AgentItem
{
    [JsonProperty("model")]
    public required string Model { get; set; }

    [JsonProperty("patches")]
    public required List<uint> Patches { get; set; }
}

public class PlayerEquippedInventory
{
    [JsonProperty("knives")]
    public required Dictionary<byte, KnifeEconItem> Knives { get; set; }

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
}
