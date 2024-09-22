/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Ian Lucas. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

using CounterStrikeSharp.API.Core;

namespace InventorySimulator;

public partial class InventorySimulator
{
    public string GetAgentModelPath(string model)
    {
        return $"characters/models/{model}.vmdl";
    }

    public bool IsKnifeClassName(string className)
    {
        return className.Contains("bayonet") || className.Contains("knife");
    }

    public long Now()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    public float ViewAsFloat<T>(T value) where T : struct
    {
        byte[] bytes = value switch
        {
            int intValue => BitConverter.GetBytes(intValue),
            uint uintValue => BitConverter.GetBytes(uintValue),
            _ => throw new ArgumentException("Unsupported type")
        };
        return BitConverter.ToSingle(bytes, 0);
    }

    public bool IsGiveNextSpawn(CCSPlayerController player)
    {
        if (invsim_validate_spawn.Value && !PlayerInventoryManager.ContainsKey(player.SteamID))
        {
            PlayerGiveNextSpawn[player.SteamID] = true;
            return true;
        }
        PlayerGiveNextSpawn.Remove(player.SteamID, out bool _);
        return false;
    }

    public bool CanGivePlayer(CCSPlayerController player)
    {
        return !PlayerGiveNextSpawn.ContainsKey(player.SteamID);
    }
}
