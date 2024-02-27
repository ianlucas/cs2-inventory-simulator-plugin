/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public PlayerInventory GetPlayerInventory(CCSPlayerController player)
    {
        if (g_PlayerInventory.TryGetValue(player.SteamID, out var inventory))
        {
            return inventory;
        }
        return new PlayerInventory(null);
    }
}

public class PlayerInventory
{
    public Dictionary<string, object>? Inventory { get; set; }

    public PlayerInventory(Dictionary<string, object>? inventory)
    {
        Inventory = inventory ?? new Dictionary<string, object>();
    }

    public bool HasProperty(string prefix, byte team)
    {
        if (Inventory == null) return false;
        return Inventory.ContainsKey($"{prefix}_{team}");
    }

    public bool HasProperty(string prefix, byte team, ushort itemDef)
    {
        if (Inventory == null) return false;
        return Inventory.ContainsKey($"{prefix}_{team}_{itemDef}");
    }

    public bool HasProperty(string prefix)
    {
        if (Inventory == null) return false;
        return Inventory.ContainsKey(prefix);
    }

    public ushort GetUShort(string prefix, byte team, ushort defaultValue = 0)
    {
        if (Inventory == null) return defaultValue;
        var key = $"{prefix}_{team}";
        if (Inventory.TryGetValue(key, out var value))
        {
            return Convert.ToUInt16((long)value);
        }
        return defaultValue;
    }

    public ushort GetUShort(string prefix, ushort defaultValue = 0)
    {
        if (Inventory == null) return defaultValue;
        if (Inventory.TryGetValue(prefix, out var value))
        {
            return Convert.ToUInt16((long)value);
        }
        return defaultValue;
    }

    public int GetInt(string prefix, byte team, ushort itemDef, int defaultValue)
    {
        if (Inventory == null) return defaultValue;
        var key = $"{prefix}_{team}_{itemDef}";
        if (Inventory.TryGetValue(key, out var value))
        {
            return Convert.ToInt32((long)value);
        }
        return defaultValue;
    }

    public float GetFloat(string prefix, byte team, ushort itemDef, float defaultValue)
    {
        if (Inventory == null) return defaultValue;
        var key = $"{prefix}_{team}_{itemDef}";
        if (Inventory.TryGetValue(key, out var value))
        {
            return value switch
            {
                double d => (float)d,
                int i => i,
                long i => (float)i,
                _ => defaultValue
            };
        }
        return defaultValue;
    }

    public string GetString(string prefix, byte team, ushort itemDef, string defaultValue)
    {
        if (Inventory == null) return defaultValue;
        var key = $"{prefix}_{team}_{itemDef}";
        if (Inventory.TryGetValue(key, out var value))
        {
            return (string)value;
        }
        return defaultValue;
    }
}
