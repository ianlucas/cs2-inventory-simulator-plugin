/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Newtonsoft.Json;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public void LoadPlayerInventories()
    {
        var path = Path.Combine(Server.GameDirectory, g_InventoriesFilePath);
        if (!File.Exists(path))
            return;
        try
        {
            string json = File.ReadAllText(path);
            var inventories = JsonConvert.DeserializeObject<Dictionary<ulong, Dictionary<string, object>>>(json);
            if (inventories == null)
                return;
            foreach (var pair in inventories)
            {
                g_PlayerInventoryLocked.Add(pair.Key);
                g_PlayerInventory[pair.Key] = new PlayerInventory(pair.Value);
            }
        }
        catch (Exception)
        {
        }
    }

    public PlayerInventory GetPlayerInventory(CCSPlayerController player)
    {
        if (g_PlayerInventory.TryGetValue(player.SteamID, out var inventory))
        {
            return inventory;
        }
        return new PlayerInventory();
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

    public ushort GetUShort(string prefix, byte team, ushort defaultValue = 0)
    {
        if (Inventory.TryGetValue($"{prefix}_{team}", out var value))
        {
            return Convert.ToUInt16((long)value);
        }
        return defaultValue;
    }

    public ushort GetUShort(string prefix, ushort defaultValue = 0)
    {
        if (Inventory.TryGetValue(prefix, out var value))
        {
            return Convert.ToUInt16((long)value);
        }
        return defaultValue;
    }

    public uint GetUInt(string prefix, uint defaultValue = 0)
    {
        if (Inventory.TryGetValue(prefix, out var value))
        {
            return (uint)((long)value);
        }
        return defaultValue;
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
